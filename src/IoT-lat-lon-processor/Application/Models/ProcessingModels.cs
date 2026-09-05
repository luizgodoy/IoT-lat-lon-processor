namespace IoT_lat_lon_processor.Application.Models;

/// <summary>
/// Parâmetros para iniciar a execução do pipeline de processamento.
/// </summary>
public sealed class ProcessingRequest
{
    public string InputFilePath { get; init; } = string.Empty;
    public string LatitudeColumn { get; init; } = string.Empty;
    public string LongitudeColumn { get; init; } = string.Empty;
    public bool SwapAxes { get; init; }
    public string AddressColumnName { get; init; } = "Endereço";
    public string? SheetName { get; init; }
    public string? CustomOutputDirectory { get; init; }
}

/// <summary>
/// Notificação periódica de progresso para atualização da interface de usuário.
/// </summary>
public sealed class ProcessingProgressUpdate
{
    public long TotalRows { get; init; }
    public long ProcessedRows { get; init; }
    public long SuccessCount { get; init; }
    public long NotFoundCount { get; init; }
    public long ErrorCount { get; init; }
    public long CacheHitCount { get; init; }
    public long RequestCount { get; init; }
    public double Percentage => TotalRows > 0 ? Math.Clamp((double)ProcessedRows / TotalRows * 100.0, 0, 100.0) : 0;
    public TimeSpan Elapsed { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}
