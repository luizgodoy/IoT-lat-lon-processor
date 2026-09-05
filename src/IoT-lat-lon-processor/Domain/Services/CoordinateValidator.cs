using System.Globalization;
using System.Text.RegularExpressions;
using IoT_lat_lon_processor.Domain.Models;

namespace IoT_lat_lon_processor.Domain.Services;

/// <summary>
/// Implementação das regras de validação, detecção de inversão e parsing seguro de coordenadas.
/// </summary>
public sealed class CoordinateValidator : ICoordinateValidator
{
    private static readonly Regex CardinalSuffixRegex = new(@"\s*([NSEWLO])\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CoordinateValidationResult Validate(decimal latitude, decimal longitude)
    {
        var coord = new Coordinate(latitude, longitude);

        if (coord.IsValid)
        {
            return CoordinateValidationResult.Valid(coord);
        }

        var latInvalid = !coord.IsLatitudeValid;
        var lonInvalid = !coord.IsLongitudeValid;

        if (latInvalid && lonInvalid)
        {
            return CoordinateValidationResult.Invalid(
                CoordinateStatus.BothInvalid,
                $"Ambos os valores estão fora dos limites permitidos (Latitude {latitude:G} fora de [-90, 90] e Longitude {longitude:G} fora de [-180, 180]).",
                coord);
        }

        if (latInvalid)
        {
            return CoordinateValidationResult.Invalid(
                CoordinateStatus.InvalidLatitude,
                $"Latitude {latitude:G} está fora do intervalo permitido [-90, 90].",
                coord);
        }

        return CoordinateValidationResult.Invalid(
            CoordinateStatus.InvalidLongitude,
            $"Longitude {longitude:G} está fora do intervalo permitido [-180, 180].",
            coord);
    }

    public CoordinateValidationResult DetectPossibleSwap(decimal first, decimal second)
    {
        var original = new Coordinate(first, second);
        var swapped = original.Swap();

        var originalValid = original.IsValid;
        var swappedValid = swapped.IsValid;

        // Caso 1: Apenas a inversão é matematicamente válida (ex: Lat=120, Lon=-45)
        if (!originalValid && swappedValid)
        {
            return CoordinateValidationResult.ProbableSwap(original, swapped);
        }

        // Caso 2: Ambos os números cabem em ambos os eixos (ambos em [-90, 90], ex: -23.55 e -46.63)
        // Nunca inverter silenciosamente!
        if (originalValid && swappedValid && Math.Abs(first) <= 90m && Math.Abs(second) <= 90m)
        {
            return CoordinateValidationResult.Ambiguous(original);
        }

        // Caso 3: Apenas a ordem original é válida (ex: Lat=-23.55, Lon=-120)
        if (originalValid && !swappedValid)
        {
            return CoordinateValidationResult.Valid(original);
        }

        // Caso 4: Ambas as ordens são inválidas
        return Validate(first, second);
    }

    public CoordinateValidationResult ParseAndValidate(string? rawLatitude, string? rawLongitude)
    {
        if (string.IsNullOrWhiteSpace(rawLatitude) || string.IsNullOrWhiteSpace(rawLongitude))
        {
            return CoordinateValidationResult.Invalid(
                CoordinateStatus.Empty,
                "Valores de latitude ou longitude vazios.");
        }

        if (!TryParseSingleCoordinate(rawLatitude, isLatitudeAxis: true, out var lat, out var latError))
        {
            return CoordinateValidationResult.Invalid(
                CoordinateStatus.Unparseable,
                latError ?? $"Valor de latitude '{rawLatitude}' inválido.");
        }

        if (!TryParseSingleCoordinate(rawLongitude, isLatitudeAxis: false, out var lon, out var lonError))
        {
            return CoordinateValidationResult.Invalid(
                CoordinateStatus.Unparseable,
                lonError ?? $"Valor de longitude '{rawLongitude}' inválido.");
        }

        return DetectPossibleSwap(lat, lon);
    }

    private static bool TryParseSingleCoordinate(string raw, bool isLatitudeAxis, out decimal parsedValue, out string? errorMessage)
    {
        parsedValue = 0m;
        errorMessage = null;

        var cleaned = raw.Trim();
        if (string.IsNullOrEmpty(cleaned))
        {
            errorMessage = "Valor vazio.";
            return false;
        }

        // Remove caracteres de graus e aspas
        cleaned = cleaned.Replace("°", "").Replace("º", "").Replace("'", "").Replace("\"", "").Trim();

        // Verifica indicador cardeal (N, S, E, W, L, O)
        var isNegative = false;
        var cardinalMatch = CardinalSuffixRegex.Match(cleaned);
        if (cardinalMatch.Success)
        {
            var letter = cardinalMatch.Groups[1].Value.ToUpperInvariant();
            if (letter is "S" or "W" or "O")
            {
                isNegative = true;
            }
            cleaned = cleaned.Substring(0, cardinalMatch.Index).Trim();
        }

        // Normalização de separador decimal:
        // Se contiver vírgula e não tiver ponto, substitui vírgula por ponto (ex: -23,55 -> -23.55)
        if (cleaned.Contains(',') && !cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(',', '.');
        }
        else if (cleaned.Contains(',') && cleaned.Contains('.'))
        {
            // Se contiver ambos, verifica qual é o último separador
            var lastDot = cleaned.LastIndexOf('.');
            var lastComma = cleaned.LastIndexOf(',');

            if (lastDot > lastComma)
            {
                // Ponto é decimal: remove vírgula de milhar (ex: 1,234.56 -> 1234.56)
                cleaned = cleaned.Replace(",", "");
            }
            else
            {
                // Vírgula é decimal: remove ponto de milhar e troca vírgula por ponto (ex: 1.234,56 -> 1234.56)
                cleaned = cleaned.Replace(".", "").Replace(',', '.');
            }
        }

        if (!decimal.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            errorMessage = $"Não foi possível converter '{raw}' em um número decimal válido.";
            return false;
        }

        if (isNegative && value > 0)
        {
            value = -value;
        }

        parsedValue = value;
        return true;
    }
}
