using IoT_lat_lon_processor.Domain.Models;

namespace IoT_lat_lon_processor.Infrastructure.Persistence;

public interface IProcessingRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task SaveJobAsync(ProcessingJob job, string? jobDirectory = null, CancellationToken ct = default);
    Task<ProcessingJob?> GetJobAsync(string jobId, CancellationToken ct = default);
    Task<IReadOnlyList<ProcessingJob>> SearchJobsAsync(
        string? text = null,
        DateTime? from = null,
        DateTime? to = null,
        JobStatus? status = null,
        CancellationToken ct = default);
    Task SaveRowsBatchAsync(IEnumerable<ProcessingRow> rows, CancellationToken ct = default);
    Task<IReadOnlyList<ProcessingRow>> GetRowsAsync(string jobId, CancellationToken ct = default);
    Task<Dictionary<long, string?>> GetCompletedRowAddressesAsync(string jobId, CancellationToken ct = default);
    Task DeleteJobAsync(string jobId, CancellationToken ct = default);
}

public interface ICoordinateCacheRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<string?> GetAddressAsync(Coordinate coordinate, int precision, string provider, CancellationToken ct = default);
    Task SetAddressAsync(Coordinate coordinate, int precision, string provider, string? address, bool found, CancellationToken ct = default);
}
