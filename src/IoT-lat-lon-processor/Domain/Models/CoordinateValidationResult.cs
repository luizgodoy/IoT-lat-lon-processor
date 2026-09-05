namespace IoT_lat_lon_processor.Domain.Models;

/// <summary>
/// Classificação do status de validação de uma coordenada.
/// </summary>
public enum CoordinateStatus
{
    /// <summary>
    /// Coordenada válida e inequívoca (longitude fora do intervalo de latitude [-90, 90]).
    /// </summary>
    Valid,

    /// <summary>
    /// Ambos os valores são numericamente válidos nos dois eixos (ambos em [-90, 90]); não pode ser invertido silenciosamente.
    /// </summary>
    Ambiguous,

    /// <summary>
    /// Inversão de eixos provável e necessária detectada (ex: Latitude=120, Longitude=-45).
    /// </summary>
    ProbableSwapDetected,

    /// <summary>
    /// Latitude fora do intervalo [-90, 90].
    /// </summary>
    InvalidLatitude,

    /// <summary>
    /// Longitude fora do intervalo [-180, 180].
    /// </summary>
    InvalidLongitude,

    /// <summary>
    /// Tanto a latitude quanto a longitude estão fora dos limites aceitáveis.
    /// </summary>
    BothInvalid,

    /// <summary>
    /// Um ou ambos os valores estão vazios ou nulos.
    /// </summary>
    Empty,

    /// <summary>
    /// Formato numérico não pôde ser convertido.
    /// </summary>
    Unparseable
}

/// <summary>
/// Resultado da análise e validação de um par de coordenadas.
/// </summary>
public sealed class CoordinateValidationResult
{
    public CoordinateStatus Status { get; init; }

    /// <summary>
    /// Indica se a coordenada é matematicamente utilizável (Valid ou Ambiguous).
    /// </summary>
    public bool IsValid => Status is CoordinateStatus.Valid or CoordinateStatus.Ambiguous;

    /// <summary>
    /// Indica se uma inversão de eixos tornaria a coordenada válida.
    /// </summary>
    public bool CanSwap => Status == CoordinateStatus.ProbableSwapDetected && Suggested.HasValue && Suggested.Value.IsValid;

    /// <summary>
    /// Indica se o caso requer confirmação do usuário (inversão ou ambiguidade de eixos).
    /// </summary>
    public bool RequiresConfirmation => Status is CoordinateStatus.ProbableSwapDetected or CoordinateStatus.Ambiguous;

    /// <summary>
    /// Indica se ambos os valores cabem em ambos os eixos.
    /// </summary>
    public bool IsAmbiguous => Status == CoordinateStatus.Ambiguous;

    public string Message { get; init; } = string.Empty;
    public Coordinate? Original { get; init; }
    public Coordinate? Suggested { get; init; }

    public static CoordinateValidationResult Valid(Coordinate coordinate) => new()
    {
        Status = CoordinateStatus.Valid,
        Message = "Coordenada válida.",
        Original = coordinate,
        Suggested = coordinate
    };

    public static CoordinateValidationResult ProbableSwap(Coordinate original, Coordinate suggested) => new()
    {
        Status = CoordinateStatus.ProbableSwapDetected,
        Message = "Os valores parecem estar invertidos. Deseja utilizar Longitude como Latitude e Latitude como Longitude?",
        Original = original,
        Suggested = suggested
    };

    public static CoordinateValidationResult Ambiguous(Coordinate original) => new()
    {
        Status = CoordinateStatus.Ambiguous,
        Message = "Atenção: ambos os valores são matematicamente válidos nos dois eixos; confirme a ordem.",
        Original = original,
        Suggested = original
    };

    public static CoordinateValidationResult Invalid(CoordinateStatus status, string message, Coordinate? original = null) => new()
    {
        Status = status,
        Message = message,
        Original = original,
        Suggested = null
    };
}
