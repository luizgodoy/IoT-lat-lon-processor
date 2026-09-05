namespace IoT_lat_lon_processor.Infrastructure.FileImport;

/// <summary>
/// Sugere de forma não-vinculante as colunas prováveis de latitude e longitude baseando-se no nome dos cabeçalhos.
/// </summary>
public static class ColumnSuggester
{
    private static readonly string[] LatitudePatterns =
    [
        "latitude", "lat", "lat_deg", "lat_rad", "latdec", "latitude_deg", "coord_lat", "lat_gps", "y"
    ];

    private static readonly string[] LongitudePatterns =
    [
        "longitude", "long", "lon", "lng", "lon_deg", "lon_rad", "londec", "longitude_deg", "coord_lon", "lon_gps", "x"
    ];

    public static string? SuggestLatitude(IEnumerable<string> columns)
    {
        var colList = columns.ToList();

        foreach (var pattern in LatitudePatterns)
        {
            var match = colList.FirstOrDefault(c => string.Equals(c.Trim(), pattern, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        foreach (var pattern in LatitudePatterns)
        {
            var match = colList.FirstOrDefault(c => c.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return null;
    }

    public static string? SuggestLongitude(IEnumerable<string> columns)
    {
        var colList = columns.ToList();

        foreach (var pattern in LongitudePatterns)
        {
            var match = colList.FirstOrDefault(c => string.Equals(c.Trim(), pattern, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        foreach (var pattern in LongitudePatterns)
        {
            var match = colList.FirstOrDefault(c => c.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return null;
    }
}
