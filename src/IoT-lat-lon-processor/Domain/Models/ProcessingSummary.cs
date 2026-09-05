namespace IoT_lat_lon_processor.Domain.Models;

/// <summary>
/// Resumo estatístico consolidado do processamento de um lote de coordenadas.
/// </summary>
public sealed class ProcessingSummary
{
    public string JobId { get; init; } = string.Empty;
    public string InputFileName { get; init; } = string.Empty;
    public string? OutputFilePath { get; init; }
    public string Provider { get; init; } = string.Empty;
    public JobStatus Status { get; init; }

    public long TotalRows { get; init; }
    public long ValidRows { get; init; }
    public long InvalidRows { get; init; }
    public long SwappedRows { get; init; }
    public long SuccessRows { get; init; }
    public long NotFoundRows { get; init; }
    public long ErrorRows { get; init; }
    public long CacheHits { get; init; }
    public long RequestCount { get; init; }
    public long RateLimitPauses { get; init; }
    public TimeSpan Duration { get; init; }

    public static ProcessingSummary FromJob(ProcessingJob job, long rateLimitPauses = 0) => new()
    {
        JobId = job.Id,
        InputFileName = job.InputFileName,
        OutputFilePath = job.OutputFilePath,
        Provider = job.Provider,
        Status = job.Status,
        TotalRows = job.TotalRows,
        ValidRows = job.ValidRows,
        InvalidRows = job.InvalidRows,
        SwappedRows = job.SwappedRows,
        SuccessRows = job.SuccessRows,
        NotFoundRows = job.NotFoundRows,
        ErrorRows = job.ErrorRows,
        CacheHits = job.CacheHits,
        RequestCount = job.RequestCount,
        RateLimitPauses = rateLimitPauses,
        Duration = job.Duration
    };
}
