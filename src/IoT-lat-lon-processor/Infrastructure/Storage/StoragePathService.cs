namespace IoT_lat_lon_processor.Infrastructure.Storage;

public interface IStoragePathService
{
    string RootDirectory { get; }
    string DatabasePath { get; }
    string GetJobDirectory(string jobId, DateTime startedAt);
    string GetJobInputDirectory(string jobId, DateTime startedAt);
    string GetJobOutputDirectory(string jobId, DateTime startedAt);
    string GetJobLogsDirectory(string jobId, DateTime startedAt);
    string GetJobStateDirectory(string jobId, DateTime startedAt);
}

public sealed class StoragePathService : IStoragePathService
{
    public string RootDirectory { get; }
    public string DatabasePath => Path.Combine(RootDirectory, "iot_processor.db");

    public StoragePathService(string? customRootDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(customRootDirectory))
        {
            RootDirectory = customRootDirectory;
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            RootDirectory = Path.Combine(localAppData, "IoT-lat-lon-processor");
        }

        Directory.CreateDirectory(RootDirectory);
    }

    public string GetJobDirectory(string jobId, DateTime startedAt)
    {
        var shortId = jobId.Length > 8 ? jobId.Substring(0, 8) : jobId;
        var folderName = $"{startedAt:yyyyMMdd-HHmmss}_{shortId}";
        var year = startedAt.ToString("yyyy");
        var month = startedAt.ToString("MM");

        var path = Path.Combine(RootDirectory, "Processamentos", year, month, folderName);
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetJobInputDirectory(string jobId, DateTime startedAt)
    {
        var path = Path.Combine(GetJobDirectory(jobId, startedAt), "input");
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetJobOutputDirectory(string jobId, DateTime startedAt)
    {
        var path = Path.Combine(GetJobDirectory(jobId, startedAt), "output");
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetJobLogsDirectory(string jobId, DateTime startedAt)
    {
        var path = Path.Combine(GetJobDirectory(jobId, startedAt), "logs");
        Directory.CreateDirectory(path);
        return path;
    }

    public string GetJobStateDirectory(string jobId, DateTime startedAt)
    {
        var path = Path.Combine(GetJobDirectory(jobId, startedAt), "state");
        Directory.CreateDirectory(path);
        return path;
    }
}
