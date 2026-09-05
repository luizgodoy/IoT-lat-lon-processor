using System.Text.Json;

namespace IoT_lat_lon_processor.Configuration;

public interface IConfigurationService
{
    GeocodingOptions Geocoding { get; }
    ProcessingOptions Processing { get; }
    void Reload();
    Task SaveAsync(GeocodingOptions geocoding, ProcessingOptions processing);
}

public sealed class ConfigurationService : IConfigurationService
{
    private readonly string _configFilePath;
    public GeocodingOptions Geocoding { get; private set; } = new();
    public ProcessingOptions Processing { get; private set; } = new();

    public ConfigurationService(string? customPath = null)
    {
        _configFilePath = customPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        Reload();
    }

    public void Reload()
    {
        if (!File.Exists(_configFilePath))
        {
            Geocoding = new GeocodingOptions();
            Processing = new ProcessingOptions();
            return;
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Geocoding", out var geoProp))
            {
                Geocoding = JsonSerializer.Deserialize<GeocodingOptions>(geoProp.GetRawText()) ?? new GeocodingOptions();
            }

            if (root.TryGetProperty("Processing", out var procProp))
            {
                Processing = JsonSerializer.Deserialize<ProcessingOptions>(procProp.GetRawText()) ?? new ProcessingOptions();
            }
        }
        catch
        {
            Geocoding = new GeocodingOptions();
            Processing = new ProcessingOptions();
        }
    }

    public async Task SaveAsync(GeocodingOptions geocoding, ProcessingOptions processing)
    {
        Geocoding = geocoding;
        Processing = processing;

        var fullConfig = new
        {
            Geocoding,
            Processing
        };

        var json = JsonSerializer.Serialize(fullConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configFilePath, json);
    }
}
