using System.Text;
using IoT_lat_lon_processor.Application.Models;
using IoT_lat_lon_processor.Application.Services;
using IoT_lat_lon_processor.Configuration;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Domain.Services;
using IoT_lat_lon_processor.Infrastructure.FileExport;
using IoT_lat_lon_processor.Infrastructure.FileImport;
using IoT_lat_lon_processor.Infrastructure.Http;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using IoT_lat_lon_processor.Infrastructure.Storage;
using Xunit;

namespace IoT_lat_lon_processor.Tests.Harness;

public class HarnessAcceptanceTests : IDisposable
{
    private readonly string _testDir;
    private readonly StoragePathService _storagePath;
    private readonly SqliteProcessingRepository _repository;
    private readonly CoordinateValidator _validator;
    private readonly RateLimiter _rateLimiter;

    public HarnessAcceptanceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "iot_harness_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _storagePath = new StoragePathService(_testDir);
        _repository = new SqliteProcessingRepository(_storagePath);
        _validator = new CoordinateValidator();
        _rateLimiter = new RateLimiter(0, 1);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    [Fact]
    public async Task TC25_Invalid_File_Returns_Clear_Error_Without_Crash()
    {
        var nonExistent = Path.Combine(_testDir, "missing_file.csv");
        var reader = new CsvFileReader();

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => reader.InspectAsync(nonExistent));
        Assert.NotNull(ex.Message);
        Assert.Contains("não encontrado", ex.Message);
    }

    [Fact]
    public async Task TC21_External_Config_Loaded_And_Updated_Without_Recompiling()
    {
        var configPath = Path.Combine(_testDir, "custom_appsettings.json");
        var initialJson = "{\"Geocoding\":{\"Provider\":\"Nominatim\",\"RequestIntervalMs\":1500}}";
        await File.WriteAllTextAsync(configPath, initialJson, Encoding.UTF8);

        var configService = new ConfigurationService(configPath);
        Assert.Equal("Nominatim", configService.Geocoding.Provider);
        Assert.Equal(1500, configService.Geocoding.RequestIntervalMs);

        // Altera externamente sem recompilar
        var updatedJson = "{\"Geocoding\":{\"Provider\":\"CustomProvider\",\"RequestIntervalMs\":2000}}";
        await File.WriteAllTextAsync(configPath, updatedJson, Encoding.UTF8);

        configService.Reload();
        Assert.Equal("CustomProvider", configService.Geocoding.Provider);
        Assert.Equal(2000, configService.Geocoding.RequestIntervalMs);
    }
}
