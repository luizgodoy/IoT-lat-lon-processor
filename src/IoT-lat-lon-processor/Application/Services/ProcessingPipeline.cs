using System.Diagnostics;
using System.Text.Json;
using IoT_lat_lon_processor.Application.Models;
using IoT_lat_lon_processor.Configuration;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Domain.Services;
using IoT_lat_lon_processor.Infrastructure.FileExport;
using IoT_lat_lon_processor.Infrastructure.FileImport;
using IoT_lat_lon_processor.Infrastructure.Http;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using IoT_lat_lon_processor.Infrastructure.Storage;

namespace IoT_lat_lon_processor.Application.Services;

public sealed class ProcessingPipeline : IProcessingPipeline
{
    private readonly IEnumerable<IFileReader> _fileReaders;
    private readonly IEnumerable<IFileWriter> _fileWriters;
    private readonly ICoordinateValidator _validator;
    private readonly IGeocodingProvider _geocodingProvider;
    private readonly IProcessingRepository _repository;
    private readonly ICoordinateCacheRepository _cache;
    private readonly IStoragePathService _pathService;
    private readonly IRateLimiter _rateLimiter;
    private readonly ProcessingOptions _processingOptions;
    private readonly GeocodingOptions _geocodingOptions;

    public ProcessingPipeline(
        IEnumerable<IFileReader> fileReaders,
        IEnumerable<IFileWriter> fileWriters,
        ICoordinateValidator validator,
        IGeocodingProvider geocodingProvider,
        IProcessingRepository repository,
        ICoordinateCacheRepository cache,
        IStoragePathService pathService,
        IRateLimiter rateLimiter,
        ProcessingOptions processingOptions,
        GeocodingOptions geocodingOptions)
    {
        _fileReaders = fileReaders;
        _fileWriters = fileWriters;
        _validator = validator;
        _geocodingProvider = geocodingProvider;
        _repository = repository;
        _cache = cache;
        _pathService = pathService;
        _rateLimiter = rateLimiter;
        _processingOptions = processingOptions;
        _geocodingOptions = geocodingOptions;
    }

