using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Infrastructure.Storage;
using SQLite;

namespace IoT_lat_lon_processor.Infrastructure.Persistence;

public sealed class SqliteProcessingRepository : IProcessingRepository, ICoordinateCacheRepository
{
    private readonly SQLiteAsyncConnection _db;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SqliteProcessingRepository(IStoragePathService pathService)
    {
        _db = new SQLiteAsyncConnection(pathService.DatabasePath);
    }

    public SqliteProcessingRepository(string databasePath)
    {
        _db = new SQLiteAsyncConnection(databasePath);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;

            await _db.CreateTableAsync<DbProcessingJob>();
            await _db.CreateTableAsync<DbProcessingRow>();
            await _db.CreateTableAsync<DbCoordinateCache>();

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task SaveJobAsync(ProcessingJob job, string? jobDirectory = null, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var entity = new DbProcessingJob
        {
            Id = job.Id,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            InputFileName = job.InputFileName,
            InputFilePath = job.InputFilePath,
            OutputFilePath = job.OutputFilePath,
            LatitudeColumn = job.LatitudeColumn,
            LongitudeColumn = job.LongitudeColumn,
            Provider = job.Provider,
            Status = job.Status.ToString(),
            TotalRows = job.TotalRows,
            ValidRows = job.ValidRows,
            InvalidRows = job.InvalidRows,
            SwappedRows = job.SwappedRows,
            SuccessRows = job.SuccessRows,
            NotFoundRows = job.NotFoundRows,
            ErrorRows = job.ErrorRows,
            CacheHits = job.CacheHits,
            RequestCount = job.RequestCount,
            DurationMs = (long)job.Duration.TotalMilliseconds,
            JobDirectory = jobDirectory ?? string.Empty
        };

        await _db.InsertOrReplaceAsync(entity);
    }

    public async Task<ProcessingJob?> GetJobAsync(string jobId, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var entity = await _db.Table<DbProcessingJob>().Where(j => j.Id == jobId).FirstOrDefaultAsync();
        return entity != null ? MapJob(entity) : null;
    }

    public async Task<IReadOnlyList<ProcessingJob>> SearchJobsAsync(
        string? text = null,
        DateTime? from = null,
        DateTime? to = null,
        JobStatus? status = null,
        CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var query = _db.Table<DbProcessingJob>();

        if (from.HasValue)
        {
            query = query.Where(j => j.StartedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(j => j.StartedAt <= to.Value);
        }

        if (status.HasValue)
        {
            var statusStr = status.Value.ToString();
            query = query.Where(j => j.Status == statusStr);
        }

        var results = await query.OrderByDescending(j => j.StartedAt).ToListAsync();

        if (!string.IsNullOrWhiteSpace(text))
        {
            var filter = text.Trim();
            results = results.Where(j =>
                j.InputFileName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                (j.OutputFilePath != null && j.OutputFilePath.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                j.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                j.Provider.Contains(filter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        return results.Select(MapJob).ToList();
    }

    public async Task SaveRowsBatchAsync(IEnumerable<ProcessingRow> rows, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var entities = rows.Select(r => new DbProcessingRow
        {
            Id = r.Id,
            JobId = r.JobId,
            SourceRowNumber = r.SourceRowNumber,
            RawLatitude = r.RawLatitude,
            RawLongitude = r.RawLongitude,
            Latitude = r.Latitude,
            Longitude = r.Longitude,
            NormalizedLatitude = r.NormalizedLatitude,
            NormalizedLongitude = r.NormalizedLongitude,
            Address = r.Address,
            Status = r.Status.ToString(),
            ErrorMessage = r.ErrorMessage,
            Attempts = r.Attempts,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        await _db.RunInTransactionAsync(conn =>
        {
            foreach (var entity in entities)
            {
                if (entity.Id == 0)
                {
                    var existing = conn.Table<DbProcessingRow>()
                        .FirstOrDefault(r => r.JobId == entity.JobId && r.SourceRowNumber == entity.SourceRowNumber);

                    if (existing != null)
                    {
                        entity.Id = existing.Id;
                        conn.Update(entity);
                    }
                    else
                    {
                        conn.Insert(entity);
                    }
                }
                else
                {
                    conn.InsertOrReplace(entity);
                }
            }
        });
    }

    public async Task<IReadOnlyList<ProcessingRow>> GetRowsAsync(string jobId, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var entities = await _db.Table<DbProcessingRow>()
            .Where(r => r.JobId == jobId)
            .OrderBy(r => r.SourceRowNumber)
            .ToListAsync();

        return entities.Select(MapRow).ToList();
    }

    public async Task<Dictionary<long, string?>> GetCompletedRowAddressesAsync(string jobId, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var entities = await _db.Table<DbProcessingRow>()
            .Where(r => r.JobId == jobId)
            .ToListAsync();

        return entities.ToDictionary(r => r.SourceRowNumber, r => r.Address);
    }

    public async Task DeleteJobAsync(string jobId, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        await _db.RunInTransactionAsync(conn =>
        {
            conn.Table<DbProcessingRow>().Delete(r => r.JobId == jobId);
            conn.Table<DbProcessingJob>().Delete(j => j.Id == jobId);
        });
    }

    public async Task<string?> GetAddressAsync(Coordinate coordinate, int precision, string provider, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var cacheKey = coordinate.GetCacheKey(precision, provider);
        var cached = await _db.Table<DbCoordinateCache>().Where(c => c.CacheKey == cacheKey).FirstOrDefaultAsync();

        return cached?.DisplayAddress;
    }

    public async Task SetAddressAsync(Coordinate coordinate, int precision, string provider, string? address, bool found, CancellationToken ct = default)
    {
        await InitializeAsync(ct);

        var cacheKey = coordinate.GetCacheKey(precision, provider);
        var entry = new DbCoordinateCache
        {
            CacheKey = cacheKey,
            Latitude = coordinate.Latitude,
            Longitude = coordinate.Longitude,
            Provider = provider.Trim(),
            DisplayAddress = address,
            Found = found,
            CreatedAt = DateTime.UtcNow
        };

        await _db.InsertOrReplaceAsync(entry);
    }

    private static ProcessingJob MapJob(DbProcessingJob entity)
    {
        Enum.TryParse<JobStatus>(entity.Status, out var status);

        return new ProcessingJob
        {
            Id = entity.Id,
            StartedAt = entity.StartedAt,
            FinishedAt = entity.FinishedAt,
            InputFileName = entity.InputFileName,
            InputFilePath = entity.InputFilePath,
            OutputFilePath = entity.OutputFilePath,
            LatitudeColumn = entity.LatitudeColumn,
            LongitudeColumn = entity.LongitudeColumn,
            Provider = entity.Provider,
            Status = status,
            TotalRows = entity.TotalRows,
            ValidRows = entity.ValidRows,
            InvalidRows = entity.InvalidRows,
            SwappedRows = entity.SwappedRows,
            SuccessRows = entity.SuccessRows,
            NotFoundRows = entity.NotFoundRows,
            ErrorRows = entity.ErrorRows,
            CacheHits = entity.CacheHits,
            RequestCount = entity.RequestCount,
            DurationMs = entity.DurationMs
        };
    }

    private static ProcessingRow MapRow(DbProcessingRow entity)
    {
        Enum.TryParse<RowProcessingStatus>(entity.Status, out var status);

        return new ProcessingRow
        {
            Id = entity.Id,
            JobId = entity.JobId,
            SourceRowNumber = entity.SourceRowNumber,
            RawLatitude = entity.RawLatitude,
            RawLongitude = entity.RawLongitude,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            NormalizedLatitude = entity.NormalizedLatitude,
            NormalizedLongitude = entity.NormalizedLongitude,
            Address = entity.Address,
            Status = status,
            ErrorMessage = entity.ErrorMessage,
            Attempts = entity.Attempts,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
