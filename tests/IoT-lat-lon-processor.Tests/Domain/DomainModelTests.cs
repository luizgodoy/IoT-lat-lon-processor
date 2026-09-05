using IoT_lat_lon_processor.Domain.Models;
using Xunit;

namespace IoT_lat_lon_processor.Tests.Domain;

public class DomainModelTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(-90, -180, true)]
    [InlineData(90, 180, true)]
    [InlineData(-23.55, -46.63, true)]
    [InlineData(-90.0001, 0, false)]
    [InlineData(90.0001, 0, false)]
    [InlineData(0, -180.0001, false)]
    [InlineData(0, 180.0001, false)]
    public void Coordinate_Validity_CalculatedCorrectly(decimal lat, decimal lon, bool expectedValid)
    {
        var coord = new Coordinate(lat, lon);
        Assert.Equal(expectedValid, coord.IsValid);
    }

    [Fact]
    public void Coordinate_Swap_InvertsAxesCorrectly()
    {
        var coord = new Coordinate(120m, -45m);
        var swapped = coord.Swap();

        Assert.Equal(-45m, swapped.Latitude);
        Assert.Equal(120m, swapped.Longitude);
        Assert.False(coord.IsValid);
        Assert.True(swapped.IsValid);
    }

    [Fact]
    public void Coordinate_GetCacheKey_RoundsAndNormalizesProvider()
    {
        var coord = new Coordinate(-23.550520123m, -46.633309876m);
        var key = coord.GetCacheKey(precision: 6, provider: " Nominatim ");

        Assert.Equal("-23.550520:-46.633310:nominatim", key);
    }

    [Fact]
    public void CoordinateValidationResult_ProbableSwap_MarksCanSwapAndRequiresConfirmation()
    {
        var original = new Coordinate(120m, -45m);
        var suggested = original.Swap();

        var result = CoordinateValidationResult.ProbableSwap(original, suggested);

        Assert.False(result.IsValid);
        Assert.True(result.CanSwap);
        Assert.True(result.RequiresConfirmation);
        Assert.Equal(CoordinateStatus.ProbableSwapDetected, result.Status);
    }

    [Fact]
    public void CoordinateValidationResult_Ambiguous_RequiresConfirmationWithoutAutoSwap()
    {
        var original = new Coordinate(-23.55m, -46.63m);
        var result = CoordinateValidationResult.Ambiguous(original);

        Assert.True(result.IsValid);
        Assert.False(result.CanSwap);
        Assert.True(result.RequiresConfirmation);
        Assert.True(result.IsAmbiguous);
        Assert.Equal(CoordinateStatus.Ambiguous, result.Status);
    }

    [Fact]
    public void GeocodingResult_TransientError_IdentifiedProperly()
    {
        var rateLimited = GeocodingResult.RateLimited("Nominatim", TimeSpan.FromSeconds(2));
        var timeout = GeocodingResult.TransientError("Nominatim", GeocodingErrorCodes.Timeout, "Timeout", 408);
        var notFound = GeocodingResult.NotFound("Nominatim");
        var permanent = GeocodingResult.PermanentError("Nominatim", GeocodingErrorCodes.Unauthorized, "Unauthorized", 401);

        Assert.True(rateLimited.IsTransientError);
        Assert.True(timeout.IsTransientError);
        Assert.False(notFound.IsTransientError);
        Assert.False(permanent.IsTransientError);
    }

    [Fact]
    public void ProcessingSummary_CreatedFromJob_TransfersMetricsAccurately()
    {
        var job = new ProcessingJob
        {
            Id = "job-123",
            InputFileName = "input.csv",
            OutputFilePath = "output.csv",
            Provider = "Nominatim",
            Status = JobStatus.Completed,
            TotalRows = 100,
            ValidRows = 90,
            InvalidRows = 10,
            SwappedRows = 5,
            SuccessRows = 85,
            NotFoundRows = 5,
            ErrorRows = 0,
            CacheHits = 40,
            RequestCount = 50,
            StartedAt = DateTime.UtcNow.AddMinutes(-2),
            FinishedAt = DateTime.UtcNow
        };

        var summary = ProcessingSummary.FromJob(job, rateLimitPauses: 3);

        Assert.Equal("job-123", summary.JobId);
        Assert.Equal(100, summary.TotalRows);
        Assert.Equal(90, summary.ValidRows);
        Assert.Equal(10, summary.InvalidRows);
        Assert.Equal(5, summary.SwappedRows);
        Assert.Equal(85, summary.SuccessRows);
        Assert.Equal(5, summary.NotFoundRows);
        Assert.Equal(0, summary.ErrorRows);
        Assert.Equal(40, summary.CacheHits);
        Assert.Equal(50, summary.RequestCount);
        Assert.Equal(3, summary.RateLimitPauses);
        Assert.Equal(JobStatus.Completed, summary.Status);
        Assert.True(summary.Duration > TimeSpan.Zero);
    }
}
