using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IoT_lat_lon_processor.Configuration;

namespace IoT_lat_lon_processor.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;

    [ObservableProperty]
    private string _provider = "Nominatim";

    [ObservableProperty]
    private string _baseUrl = "https://nominatim.openstreetmap.org";

    [ObservableProperty]
    private string _userAgent = "IoT-lat-lon-processor/1.0 (contact: support@local.invalid)";

    [ObservableProperty]
    private int _requestIntervalMs = 1100;

    [ObservableProperty]
    private int _maxConcurrency = 1;

    [ObservableProperty]
    private int _timeoutSeconds = 30;

    [ObservableProperty]
    private int _maxRetries = 3;

    [ObservableProperty]
    private int _cachePrecision = 6;

    [ObservableProperty]
    private bool _keepInputFiles = true;

    [ObservableProperty]
    private bool _keepLogs = true;

    [ObservableProperty]
    private int _checkpointEveryRows = 100;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string? _warningMessage;

    public SettingsViewModel(IConfigurationService configService)
    {
        _configService = configService;
        LoadCurrentSettings();
    }

    public void LoadCurrentSettings()
    {
        _configService.Reload();
        var geo = _configService.Geocoding;
        var proc = _configService.Processing;

        Provider = geo.Provider;
        BaseUrl = geo.BaseUrl;
        UserAgent = geo.UserAgent;
        RequestIntervalMs = geo.RequestIntervalMs;
        MaxConcurrency = geo.MaxConcurrency;
        TimeoutSeconds = geo.TimeoutSeconds;
        MaxRetries = geo.MaxRetries;
        CachePrecision = geo.CachePrecision;

        KeepInputFiles = proc.KeepInputFiles;
        KeepLogs = proc.KeepLogs;
        CheckpointEveryRows = proc.CheckpointEveryRows;

        CheckTrafficWarnings();
    }

    partial void OnRequestIntervalMsChanged(int value) => CheckTrafficWarnings();
    partial void OnMaxConcurrencyChanged(int value) => CheckTrafficWarnings();

    private void CheckTrafficWarnings()
    {
        if (string.Equals(Provider, "Nominatim", StringComparison.OrdinalIgnoreCase))
        {
            if (RequestIntervalMs < 1000 || MaxConcurrency > 1)
            {
                WarningMessage = "Atenção: A política de uso do OpenStreetMap Nominatim exige no máximo 1 requisição por segundo (intervalo >= 1000ms e concorrência = 1). Valores agressivos podem levar a bloqueio temporário (HTTP 429).";
                return;
            }
        }
        WarningMessage = null;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        CheckTrafficWarnings();

        var geo = new GeocodingOptions
        {
            Provider = Provider,
            BaseUrl = BaseUrl,
            UserAgent = UserAgent,
            RequestIntervalMs = RequestIntervalMs,
            MaxConcurrency = MaxConcurrency,
            TimeoutSeconds = TimeoutSeconds,
            MaxRetries = MaxRetries,
            CachePrecision = CachePrecision
        };

        var proc = new ProcessingOptions
        {
            KeepInputFiles = KeepInputFiles,
            KeepLogs = KeepLogs,
            CheckpointEveryRows = CheckpointEveryRows
        };

        await _configService.SaveAsync(geo, proc);
        StatusMessage = "Configurações salvas com sucesso!";
    }

    [RelayCommand]
    public void ResetDefaults()
    {
        var defaultGeo = new GeocodingOptions();
        var defaultProc = new ProcessingOptions();

        Provider = defaultGeo.Provider;
        BaseUrl = defaultGeo.BaseUrl;
        UserAgent = defaultGeo.UserAgent;
        RequestIntervalMs = defaultGeo.RequestIntervalMs;
        MaxConcurrency = defaultGeo.MaxConcurrency;
        TimeoutSeconds = defaultGeo.TimeoutSeconds;
        MaxRetries = defaultGeo.MaxRetries;
        CachePrecision = defaultGeo.CachePrecision;

        KeepInputFiles = defaultProc.KeepInputFiles;
        KeepLogs = defaultProc.KeepLogs;
        CheckpointEveryRows = defaultProc.CheckpointEveryRows;

        CheckTrafficWarnings();
        StatusMessage = "Valores padrão restaurados. Clique em Salvar para persistir.";
    }
}
