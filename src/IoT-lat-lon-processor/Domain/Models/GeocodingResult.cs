namespace IoT_lat_lon_processor.Domain.Models;

/// <summary>
/// Código de erro padronizado para operações de geocodificação reversa.
/// </summary>
public static class GeocodingErrorCodes
{
    public const string InvalidCoordinate = "invalid_coordinate";
    public const string NotFound = "not_found";
    public const string Timeout = "timeout";
    public const string RateLimited = "rate_limited";
    public const string Unauthorized = "unauthorized";
    public const string ProviderError = "provider_error";
    public const string NetworkError = "network_error";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// Resultado da operação de geocodificação reversa para uma coordenada.
/// </summary>
public sealed class GeocodingResult
{
    public bool Success { get; init; }
    public string? DisplayAddress { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int? HttpStatus { get; init; }
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Indica se o erro é transitório e passível de nova tentativa (retry).
    /// </summary>
    public bool IsTransientError => ErrorCode is GeocodingErrorCodes.Timeout
                                 or GeocodingErrorCodes.RateLimited
                                 or GeocodingErrorCodes.NetworkError
                                 or GeocodingErrorCodes.ProviderError;

    public static GeocodingResult Succeeded(string displayAddress, string provider) => new()
    {
        Success = true,
        DisplayAddress = displayAddress,
        Provider = provider
    };

    public static GeocodingResult NotFound(string provider, string? message = "Endereço não localizado para a coordenada informada.") => new()
    {
        Success = false,
        Provider = provider,
        ErrorCode = GeocodingErrorCodes.NotFound,
        ErrorMessage = message,
        HttpStatus = 404
    };

    public static GeocodingResult RateLimited(string provider, TimeSpan? retryAfter = null, string? message = "Limite de requisições atingido (HTTP 429).") => new()
    {
        Success = false,
        Provider = provider,
        ErrorCode = GeocodingErrorCodes.RateLimited,
        ErrorMessage = message,
        HttpStatus = 429,
        RetryAfter = retryAfter
    };

    public static GeocodingResult TransientError(string provider, string errorCode, string message, int? httpStatus = null, TimeSpan? retryAfter = null) => new()
    {
        Success = false,
        Provider = provider,
        ErrorCode = errorCode,
        ErrorMessage = message,
        HttpStatus = httpStatus,
        RetryAfter = retryAfter
    };

    public static GeocodingResult PermanentError(string provider, string errorCode, string message, int? httpStatus = null) => new()
    {
        Success = false,
        Provider = provider,
        ErrorCode = errorCode,
        ErrorMessage = message,
        HttpStatus = httpStatus
    };

    public static GeocodingResult Cancelled(string provider, string message = "Operação cancelada.") => new()
    {
        Success = false,
        Provider = provider,
        ErrorCode = GeocodingErrorCodes.Cancelled,
        ErrorMessage = message
    };
}
