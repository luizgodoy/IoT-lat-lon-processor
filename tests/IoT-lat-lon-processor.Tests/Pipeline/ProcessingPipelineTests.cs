using System.Text;
using IoT_lat_lon_processor.Application.Models;
using IoT_lat_lon_processor.Application.Services;
using IoT_lat_lon_processor.Configuration;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Domain.Services;
using IoT_lat_lon_processor.Infrastructure.FileExport;
using IoT_lat_lon_processor.Infrastructure.FileImport;
using IoT_lat_lon_processor.Infrastructure.Http;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using IoT_lat_lon_processor.Infrastructure.Storage;
using Xunit;

namespace IoT_lat_lon_processor.Tests.Pipeline;

public class ProcessingPipelineTests : IDisposable
{
    private readonly string _testDir;
    private readonly StoragePathService _storagePathService;
    private readonly SqliteProcessingRepository _repository;
    private readonly CoordinateValidator _validator;
    private readonly RateLimiter _rateLimiter;
    private readonly ProcessingOptions _procOptions;
    private readonly GeocodingOptions _geoOptions;

    public ProcessingPipelineTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "iot_pipe_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _storagePathService = new StoragePathService(_testDir);
        _repository = new SqliteProcessingRepository(_storagePathService);
        _validator = new CoordinateValidator();
        _rateLimiter = new RateLimiter(intervalMs: 0, maxConcurrency: 5);
        _procOptions = new ProcessingOptions { ProgressUpdateIntervalMs = 50, CheckpointEveryRows = 5 };
        _geoOptions = new GeocodingOptions { MaxRetries = 1, RequestIntervalMs = 0, CachePrecision = 6 };
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task TC09_Duplicate_Coordinates_Deduplicated_To_Reduce_Requests()
    {
        var inputPath = Path.Combine(_testDir, "duplicates.csv");
        var sb = new StringBuilder("id,latitude,longitude\n");
        // 100 linhas com a mesma coordenada
        for (var i = 1; i <= 100; i++)
        {
            sb.AppendLine($"{i},-23.550520,-46.633308");
        }
        await File.WriteAllTextAsync(inputPath, sb.ToString(), Encoding.UTF8);

        var mockProvider = new MockCountingGeocodingProvider(coord =>
            Task.FromResult(GeocodingResult.Succeeded("Praça da Sé, SP", "MockProvider")));

        var pipeline = CreatePipeline(mockProvider);

        var request = new ProcessingRequest
        {
            InputFilePath = inputPath,
            LatitudeColumn = "latitude",
            LongitudeColumn = "longitude"
        };

        var summary = await pipeline.ExecuteAsync(request);

        Assert.Equal(100, summary.TotalRows);
        Assert.Equal(100, summary.SuccessRows);
        Assert.Equal(1, summary.RequestCount); // Exatamente 1 requisição para 100 linhas!
        Assert.Equal(99, summary.CacheHits); // 99 deduplicações/cache hits!
        Assert.True(File.Exists(summary.OutputFilePath));
    }

    [Fact]
    public async Task TC13_Single_Line_Error_Does_Not_Abort_Other_Lines()
    {
        var inputPath = Path.Combine(_testDir, "partial_errors.csv");
        var content = "id,lat,lon\n" +
                      "1,-23.55,-46.63\n" + // Válido
                      "2,999.00,-45.00\n" + // Inválido (Lat=999)
                      "3,-22.90,-43.17\n";  // Válido
        await File.WriteAllTextAsync(inputPath, content, Encoding.UTF8);

        var mockProvider = new MockCountingGeocodingProvider(coord =>
            Task.FromResult(GeocodingResult.Succeeded("Endereço Resolvido", "MockProvider")));

        var pipeline = CreatePipeline(mockProvider);

        var request = new ProcessingRequest
        {
            InputFilePath = inputPath,
            LatitudeColumn = "lat",
            LongitudeColumn = "lon"
        };

        var summary = await pipeline.ExecuteAsync(request);

        Assert.Equal(3, summary.TotalRows);
        Assert.Equal(2, summary.ValidRows);
        Assert.Equal(1, summary.InvalidRows);
        Assert.Equal(2, summary.SuccessRows);
        Assert.Equal(JobStatus.Completed, summary.Status);
        Assert.True(File.Exists(summary.OutputFilePath));

        // Verifica saída
        var reader = new CsvFileReader();
        var rows = new List<DataRowItem>();
        await foreach (var r in reader.ReadRowsAsync(summary.OutputFilePath!))
        {
            rows.Add(r);
        }

        Assert.Equal(3, rows.Count);
        Assert.Equal("Endereço Resolvido", rows[0].Values[^1]);
        Assert.Equal("", rows[1].Values[^1]); // linha com erro permaneceu vazia
        Assert.Equal("Endereço Resolvido", rows[2].Values[^1]);
    }

    [Fact]
    public async Task TC14_Cancel_Processing_Sets_Cancelled_And_Preserves_State()
    {
        var inputPath = Path.Combine(_testDir, "cancel_test.csv");
        var sb = new StringBuilder("id,latitude,longitude\n");
        for (var i = 1; i <= 50; i++)
        {
            sb.AppendLine($"{i},{-20.0 - i * 0.01},{-40.0 - i * 0.01}");
        }
        await File.WriteAllTextAsync(inputPath, sb.ToString(), Encoding.UTF8);

        using var cts = new CancellationTokenSource();
        var calls = 0;

        var mockProvider = new MockCountingGeocodingProvider(coord =>
        {
            calls++;
            if (calls >= 5)
            {
                cts.Cancel(); // Cancela na 5ª chamada
            }
            return Task.FromResult(GeocodingResult.Succeeded("Endereço", "MockProvider"));
        });

        var pipeline = CreatePipeline(mockProvider);

        var request = new ProcessingRequest
        {
            InputFilePath = inputPath,
            LatitudeColumn = "latitude",
            LongitudeColumn = "longitude"
        };

        var summary = await pipeline.ExecuteAsync(request, ct: cts.Token);

        Assert.Equal(JobStatus.Cancelled, summary.Status);

        var savedJob = await _repository.GetJobAsync(summary.JobId);
        Assert.NotNull(savedJob);
        Assert.Equal(JobStatus.Cancelled, savedJob.Status);

        var rows = await _repository.GetRowsAsync(summary.JobId);
        Assert.Equal(50, rows.Count);
    }

