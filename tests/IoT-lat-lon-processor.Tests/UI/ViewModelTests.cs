using IoT_lat_lon_processor.Configuration;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Domain.Services;
using IoT_lat_lon_processor.Infrastructure.FileImport;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using IoT_lat_lon_processor.Infrastructure.Storage;
using IoT_lat_lon_processor.UI.ViewModels;
using Xunit;

namespace IoT_lat_lon_processor.Tests.UI;

public class ViewModelTests : IDisposable
{
    private readonly string _testDir;

    public ViewModelTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "iot_vm_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
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
    public async Task StartViewModel_Analyze_Without_File_Sets_ErrorMessage()
    {
        var vm = new StartViewModel([]);
        await vm.AnalyzeAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("Selecione um arquivo", vm.ErrorMessage);
    }

    [Fact]
    public void MappingViewModel_Swap_Detection_Updates_Diagnostics()
    {
        var validator = new CoordinateValidator();
        var vm = new MappingViewModel(validator, []);

        var schema = new FileSchema
        {
            FileName = "swap_sample.csv",
            Columns = ["id", "lat", "lon"],
            SampleRows =
            [
                ["1", "120", "-45"],
                ["2", "125", "-44"]
            ],
            SuggestedLatitudeColumn = "lat",
            SuggestedLongitudeColumn = "lon"
        };

        vm.LoadSchema(schema);

        Assert.True(vm.ShowSwapOption);
        Assert.Contains("parecem estar invertidos", vm.DiagnosticMessage);

        // Usuário ativa a confirmação de swap
        vm.SwapAxes = true;
        Assert.Contains("válidas", vm.DiagnosticMessage);
    }

    [Fact]
    public void MappingViewModel_Ambiguous_Alerts_User()
    {
        var validator = new CoordinateValidator();
        var vm = new MappingViewModel(validator, []);

        var schema = new FileSchema
        {
            FileName = "ambiguous.csv",
            Columns = ["id", "latitude", "longitude"],
            SampleRows =
            [
                ["1", "-23.55", "-46.63"]
            ],
            SuggestedLatitudeColumn = "latitude",
            SuggestedLongitudeColumn = "longitude"
        };

        vm.LoadSchema(schema);

        Assert.True(vm.ShowAmbiguousAlert);
        Assert.False(vm.ShowSwapOption);
        Assert.Contains("matematicamente válidos", vm.DiagnosticMessage);
    }

    [Fact]
    public async Task SettingsViewModel_Warns_On_Aggressive_Rate_Limits()
    {
        var configFile = Path.Combine(_testDir, "test_settings.json");
        var configService = new ConfigurationService(configFile);
        var vm = new SettingsViewModel(configService);

        // Padrão seguro
        Assert.Null(vm.WarningMessage);

        // Altera para intervalo agressivo
        vm.RequestIntervalMs = 500;
        Assert.NotNull(vm.WarningMessage);
        Assert.Contains("política de uso", vm.WarningMessage);

        // Salva configuração alterada
        await vm.SaveAsync();

        // Recarrega do arquivo
        var reloaded = new ConfigurationService(configFile);
        Assert.Equal(500, reloaded.Geocoding.RequestIntervalMs);
    }

    [Fact]
    public async Task HistoryViewModel_Loads_And_Filters_Jobs()
    {
        var dbPath = Path.Combine(_testDir, "history_test.db");
        var repo = new SqliteProcessingRepository(dbPath);
        var launcher = new MockFileLauncher();
        var pathService = new StoragePathService(_testDir);

        await repo.SaveJobAsync(new ProcessingJob
        {
            Id = "job-hist-1",
            InputFileName = "rotas_sp.csv",
            Status = JobStatus.Completed
        });
        await repo.SaveJobAsync(new ProcessingJob
        {
            Id = "job-hist-2",
            InputFileName = "rotas_rj.csv",
            Status = JobStatus.Cancelled
        });

        var vm = new HistoryViewModel(repo, launcher, pathService);
        await vm.LoadHistoryAsync();

        Assert.Equal(2, vm.Jobs.Count);

        // Filtra por texto
        vm.SearchText = "sp";
        await vm.LoadHistoryAsync();
        Assert.Single(vm.Jobs);
        Assert.Equal("job-hist-1", vm.Jobs[0].Id);
    }

    private sealed class MockFileLauncher : IFileLauncherService
    {
        public List<string> OpenedFiles { get; } = [];
        public List<string> OpenedFolders { get; } = [];

        public void OpenFile(string filePath) => OpenedFiles.Add(filePath);
        public void OpenFolder(string folderPath) => OpenedFolders.Add(folderPath);
    }
}
