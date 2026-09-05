namespace IoT_lat_lon_processor.Configuration;

/// <summary>
/// Opções de configuração para serviços de geocodificação reversa.
/// </summary>
public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    public string Provider { get; set; } = "Nominatim";
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
    public int RequestIntervalMs { get; set; } = 1100;
    public int MaxConcurrency { get; set; } = 1;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int CachePrecision { get; set; } = 6;
    public string UserAgent { get; set; } = "IoT-lat-lon-processor/1.0 (contact: support@local.invalid)";
}
