using IoT_lat_lon_processor.Application.Services;
using IoT_lat_lon_processor.Configuration;
using IoT_lat_lon_processor.Domain.Services;
using IoT_lat_lon_processor.Infrastructure.FileExport;
using IoT_lat_lon_processor.Infrastructure.FileImport;
using IoT_lat_lon_processor.Infrastructure.Geocoding;
using IoT_lat_lon_processor.Infrastructure.Http;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using IoT_lat_lon_processor.Infrastructure.Storage;
using IoT_lat_lon_processor.UI.ViewModels;
using IoT_lat_lon_processor.UI.Views;
using Microsoft.Extensions.Logging;

namespace IoT_lat_lon_processor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Configuração e Armazenamento
        builder.Services.AddSingleton<IStoragePathService, StoragePathService>();
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IConfigurationService>().Geocoding);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IConfigurationService>().Processing);
        builder.Services.AddSingleton<IFileLauncherService, FileLauncherService>();

        // Persistência SQLite
        builder.Services.AddSingleton<SqliteProcessingRepository>();
        builder.Services.AddSingleton<IProcessingRepository>(sp => sp.GetRequiredService<SqliteProcessingRepository>());
        builder.Services.AddSingleton<ICoordinateCacheRepository>(sp => sp.GetRequiredService<SqliteProcessingRepository>());

        // Validador de Coordenadas
        builder.Services.AddSingleton<ICoordinateValidator, CoordinateValidator>();

        // HTTP Client e Rate Limiter
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IRateLimiter>(sp =>
        {
            var geo = sp.GetRequiredService<IConfigurationService>().Geocoding;
            return new RateLimiter(geo.RequestIntervalMs, geo.MaxConcurrency);
        });

        // Provedor de Geocodificação
        builder.Services.AddSingleton<IGeocodingProvider>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            var options = sp.GetRequiredService<IConfigurationService>().Geocoding;
            var rateLimiter = sp.GetRequiredService<IRateLimiter>();
            var cache = sp.GetRequiredService<ICoordinateCacheRepository>();
            return new NominatimGeocodingProvider(httpClient, options, rateLimiter, cache);
        });

        // Importadores e Exportadores de Arquivo
        builder.Services.AddSingleton<IFileReader, CsvFileReader>();
        builder.Services.AddSingleton<IFileReader, ExcelFileReader>();
        builder.Services.AddSingleton<IFileWriter, CsvFileWriter>();
        builder.Services.AddSingleton<IFileWriter, ExcelFileWriter>();

        // Pipeline de Processamento
        builder.Services.AddSingleton<IProcessingPipeline, ProcessingPipeline>();

        // ViewModels e Views
        builder.Services.AddTransient<StartViewModel>();
        builder.Services.AddTransient<StartView>();

        builder.Services.AddTransient<MappingViewModel>();
        builder.Services.AddTransient<MappingView>();

        builder.Services.AddTransient<ProcessingViewModel>();
        builder.Services.AddTransient<ProcessingView>();

        builder.Services.AddTransient<ResultViewModel>();
        builder.Services.AddTransient<ResultView>();

        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<HistoryView>();

        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsView>();

        return builder.Build();
    }
}