    public async Task<ProcessingSummary> ExecuteAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgressUpdate>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.InputFilePath))
        {
            throw new FileNotFoundException("Arquivo de entrada não encontrado.", request.InputFilePath);
        }

        var reader = _fileReaders.FirstOrDefault(r => r.CanHandle(request.InputFilePath))
            ?? throw new NotSupportedException($"Formato de arquivo não suportado: {request.InputFilePath}");

        var writer = _fileWriters.FirstOrDefault(w => w.CanHandle(request.InputFilePath))
            ?? throw new NotSupportedException($"Formato de escrita não suportado: {request.InputFilePath}");

        var jobId = Guid.NewGuid().ToString("N");
        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var jobDir = _pathService.GetJobDirectory(jobId, startedAt);
        var inputDir = _pathService.GetJobInputDirectory(jobId, startedAt);
        var outputDir = request.CustomOutputDirectory ?? _pathService.GetJobOutputDirectory(jobId, startedAt);
        var logsDir = _pathService.GetJobLogsDirectory(jobId, startedAt);
        var stateDir = _pathService.GetJobStateDirectory(jobId, startedAt);

        // Copia arquivo de entrada se habilitado
        var inputFileName = Path.GetFileName(request.InputFilePath);
        if (_processingOptions.KeepInputFiles)
        {
            var copiedInput = Path.Combine(inputDir, inputFileName);
            File.Copy(request.InputFilePath, copiedInput, overwrite: true);
        }

        var schema = await reader.InspectAsync(request.InputFilePath, request.SheetName, ct);
        var latColIdx = schema.Columns.ToList().FindIndex(c => string.Equals(c.Trim(), request.LatitudeColumn.Trim(), StringComparison.OrdinalIgnoreCase));
        var lonColIdx = schema.Columns.ToList().FindIndex(c => string.Equals(c.Trim(), request.LongitudeColumn.Trim(), StringComparison.OrdinalIgnoreCase));

        if (latColIdx < 0 || lonColIdx < 0)
        {
            throw new InvalidOperationException($"Colunas de latitude '{request.LatitudeColumn}' ou longitude '{request.LongitudeColumn}' não foram encontradas no arquivo.");
        }

        var outputFileName = $"{Path.GetFileNameWithoutExtension(inputFileName)}_processado{Path.GetExtension(inputFileName)}";
        var outputPath = Path.Combine(outputDir, outputFileName);

        var job = new ProcessingJob
        {
            Id = jobId,
            StartedAt = startedAt,
            InputFileName = inputFileName,
            InputFilePath = request.InputFilePath,
            OutputFilePath = outputPath,
            LatitudeColumn = request.LatitudeColumn,
            LongitudeColumn = request.LongitudeColumn,
            Provider = _geocodingProvider.Name,
            Status = JobStatus.Running
        };

        await _repository.SaveJobAsync(job, jobDir, ct);

        // Fase 1: Leitura e Validação em Streaming
        var allRows = new List<ProcessingRow>();
        var uniqueCoordinates = new Dictionary<string, (Coordinate Coord, List<ProcessingRow> Rows)>();

        await foreach (var rowItem in reader.ReadRowsAsync(request.InputFilePath, request.SheetName, ct))
        {
            ct.ThrowIfCancellationRequested();

            var rawLat = rowItem.GetValue(latColIdx);
            var rawLon = rowItem.GetValue(lonColIdx);

            if (request.SwapAxes)
            {
                (rawLat, rawLon) = (rawLon, rawLat);
            }

            var validation = _validator.ParseAndValidate(rawLat, rawLon);

            var procRow = new ProcessingRow
            {
                JobId = jobId,
                SourceRowNumber = rowItem.RowNumber,
                RawLatitude = rawLat,
                RawLongitude = rawLon,
                UpdatedAt = DateTime.UtcNow
            };

            if (validation.IsValid && validation.Original.HasValue)
            {
                var coord = validation.Original.Value;
                procRow.Latitude = coord.Latitude;
                procRow.Longitude = coord.Longitude;
                procRow.NormalizedLatitude = Math.Round(coord.Latitude, _geocodingOptions.CachePrecision, MidpointRounding.AwayFromZero);
                procRow.NormalizedLongitude = Math.Round(coord.Longitude, _geocodingOptions.CachePrecision, MidpointRounding.AwayFromZero);
                procRow.Status = RowProcessingStatus.Pending;

                job.ValidRows++;
                if (request.SwapAxes)
                {
                    job.SwappedRows++;
                }

                var cacheKey = coord.GetCacheKey(_geocodingOptions.CachePrecision, _geocodingProvider.Name);
                if (!uniqueCoordinates.TryGetValue(cacheKey, out var entry))
                {
                    entry = (coord, new List<ProcessingRow>());
                    uniqueCoordinates[cacheKey] = entry;
                }
                entry.Rows.Add(procRow);
            }
            else
            {
                procRow.Status = RowProcessingStatus.InvalidCoordinate;
                procRow.ErrorMessage = validation.Message;
                job.InvalidRows++;
            }

            allRows.Add(procRow);
            job.TotalRows++;
        }

        // Salva checkpoint inicial das linhas
        await _repository.SaveRowsBatchAsync(allRows, ct);

        // Fase 2: Resolução de Endereços com Deduplicação e Cache
        var lastProgressReport = Stopwatch.StartNew();
        long processedRowsCount = job.InvalidRows;

        foreach (var (cacheKey, (coord, rows)) in uniqueCoordinates)
        {
            if (ct.IsCancellationRequested)
            {
                job.Status = JobStatus.Cancelled;
                break;
            }

            // Verifica Cache Primeiro
            var cachedAddress = await _cache.GetAddressAsync(coord, _geocodingOptions.CachePrecision, _geocodingProvider.Name, ct);

            if (!string.IsNullOrEmpty(cachedAddress))
            {
                job.CacheHits += rows.Count;
                job.SuccessRows += rows.Count;

                foreach (var r in rows)
                {
                    r.Address = cachedAddress;
                    r.Status = RowProcessingStatus.Success;
                    r.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Consulta Serviço Externo sob Rate Limit
                job.RequestCount++;
                var result = await _geocodingProvider.ReverseAsync(coord, ct);

                if (result.Success && !string.IsNullOrEmpty(result.DisplayAddress))
                {
                    job.SuccessRows += rows.Count;
                    if (rows.Count > 1)
                    {
                        job.CacheHits += rows.Count - 1; // Deduplicação poupou chamadas de rede
                    }

                    foreach (var r in rows)
                    {
                        r.Address = result.DisplayAddress;
                        r.Status = RowProcessingStatus.Success;
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (result.ErrorCode == GeocodingErrorCodes.NotFound)
                {
                    job.NotFoundRows += rows.Count;
                    foreach (var r in rows)
                    {
                        r.Status = RowProcessingStatus.NotFound;
                        r.ErrorMessage = result.ErrorMessage;
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (result.ErrorCode == GeocodingErrorCodes.Cancelled)
                {
                    job.Status = JobStatus.Cancelled;
                    break;
                }
                else
                {
                    job.ErrorRows += rows.Count;
                    foreach (var r in rows)
                    {
                        r.Status = RowProcessingStatus.Error;
                        r.ErrorMessage = result.ErrorMessage;
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            processedRowsCount += rows.Count;

            // Atualização periódica de progresso para UI
            if (lastProgressReport.ElapsedMilliseconds >= _processingOptions.ProgressUpdateIntervalMs)
            {
                progress?.Report(new ProcessingProgressUpdate
                {
                    TotalRows = job.TotalRows,
                    ProcessedRows = processedRowsCount,
                    SuccessCount = job.SuccessRows,
                    NotFoundCount = job.NotFoundRows,
                    ErrorCount = job.ErrorRows,
                    CacheHitCount = job.CacheHits,
                    RequestCount = job.RequestCount,
                    Elapsed = stopwatch.Elapsed,
                    StatusMessage = $"Processando coordenadas... ({processedRowsCount}/{job.TotalRows})"
                });
                lastProgressReport.Restart();
            }

            // Checkpoint periódico no banco de dados e disco
            if (processedRowsCount % _processingOptions.CheckpointEveryRows == 0)
            {
                await _repository.SaveRowsBatchAsync(rows, ct);
                await WriteCheckpointFileAsync(stateDir, job, processedRowsCount);
            }
        }

        // Salva estado final das linhas no SQLite
        await _repository.SaveRowsBatchAsync(allRows, CancellationToken.None);

        // Fase 3: Geração do Arquivo de Saída Enriquecido
        if (job.Status != JobStatus.Cancelled)
        {
            var addressesMap = allRows.ToDictionary(r => r.SourceRowNumber, r => r.Address);
            await writer.WriteEnrichedFileAsync(
                request.InputFilePath,
                outputPath,
                request.AddressColumnName,
                addressesMap,
                request.SheetName,
                ct);

            job.Status = job.ErrorRows == job.TotalRows && job.TotalRows > 0
                ? JobStatus.Failed
                : JobStatus.Completed;
        }

        stopwatch.Stop();
        job.FinishedAt = DateTime.UtcNow;
        job.DurationMs = stopwatch.ElapsedMilliseconds;

        await _repository.SaveJobAsync(job, jobDir, CancellationToken.None);

        // Gera Log Estruturado
        if (_processingOptions.KeepLogs)
        {
            await WriteStructuredLogAsync(logsDir, job);
        }

        progress?.Report(new ProcessingProgressUpdate
        {
            TotalRows = job.TotalRows,
            ProcessedRows = job.TotalRows,
            SuccessCount = job.SuccessRows,
            NotFoundCount = job.NotFoundRows,
            ErrorCount = job.ErrorRows,
            CacheHitCount = job.CacheHits,
            RequestCount = job.RequestCount,
            Elapsed = stopwatch.Elapsed,
            StatusMessage = job.Status == JobStatus.Completed ? "Processamento concluído com sucesso." : "Processamento interrompido."
        });

        return ProcessingSummary.FromJob(job, _rateLimiter.PauseCount);
    }

    public async Task<ProcessingSummary> ResumeAsync(
        string jobId,
        IProgress<ProcessingProgressUpdate>? progress = null,
        CancellationToken ct = default)
    {
        var job = await _repository.GetJobAsync(jobId, ct)
            ?? throw new InvalidOperationException($"Processamento com id '{jobId}' não encontrado.");

        var existingRows = await _repository.GetRowsAsync(jobId, ct);
        if (existingRows.Count == 0)
        {
            throw new InvalidOperationException("Não há registros salvos para retomar este processamento.");
        }

        var writer = _fileWriters.FirstOrDefault(w => w.CanHandle(job.InputFilePath))
            ?? throw new NotSupportedException($"Formato não suportado para escrita: {job.InputFilePath}");

        job.Status = JobStatus.Running;
        var stopwatch = Stopwatch.StartNew();

        var pendingRows = existingRows.Where(r => r.Status is RowProcessingStatus.Pending or RowProcessingStatus.Error).ToList();
        var uniquePending = new Dictionary<string, (Coordinate Coord, List<ProcessingRow> Rows)>();

        foreach (var row in pendingRows)
        {
            if (row.Latitude.HasValue && row.Longitude.HasValue)
            {
                var coord = new Coordinate(row.Latitude.Value, row.Longitude.Value);
                var key = coord.GetCacheKey(_geocodingOptions.CachePrecision, _geocodingProvider.Name);
                if (!uniquePending.TryGetValue(key, out var entry))
                {
                    entry = (coord, new List<ProcessingRow>());
                    uniquePending[key] = entry;
                }
                entry.Rows.Add(row);
            }
        }

        foreach (var (_, (coord, rows)) in uniquePending)
        {
            if (ct.IsCancellationRequested)
            {
                job.Status = JobStatus.Cancelled;
                break;
            }

            var cached = await _cache.GetAddressAsync(coord, _geocodingOptions.CachePrecision, _geocodingProvider.Name, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                job.CacheHits += rows.Count;
                job.SuccessRows += rows.Count;
                foreach (var r in rows)
                {
                    r.Address = cached;
                    r.Status = RowProcessingStatus.Success;
                    r.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                job.RequestCount++;
                var result = await _geocodingProvider.ReverseAsync(coord, ct);

                if (result.Success && !string.IsNullOrEmpty(result.DisplayAddress))
                {
                    job.SuccessRows += rows.Count;
                    foreach (var r in rows)
                    {
                        r.Address = result.DisplayAddress;
                        r.Status = RowProcessingStatus.Success;
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (result.ErrorCode == GeocodingErrorCodes.NotFound)
                {
                    job.NotFoundRows += rows.Count;
                    foreach (var r in rows)
                    {
                        r.Status = RowProcessingStatus.NotFound;
                        r.ErrorMessage = result.ErrorMessage;
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    job.ErrorRows += rows.Count;
                    foreach (var r in rows)
                    {
                        r.Status = RowProcessingStatus.Error;
                        r.ErrorMessage = result.ErrorMessage;
                        r.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await _repository.SaveRowsBatchAsync(rows, ct);
        }

        if (job.Status != JobStatus.Cancelled && !string.IsNullOrEmpty(job.OutputFilePath))
        {
            var addressesMap = existingRows.ToDictionary(r => r.SourceRowNumber, r => r.Address);
            await writer.WriteEnrichedFileAsync(
                job.InputFilePath,
                job.OutputFilePath,
                "Endereço",
                addressesMap,
                sheetName: null,
                ct);

            job.Status = JobStatus.Completed;
        }

        stopwatch.Stop();
        job.FinishedAt = DateTime.UtcNow;
        job.DurationMs += stopwatch.ElapsedMilliseconds;

        await _repository.SaveJobAsync(job, ct: CancellationToken.None);

        return ProcessingSummary.FromJob(job, _rateLimiter.PauseCount);
    }

    private static async Task WriteCheckpointFileAsync(string stateDir, ProcessingJob job, long processedRows)
    {
        var checkpointPath = Path.Combine(stateDir, "checkpoint.json");
        var checkpointData = new
        {
            job.Id,
            job.InputFileName,
            ProcessedRows = processedRows,
            job.TotalRows,
            job.SuccessRows,
            job.ErrorRows,
            job.CacheHits,
            job.RequestCount,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(checkpointData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(checkpointPath, json);
    }

    private static async Task WriteStructuredLogAsync(string logsDir, ProcessingJob job)
    {
        var logPath = Path.Combine(logsDir, "processamento.jsonl");
        var logEntry = new
        {
            JobId = job.Id,
            DataInicio = job.StartedAt,
            DataFim = job.FinishedAt,
            Arquivo = job.InputFileName,
            QuantidadeRegistros = job.TotalRows,
            RegistrosValidos = job.ValidRows,
            RegistrosInvalidos = job.InvalidRows,
            RegistrosInvertidos = job.SwappedRows,
            EnderecosEncontrados = job.SuccessRows,
            EnderecosNaoEncontrados = job.NotFoundRows,
            Erros = job.ErrorRows,
            CacheHits = job.CacheHits,
            QuantidadeRequisicoes = job.RequestCount,
            TempoTotalMs = job.DurationMs,
            Status = job.Status.ToString()
        };

        var jsonLine = JsonSerializer.Serialize(logEntry) + Environment.NewLine;
        await File.AppendAllTextAsync(logPath, jsonLine);
    }
}
