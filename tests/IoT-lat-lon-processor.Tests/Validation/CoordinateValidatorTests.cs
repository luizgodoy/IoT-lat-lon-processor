using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Domain.Services;
using Xunit;

namespace IoT_lat_lon_processor.Tests.Validation;

public class CoordinateValidatorTests
{
    private readonly CoordinateValidator _validator = new();

    [Fact]
    public void TC05_Latitude_91_Is_Invalid()
    {
        var result = _validator.Validate(91m, -45m);

        Assert.False(result.IsValid);
        Assert.Equal(CoordinateStatus.InvalidLatitude, result.Status);
    }

    [Fact]
    public void TC06_Longitude_181_Is_Invalid()
    {
        var result = _validator.Validate(-23.55m, 181m);

        Assert.False(result.IsValid);
        Assert.Equal(CoordinateStatus.InvalidLongitude, result.Status);
    }

    [Fact]
    public void TC07_Lat120_LonMinus45_Detects_Probable_Swap()
    {
        var result = _validator.DetectPossibleSwap(120m, -45m);

        Assert.False(result.IsValid);
        Assert.Equal(CoordinateStatus.ProbableSwapDetected, result.Status);
        Assert.True(result.CanSwap);
        Assert.True(result.RequiresConfirmation);
        Assert.NotNull(result.Suggested);
        Assert.Equal(-45m, result.Suggested.Value.Latitude);
        Assert.Equal(120m, result.Suggested.Value.Longitude);
        Assert.True(result.Suggested.Value.IsValid);

        // Verifica também via ParseAndValidate
        var parseResult = _validator.ParseAndValidate("120", "-45");
        Assert.Equal(CoordinateStatus.ProbableSwapDetected, parseResult.Status);
    }

    [Fact]
    public void TC08_LatMinus23_LonMinus46_Is_Ambiguous_Does_Not_Swap_Silently()
    {
        var result = _validator.DetectPossibleSwap(-23.55m, -46.63m);

        Assert.Equal(CoordinateStatus.Ambiguous, result.Status);
        Assert.True(result.IsValid);
        Assert.False(result.CanSwap);
        Assert.True(result.RequiresConfirmation);
        Assert.NotNull(result.Original);
        Assert.Equal(-23.55m, result.Original.Value.Latitude);
        Assert.Equal(-46.63m, result.Original.Value.Longitude);
    }

    [Fact]
    public void ParseAndValidate_Comma_Decimal_Separator_Parsed_Correctly()
    {
        var result = _validator.ParseAndValidate("-23,550520", "-46,633308");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Original);
        Assert.Equal(-23.550520m, result.Original.Value.Latitude);
        Assert.Equal(-46.633308m, result.Original.Value.Longitude);
    }

    [Fact]
    public void ParseAndValidate_Cardinal_Directions_Converted_Correctly()
    {
        var result = _validator.ParseAndValidate("23.550520 S", "46.633308 W");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Original);
        Assert.Equal(-23.550520m, result.Original.Value.Latitude);
        Assert.Equal(-46.633308m, result.Original.Value.Longitude);
    }

    [Theory]
    [InlineData("", "-46.63")]
    [InlineData("-23.55", "   ")]
    [InlineData(null, null)]
    public void ParseAndValidate_Empty_Inputs_Return_Empty_Status(string? lat, string? lon)
    {
        var result = _validator.ParseAndValidate(lat, lon);

        Assert.False(result.IsValid);
        Assert.Equal(CoordinateStatus.Empty, result.Status);
    }

    [Theory]
    [InlineData("invalid_lat", "-46.63")]
    [InlineData("-23.55", "corrupt_lon")]
    public void ParseAndValidate_NonNumeric_Inputs_Return_Unparseable(string lat, string lon)
    {
        var result = _validator.ParseAndValidate(lat, lon);

        Assert.False(result.IsValid);
        Assert.Equal(CoordinateStatus.Unparseable, result.Status);
    }

    [Fact]
    public void Validate_Both_Invalid_Returns_BothInvalid()
    {
        var result = _validator.Validate(150m, 250m);

        Assert.False(result.IsValid);
        Assert.Equal(CoordinateStatus.BothInvalid, result.Status);
        Assert.False(result.CanSwap);
    }

    [Fact]
    public void Validate_Unambiguous_Valid_Coordinate_Returns_Valid()
    {
        // Latitude=-23.55 (valid for both lat and lon), Longitude=-150 (valid for lon, invalid for lat!)
        // Swapping would make lat=-150 which is impossible!
        var result = _validator.Validate(-23.55m, -150m);

        Assert.True(result.IsValid);
        Assert.Equal(CoordinateStatus.Valid, result.Status);
        Assert.False(result.RequiresConfirmation);
    }
}
