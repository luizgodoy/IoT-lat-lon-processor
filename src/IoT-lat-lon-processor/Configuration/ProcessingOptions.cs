namespace IoT_lat_lon_processor.Configuration;

/// <summary>
/// Opções de configuração para o pipeline de processamento de arquivos.
/// </summary>
public sealed class ProcessingOptions
{
    public const string SectionName = "Processing";

    public int ProgressUpdateIntervalMs { get; set; } = 250;
    public int CheckpointEveryRows { get; set; } = 100;
    public bool KeepInputFiles { get; set; } = true;
    public bool KeepLogs { get; set; } = true;
}
