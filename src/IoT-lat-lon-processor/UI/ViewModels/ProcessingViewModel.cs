using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IoT_lat_lon_processor.Application.Models;
using IoT_lat_lon_processor.Application.Services;
using IoT_lat_lon_processor.Domain.Models;

namespace IoT_lat_lon_processor.UI.ViewModels;

public partial class ProcessingViewModel : ObservableObject, IQueryAttributable
{
    private readonly IProcessingPipeline _pipeline;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private long _totalRows;

    [ObservableProperty]
    private long _processedRows;

    [ObservableProperty]
    private long _successCount;

    [ObservableProperty]
    private long _notFoundCount;

    [ObservableProperty]
    private long _errorCount;

    [ObservableProperty]
    private long _cacheHitCount;

    [ObservableProperty]
    private long _requestCount;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _elapsedFormatted = "00:00";

    [ObservableProperty]
    private string _statusMessage = "Iniciando processamento...";

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _canCancel = true;

    public ProcessingViewModel(IProcessingPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ProcessingRequest", out var reqObj) && reqObj is ProcessingRequest request)
        {
            _ = StartPipelineAsync(request);
        }
        else if (query.TryGetValue("ResumeJobId", out var jobIdObj) && jobIdObj is string jobId)
        {
            _ = ResumePipelineAsync(jobId);
        }
    }

    private async Task StartPipelineAsync(ProcessingRequest request)
    {
        _cts = new CancellationTokenSource();
        IsProcessing = true;
        CanCancel = true;
        StatusMessage = "Processando arquivo...";

        var progress = new Progress<ProcessingProgressUpdate>(update =>
        {
            TotalRows = update.TotalRows;
            ProcessedRows = update.ProcessedRows;
            SuccessCount = update.SuccessCount;
            NotFoundCount = update.NotFoundCount;
            ErrorCount = update.ErrorCount;
            CacheHitCount = update.CacheHitCount;
            RequestCount = update.RequestCount;
            ProgressPercentage = update.Percentage / 100.0;
            ElapsedFormatted = $"{update.Elapsed:mm\\:ss}";
            StatusMessage = update.StatusMessage;
        });

        try
        {
            var summary = await _pipeline.ExecuteAsync(request, progress, _cts.Token);
            await NavigateToResultAsync(summary);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Processamento cancelado pelo usuário.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro durante o processamento: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            CanCancel = false;
        }
    }

    private async Task ResumePipelineAsync(string jobId)
    {
        _cts = new CancellationTokenSource();
        IsProcessing = true;
        CanCancel = true;
        StatusMessage = "Retomando processamento...";

        var progress = new Progress<ProcessingProgressUpdate>(update =>
        {
            TotalRows = update.TotalRows;
            ProcessedRows = update.ProcessedRows;
            SuccessCount = update.SuccessCount;
            NotFoundCount = update.NotFoundCount;
            ErrorCount = update.ErrorCount;
            CacheHitCount = update.CacheHitCount;
            RequestCount = update.RequestCount;
            ProgressPercentage = update.Percentage / 100.0;
            ElapsedFormatted = $"{update.Elapsed:mm\\:ss}";
            StatusMessage = update.StatusMessage;
        });

        try
        {
            var summary = await _pipeline.ResumeAsync(jobId, progress, _cts.Token);
            await NavigateToResultAsync(summary);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao retomar processamento: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            CanCancel = false;
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            StatusMessage = "Cancelando processamento...";
            CanCancel = false;
            _cts.Cancel();
        }
    }

    private async Task NavigateToResultAsync(ProcessingSummary summary)
    {
        var navParam = new Dictionary<string, object>
        {
            { "ProcessingSummary", summary }
        };

        await Shell.Current.GoToAsync("ResultView", navParam);
    }
}
