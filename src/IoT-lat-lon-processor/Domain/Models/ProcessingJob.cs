namespace IoT_lat_lon_processor.Domain.Models;

/// <summary>
/// Status de execução de um trabalho de processamento.
/// </summary>
public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Cancelled,
    Failed
}

/// <summary>
/// Representa uma sessão de processamento de arquivo com suas métricas e configurações.
/// </summary>
public sealed class ProcessingJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    public string InputFileName { get; set; } = string.Empty;
    public string InputFilePath { get; set; } = string.Empty;
    public string? OutputFilePath { get; set; }

    public string LatitudeColumn { get; set; } = string.Empty;
    public string LongitudeColumn { get; set; } = string.Empty;
    public string Provider { get; set; } = "Nominatim";

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public long TotalRows { get; set; }
    public long ValidRows { get; set; }
    public long InvalidRows { get; set; }
    public long SwappedRows { get; set; }
    public long SuccessRows { get; set; }
    public long NotFoundRows { get; set; }
    public long ErrorRows { get; set; }
    public long CacheHits { get; set; }
    public long RequestCount { get; set; }
    public long DurationMs { get; set; }

    public TimeSpan Duration => FinishedAt.HasValue
        ? FinishedAt.Value - StartedAt
        : TimeSpan.FromMilliseconds(DurationMs);
}
