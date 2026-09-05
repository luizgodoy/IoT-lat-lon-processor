using IoT_lat_lon_processor.Domain.Models;

namespace IoT_lat_lon_processor.Domain.Services;

/// <summary>
/// Contrato para validação, detecção de inversão e parsing de coordenadas geográficas.
/// </summary>
public interface ICoordinateValidator
{
    /// <summary>
    /// Valida numericamente um par de latitude e longitude.
    /// </summary>
    CoordinateValidationResult Validate(decimal latitude, decimal longitude);

    /// <summary>
    /// Analisa se duas coordenadas fornecidas estão invertidas ou em situação ambígua.
    /// </summary>
    CoordinateValidationResult DetectPossibleSwap(decimal first, decimal second);

    /// <summary>
    /// Realiza o parsing textual das coordenadas com suporte a vírgula/ponto e indicadores cardeais (N, S, L, O, W, E).
    /// </summary>
    CoordinateValidationResult ParseAndValidate(string? rawLatitude, string? rawLongitude);
}
