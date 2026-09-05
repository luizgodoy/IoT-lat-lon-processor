using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using IoT_lat_lon_processor.Infrastructure.Storage;
using Xunit;

namespace IoT_lat_lon_processor.Tests.Persistence;

public class SqliteRepositoryTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _dbPath;
    private readonly SqliteProcessingRepository _repository;

    public SqliteRepositoryTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "iot_sqlite_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _dbPath = Path.Combine(_testDir, "test.db");
        _repository = new SqliteProcessingRepository(_dbPath);
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
        catch
        {
            // Ignora falhas de exclusão de arquivos bloqueados pelo SQLite em teste
        }
    }

    [Fact]
    public async Task SaveJob_And_GetJob_RoundTrips_Metrics_Correctly()
    {
        var job = new ProcessingJob
        {
            Id = "job-abc-123",
            InputFileName = "sensores.csv",
            InputFilePath = @"C:\data\sensores.csv",
            OutputFilePath = @"C:\data\sensores_processado.csv",
            LatitudeColumn = "lat",
            LongitudeColumn = "lon",
            Provider = "Nominatim",
            Status = JobStatus.Completed,
            TotalRows = 50,
            ValidRows = 45,
            InvalidRows = 5,
            SwappedRows = 2,
            SuccessRows = 43,
            NotFoundRows = 2,
            ErrorRows = 0,
            CacheHits = 20,
            RequestCount = 25,
            DurationMs = 15400
        };

        await _repository.SaveJobAsync(job);

        var retrieved = await _repository.GetJobAsync("job-abc-123");

        Assert.NotNull(retrieved);
        Assert.Equal("job-abc-123", retrieved.Id);
        Assert.Equal("sensores.csv", retrieved.InputFileName);
        Assert.Equal(JobStatus.Completed, retrieved.Status);
        Assert.Equal(50, retrieved.TotalRows);
        Assert.Equal(43, retrieved.SuccessRows);
        Assert.Equal(20, retrieved.CacheHits);
        Assert.Equal(15400, retrieved.DurationMs);
    }

    [Fact]
    public async Task TC16_SearchJobs_Filters_By_Text_Status_And_Date()
    {
        var now = DateTime.UtcNow;

        var job1 = new ProcessingJob
        {
            Id = "job-1",
            InputFileName = "telemetria_frota.csv",
            StartedAt = now.AddDays(-2),
            Status = JobStatus.Completed
        };

        var job2 = new ProcessingJob
        {
            Id = "job-2",
            InputFileName = "sensores_iot.xlsx",
            StartedAt = now.AddDays(-1),
            Status = JobStatus.Cancelled
        };

        var job3 = new ProcessingJob
        {
            Id = "job-3",
            InputFileName = "rastreamento.csv",
            StartedAt = now,
            Status = JobStatus.Completed
        };

        await _repository.SaveJobAsync(job1);
        await _repository.SaveJobAsync(job2);
        await _repository.SaveJobAsync(job3);

        // Busca por texto
        var textResults = await _repository.SearchJobsAsync(text: "frota");
        Assert.Single(textResults);
        Assert.Equal("job-1", textResults[0].Id);

        // Busca por status
        var cancelledResults = await _repository.SearchJobsAsync(status: JobStatus.Cancelled);
        Assert.Single(cancelledResults);
        Assert.Equal("job-2", cancelledResults[0].Id);

        // Busca por período
        var dateResults = await _repository.SearchJobsAsync(from: now.AddHours(-12));
        Assert.Single(dateResults);
        Assert.Equal("job-3", dateResults[0].Id);
    }

    [Fact]
    public async Task SaveRowsBatch_And_GetCompletedRowAddresses_Supports_Checkpoint()
    {
        var jobId = "job-check-1";

        var rows = new List<ProcessingRow>
        {
            new()
            {
                JobId = jobId,
                SourceRowNumber = 1,
                RawLatitude = "-23.55",
                RawLongitude = "-46.63",
                NormalizedLatitude = -23.55m,
                NormalizedLongitude = -46.63m,
                Address = "Praça da Sé",
                Status = RowProcessingStatus.Success,
                Attempts = 1
            },
            new()
            {
                JobId = jobId,
                SourceRowNumber = 2,
                RawLatitude = "91.00",
                RawLongitude = "-45.00",
                Status = RowProcessingStatus.InvalidCoordinate,
                ErrorMessage = "Latitude fora do limite"
            }
        };

        await _repository.SaveRowsBatchAsync(rows);

        var retrievedRows = await _repository.GetRowsAsync(jobId);
        Assert.Equal(2, retrievedRows.Count);

        var completedMap = await _repository.GetCompletedRowAddressesAsync(jobId);
        Assert.Equal(2, completedMap.Count);
        Assert.Equal("Praça da Sé", completedMap[1]);
        Assert.Null(completedMap[2]);
    }

    [Fact]
    public async Task TC09_CoordinateCache_Stores_And_Retrieves_Normalized_Addresses()
    {
        var coord = new Coordinate(-23.55052012m, -46.63330891m);
        var provider = "Nominatim";
        var precision = 6;
        var address = "Catedral da Sé, São Paulo, SP";

        // Inicialmente não está no cache
        var initial = await _repository.GetAddressAsync(coord, precision, provider);
        Assert.Null(initial);

        // Salva no cache
        await _repository.SetAddressAsync(coord, precision, provider, address, found: true);

        // Recupera com outra instância de coordenada que arredonda para a mesma chave
        var sameRoundedCoord = new Coordinate(-23.55052044m, -46.63330866m);
        var cachedAddress = await _repository.GetAddressAsync(sameRoundedCoord, precision, provider);

        Assert.NotNull(cachedAddress);
        Assert.Equal(address, cachedAddress);
    }

    [Fact]
    public void StoragePathService_Generates_Correct_Folder_Hierarchy()
    {
        var pathService = new StoragePathService(_testDir);
        var started = new DateTime(2026, 9, 4, 8, 21, 0, DateTimeKind.Utc);
        var jobId = "ABC123456789";

        var jobDir = pathService.GetJobDirectory(jobId, started);
        var inputDir = pathService.GetJobInputDirectory(jobId, started);
        var outputDir = pathService.GetJobOutputDirectory(jobId, started);
        var logsDir = pathService.GetJobLogsDirectory(jobId, started);
        var stateDir = pathService.GetJobStateDirectory(jobId, started);

        Assert.Contains(Path.Combine("Processamentos", "2026", "09", "20260904-082100_ABC12345"), jobDir);
        Assert.True(Directory.Exists(inputDir));
        Assert.True(Directory.Exists(outputDir));
        Assert.True(Directory.Exists(logsDir));
        Assert.True(Directory.Exists(stateDir));
    }
}
