using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using IoT_lat_lon_processor.Infrastructure.Storage;

namespace IoT_lat_lon_processor.UI.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IProcessingRepository _repository;
    private readonly IFileLauncherService _launcher;
    private readonly IStoragePathService _storagePathService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedStatus = "Todos";

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private ObservableCollection<ProcessingJob> _jobs = [];

    [ObservableProperty]
    private ProcessingJob? _selectedJob;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<string> StatusOptions { get; } =
    [
        "Todos", "Completed", "Cancelled", "Failed"
    ];

    public HistoryViewModel(
        IProcessingRepository repository,
        IFileLauncherService launcher,
        IStoragePathService storagePathService)
    {
        _repository = repository;
        _launcher = launcher;
        _storagePathService = storagePathService;
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        IsBusy = true;
        try
        {
            JobStatus? statusFilter = null;
            if (Enum.TryParse<JobStatus>(SelectedStatus, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            var results = await _repository.SearchJobsAsync(
                text: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                from: StartDate,
                to: EndDate?.AddDays(1),
                status: statusFilter);

            Jobs.Clear();
            foreach (var j in results)
            {
                Jobs.Add(j);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenOutputFile(ProcessingJob? job)
    {
        job ??= SelectedJob;
        if (job != null && !string.IsNullOrEmpty(job.OutputFilePath) && File.Exists(job.OutputFilePath))
        {
            _launcher.OpenFile(job.OutputFilePath);
        }
    }

    [RelayCommand]
    public void OpenJobFolder(ProcessingJob? job)
    {
        job ??= SelectedJob;
        if (job != null)
        {
            var dir = !string.IsNullOrEmpty(job.OutputFilePath) ? Path.GetDirectoryName(job.OutputFilePath) : null;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                dir = _storagePathService.GetJobDirectory(job.Id, job.StartedAt);
            }

            _launcher.OpenFolder(dir);
        }
    }

    [RelayCommand]
    public void OpenJobLog(ProcessingJob? job)
    {
        job ??= SelectedJob;
        if (job == null) return;

        var logsDir = _storagePathService.GetJobLogsDirectory(job.Id, job.StartedAt);
        var logFile = Path.Combine(logsDir, "processamento.jsonl");

        if (File.Exists(logFile))
        {
            _launcher.OpenFile(logFile);
        }
        else
        {
            var root = Path.Combine(_storagePathService.RootDirectory, "Processamentos");
            if (Directory.Exists(root))
            {
                var foundLog = Directory.GetFiles(root, "processamento.jsonl", SearchOption.AllDirectories)
                    .FirstOrDefault(f => f.Contains(job.Id.Substring(0, Math.Min(8, job.Id.Length))));

                if (foundLog != null)
                {
                    _launcher.OpenFile(foundLog);
                }
            }
        }
    }

    [RelayCommand]
    public async Task ResumeJobAsync(ProcessingJob? job)
    {
        job ??= SelectedJob;
        if (job == null) return;

        var navParam = new Dictionary<string, object>
        {
            { "ResumeJobId", job.Id }
        };

        await Shell.Current.GoToAsync("ProcessingView", navParam);
    }

    [RelayCommand]
    public async Task DeleteJobAsync(ProcessingJob? job)
    {
        job ??= SelectedJob;
        if (job == null) return;

        var confirm = await Microsoft.Maui.Controls.Application.Current?.Windows[0].Page?.DisplayAlertAsync(
            "Excluir Histórico",
            $"Deseja realmente remover o registro de processamento do arquivo '{job.InputFileName}'?",
            "Sim, excluir",
            "Cancelar")!;

        if (confirm)
        {
            await _repository.DeleteJobAsync(job.Id);
            Jobs.Remove(job);
        }
    }
}
