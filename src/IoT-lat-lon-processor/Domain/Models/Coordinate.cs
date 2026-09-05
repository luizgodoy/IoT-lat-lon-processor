using System.Globalization;

namespace IoT_lat_lon_processor.Domain.Models;

/// <summary>
/// Representa uma coordenada geográfica (Latitude e Longitude) com precisão decimal.
/// </summary>
public readonly record struct Coordinate(decimal Latitude, decimal Longitude)
{
    /// <summary>
    /// Limite mínimo de latitude: -90.
    /// </summary>
    public const decimal MinLatitude = -90m;

    /// <summary>
    /// Limite máximo de latitude: 90.
    /// </summary>
    public const decimal MaxLatitude = 90m;

    /// <summary>
    /// Limite mínimo de longitude: -180.
    /// </summary>
    public const decimal MinLongitude = -180m;

    /// <summary>
    /// Limite máximo de longitude: 180.
    /// </summary>
    public const decimal MaxLongitude = 180m;

    /// <summary>
    /// Verifica se a latitude está no intervalo [-90, 90].
    /// </summary>
    public bool IsLatitudeValid => Latitude >= MinLatitude && Latitude <= MaxLatitude;

    /// <summary>
    /// Verifica se a longitude está no intervalo [-180, 180].
    /// </summary>
    public bool IsLongitudeValid => Longitude >= MinLongitude && Longitude <= MaxLongitude;

    /// <summary>
    /// Verifica se ambos os eixos são válidos.
    /// </summary>
    public bool IsValid => IsLatitudeValid && IsLongitudeValid;

    /// <summary>
    /// Retorna uma nova coordenada com os eixos invertidos (Latitude vira Longitude e vice-versa).
    /// </summary>
    public Coordinate Swap() => new(Longitude, Latitude);

    /// <summary>
    /// Gera a chave de cache normalizada para a coordenada com precisão configurável e identificador do provedor.
    /// Formato: round(latitude, precision):round(longitude, precision):provider
    /// </summary>
    public string GetCacheKey(int precision = 6, string provider = "Nominatim")
    {
        var roundedLat = Math.Round(Latitude, precision, MidpointRounding.AwayFromZero);
        var roundedLon = Math.Round(Longitude, precision, MidpointRounding.AwayFromZero);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{roundedLat:F6}:{roundedLon:F6}:{provider.Trim().ToLowerInvariant()}");
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"({Latitude:G}, {Longitude:G})");
}
