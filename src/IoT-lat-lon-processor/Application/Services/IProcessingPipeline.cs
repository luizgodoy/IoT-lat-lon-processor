using IoT_lat_lon_processor.Application.Models;
using IoT_lat_lon_processor.Domain.Models;

namespace IoT_lat_lon_processor.Application.Services;

public interface IProcessingPipeline
{
    Task<ProcessingSummary> ExecuteAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgressUpdate>? progress = null,
        CancellationToken ct = default);

    Task<ProcessingSummary> ResumeAsync(
        string jobId,
        IProgress<ProcessingProgressUpdate>? progress = null,
        CancellationToken ct = default);
}
