namespace IoT_lat_lon_processor.Domain.Models;

/// <summary>
/// Status do processamento de uma linha individual do arquivo.
/// </summary>
public enum RowProcessingStatus
{
    Pending,
    Success,
    NotFound,
    InvalidCoordinate,
    Error,
    Skipped
}

/// <summary>
/// Representa uma linha processada ou a ser processada de um arquivo de coordenadas.
/// </summary>
public sealed class ProcessingRow
{
    public long Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public long SourceRowNumber { get; set; }

    public string? RawLatitude { get; set; }
    public string? RawLongitude { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public decimal? NormalizedLatitude { get; set; }
    public decimal? NormalizedLongitude { get; set; }

    public string? Address { get; set; }

    public RowProcessingStatus Status { get; set; } = RowProcessingStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int Attempts { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
