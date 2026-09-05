using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Infrastructure.Storage;

namespace IoT_lat_lon_processor.UI.ViewModels;

public partial class ResultViewModel : ObservableObject, IQueryAttributable
{
    private readonly IFileLauncherService _launcher;
    private readonly IStoragePathService _storagePathService;

    [ObservableProperty]
    private string _jobId = string.Empty;

    [ObservableProperty]
    private string _inputFileName = string.Empty;

    [ObservableProperty]
    private string? _outputFilePath;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _statusColor = "Green";

    [ObservableProperty]
    private long _totalRows;

    [ObservableProperty]
    private long _validRows;

    [ObservableProperty]
    private long _invalidRows;

    [ObservableProperty]
    private long _swappedRows;

    [ObservableProperty]
    private long _successRows;

    [ObservableProperty]
    private long _notFoundRows;

    [ObservableProperty]
    private long _errorRows;

    [ObservableProperty]
    private long _cacheHits;

    [ObservableProperty]
    private long _requestCount;

    [ObservableProperty]
    private string _durationFormatted = string.Empty;

    public ResultViewModel(IFileLauncherService launcher, IStoragePathService storagePathService)
    {
        _launcher = launcher;
        _storagePathService = storagePathService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ProcessingSummary", out var sumObj) && sumObj is ProcessingSummary summary)
        {
            LoadSummary(summary);
        }
    }

    public void LoadSummary(ProcessingSummary summary)
    {
        JobId = summary.JobId;
        InputFileName = summary.InputFileName;
        OutputFilePath = summary.OutputFilePath;
        Status = summary.Status switch
        {
            JobStatus.Completed => "Concluído com Sucesso",
            JobStatus.Cancelled => "Cancelado",
            JobStatus.Failed => "Falha no Processamento",
            _ => summary.Status.ToString()
        };

        StatusColor = summary.Status switch
        {
            JobStatus.Completed => "#2E7D32",
            JobStatus.Cancelled => "#F57C00",
            JobStatus.Failed => "#C62828",
            _ => "Gray"
        };

        TotalRows = summary.TotalRows;
        ValidRows = summary.ValidRows;
        InvalidRows = summary.InvalidRows;
        SwappedRows = summary.SwappedRows;
        SuccessRows = summary.SuccessRows;
        NotFoundRows = summary.NotFoundRows;
        ErrorRows = summary.ErrorRows;
        CacheHits = summary.CacheHits;
        RequestCount = summary.RequestCount;
        DurationFormatted = $"{summary.Duration.Minutes:D2}m {summary.Duration.Seconds:D2}s ({summary.Duration.TotalMilliseconds:N0} ms)";
    }

    [RelayCommand]
    public void OpenFile()
    {
        if (!string.IsNullOrEmpty(OutputFilePath) && File.Exists(OutputFilePath))
        {
            _launcher.OpenFile(OutputFilePath);
        }
    }

    [RelayCommand]
    public void OpenFolder()
    {
        if (!string.IsNullOrEmpty(OutputFilePath))
        {
            var dir = Path.GetDirectoryName(OutputFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                _launcher.OpenFolder(dir);
            }
        }
    }

    [RelayCommand]
    public void ViewLog()
    {
        if (string.IsNullOrEmpty(JobId)) return;

        // Procura o arquivo de log no diretório do job
        var now = DateTime.UtcNow;
        var logsDir = _storagePathService.GetJobLogsDirectory(JobId, now);
        var logFile = Path.Combine(logsDir, "processamento.jsonl");

        if (File.Exists(logFile))
        {
            _launcher.OpenFile(logFile);
        }
        else
        {
            // Tenta encontrar em subpastas de Processamentos
            var root = Path.Combine(_storagePathService.RootDirectory, "Processamentos");
            if (Directory.Exists(root))
            {
                var foundLog = Directory.GetFiles(root, "processamento.jsonl", SearchOption.AllDirectories)
                    .FirstOrDefault(f => f.Contains(JobId.Substring(0, Math.Min(8, JobId.Length))));

                if (foundLog != null)
                {
                    _launcher.OpenFile(foundLog);
                }
            }
        }
    }

    [RelayCommand]
    public async Task GoHomeAsync()
    {
        await Shell.Current.GoToAsync("//StartView");
    }
}