    [Fact]
    public async Task TC15_Resume_Job_Completes_Remaining_Lines_Only()
    {
        var inputPath = Path.Combine(_testDir, "resume_test.csv");
        var content = "id,latitude,longitude\n" +
                      "1,-23.55,-46.63\n" +
                      "2,-22.90,-43.17\n";
        await File.WriteAllTextAsync(inputPath, content, Encoding.UTF8);

        var jobId = "job-interrupted-1";
        var job = new ProcessingJob
        {
            Id = jobId,
            InputFileName = Path.GetFileName(inputPath),
            InputFilePath = inputPath,
            OutputFilePath = Path.Combine(_testDir, "resume_out.csv"),
            LatitudeColumn = "latitude",
            LongitudeColumn = "longitude",
            Status = JobStatus.Cancelled,
            TotalRows = 2
        };
        await _repository.SaveJobAsync(job);

        // Linha 1 já estava concluída, linha 2 ainda com erro/pendente
        var rows = new List<ProcessingRow>
        {
            new()
            {
                JobId = jobId,
                SourceRowNumber = 1,
                RawLatitude = "-23.55",
                RawLongitude = "-46.63",
                Latitude = -23.55m,
                Longitude = -46.63m,
                Address = "Endereço Linha 1",
                Status = RowProcessingStatus.Success
            },
            new()
            {
                JobId = jobId,
                SourceRowNumber = 2,
                RawLatitude = "-22.90",
                RawLongitude = "-43.17",
                Latitude = -22.90m,
                Longitude = -43.17m,
                Status = RowProcessingStatus.Pending
            }
        };
        await _repository.SaveRowsBatchAsync(rows);

        var mockProvider = new MockCountingGeocodingProvider(coord =>
            Task.FromResult(GeocodingResult.Succeeded("Endereço Linha 2", "MockProvider")));

        var pipeline = CreatePipeline(mockProvider);
        var summary = await pipeline.ResumeAsync(jobId);

        Assert.Equal(JobStatus.Completed, summary.Status);
        Assert.Equal(1, mockProvider.CallCount); // Apenas a linha 2 pendente foi consultada!

        var finalRows = await _repository.GetRowsAsync(jobId);
        Assert.Equal(2, finalRows.Count);
        Assert.Equal("Endereço Linha 1", finalRows[0].Address);
        Assert.Equal("Endereço Linha 2", finalRows[1].Address);
    }

    [Fact]
    public async Task Pipeline_SwapAxes_True_Processes_Inverted_Columns_Successfully()
    {
        var inputPath = Path.Combine(_testDir, "swapped.csv");
        // Coluna chamada 'lat' tem -46.63 (longitude real), e coluna 'lon' tem -23.55 (latitude real)
        var content = "id,lat,lon\n1,-46.63,-23.55\n";
        await File.WriteAllTextAsync(inputPath, content, Encoding.UTF8);

        Coordinate? capturedCoord = null;
        var mockProvider = new MockCountingGeocodingProvider(coord =>
        {
            capturedCoord = coord;
            return Task.FromResult(GeocodingResult.Succeeded("São Paulo, SP", "MockProvider"));
        });

        var pipeline = CreatePipeline(mockProvider);

        var request = new ProcessingRequest
        {
            InputFilePath = inputPath,
            LatitudeColumn = "lat",
            LongitudeColumn = "lon",
            SwapAxes = true // Usuário confirmou a inversão
        };

        var summary = await pipeline.ExecuteAsync(request);

        Assert.Equal(1, summary.TotalRows);
        Assert.Equal(1, summary.SwappedRows);
        Assert.Equal(1, summary.SuccessRows);
        Assert.NotNull(capturedCoord);
        Assert.Equal(-23.55m, capturedCoord.Value.Latitude); // Invertido para a latitude correta!
        Assert.Equal(-46.63m, capturedCoord.Value.Longitude);
    }

    private ProcessingPipeline CreatePipeline(IGeocodingProvider provider)
    {
        var readers = new IFileReader[] { new CsvFileReader(), new ExcelFileReader() };
        var writers = new IFileWriter[] { new CsvFileWriter(), new ExcelFileWriter() };

        return new ProcessingPipeline(
            readers,
            writers,
            _validator,
            provider,
            _repository,
            _repository,
            _storagePathService,
            _rateLimiter,
            _procOptions,
            _geoOptions);
    }

    private sealed class MockCountingGeocodingProvider : IGeocodingProvider
    {
        public string Name => "MockProvider";
        private readonly Func<Coordinate, Task<GeocodingResult>> _func;
        public int CallCount { get; private set; }

        public MockCountingGeocodingProvider(Func<Coordinate, Task<GeocodingResult>> func)
        {
            _func = func;
        }

        public async Task<GeocodingResult> ReverseAsync(Coordinate coordinate, CancellationToken ct = default)
        {
            CallCount++;
            return await _func(coordinate);
        }
    }
}
