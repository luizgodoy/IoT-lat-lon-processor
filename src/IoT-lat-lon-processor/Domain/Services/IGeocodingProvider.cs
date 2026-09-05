using IoT_lat_lon_processor.Domain.Models;

namespace IoT_lat_lon_processor.Domain.Services;

/// <summary>
/// Contrato para provedores de geocodificação reversa (ex: Nominatim, Google, Mapbox).
/// </summary>
public interface IGeocodingProvider
{
    string Name { get; }

    /// <summary>
    /// Converte uma coordenada geográfica em endereço textual completo.
    /// </summary>
    Task<GeocodingResult> ReverseAsync(Coordinate coordinate, CancellationToken ct = default);
}
