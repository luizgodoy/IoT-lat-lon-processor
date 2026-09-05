using SQLite;

namespace IoT_lat_lon_processor.Infrastructure.Persistence;

[Table("ProcessingJobs")]
public sealed class DbProcessingJob
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    [Indexed]
    public string InputFileName { get; set; } = string.Empty;
    public string InputFilePath { get; set; } = string.Empty;
    public string? OutputFilePath { get; set; }

    public string LatitudeColumn { get; set; } = string.Empty;
    public string LongitudeColumn { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;

    [Indexed]
    public string Status { get; set; } = string.Empty;

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

    public string JobDirectory { get; set; } = string.Empty;
}

[Table("ProcessingRows")]
public sealed class DbProcessingRow
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    [Indexed]
    public string JobId { get; set; } = string.Empty;

    [Indexed]
    public long SourceRowNumber { get; set; }

    public string? RawLatitude { get; set; }
    public string? RawLongitude { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal? NormalizedLatitude { get; set; }
    public decimal? NormalizedLongitude { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int Attempts { get; set; }
    public DateTime UpdatedAt { get; set; }
}

[Table("CoordinateCache")]
public sealed class DbCoordinateCache
{
    [PrimaryKey]
    public string CacheKey { get; set; } = string.Empty;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Provider { get; set; } = string.Empty;

    public string? DisplayAddress { get; set; }
    public bool Found { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
