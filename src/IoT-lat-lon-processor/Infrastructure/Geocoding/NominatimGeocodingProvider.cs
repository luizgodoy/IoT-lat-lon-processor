using System.Globalization;
using System.Net;
using System.Text.Json;
using IoT_lat_lon_processor.Configuration;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Domain.Services;
using IoT_lat_lon_processor.Infrastructure.Http;
using IoT_lat_lon_processor.Infrastructure.Persistence;

namespace IoT_lat_lon_processor.Infrastructure.Geocoding;

public sealed class NominatimGeocodingProvider : IGeocodingProvider
{
    public string Name => "Nominatim";

    private readonly HttpClient _httpClient;
    private readonly GeocodingOptions _options;
    private readonly IRateLimiter _rateLimiter;
    private readonly ICoordinateCacheRepository? _cache;

    public NominatimGeocodingProvider(
        HttpClient httpClient,
        GeocodingOptions options,
        IRateLimiter rateLimiter,
        ICoordinateCacheRepository? cache = null)
    {
        _httpClient = httpClient;
        _options = options;
        _rateLimiter = rateLimiter;
        _cache = cache;

        if (_httpClient.Timeout == TimeSpan.FromSeconds(100)) // default HttpClient timeout
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds));
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent") && !string.IsNullOrWhiteSpace(_options.UserAgent))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", _options.UserAgent);
        }
    }

    public async Task<GeocodingResult> ReverseAsync(Coordinate coordinate, CancellationToken ct = default)
    {
        if (!coordinate.IsValid)
        {
            return GeocodingResult.PermanentError(
                Name,
                GeocodingErrorCodes.InvalidCoordinate,
                $"Coordenada ({coordinate.Latitude:G}, {coordinate.Longitude:G}) inválida para geocodificação.");
        }

        // 1. Verifica no Cache
        if (_cache != null)
        {
            var cached = await _cache.GetAddressAsync(coordinate, _options.CachePrecision, Name, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                return GeocodingResult.Succeeded(cached, Name);
            }
        }

        var latStr = coordinate.Latitude.ToString(CultureInfo.InvariantCulture);
        var lonStr = coordinate.Longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"{_options.BaseUrl.TrimEnd('/')}/reverse?format=jsonv2&lat={latStr}&lon={lonStr}&zoom=18&addressdetails=1";

        var attempts = 0;
        var maxRetries = Math.Max(1, _options.MaxRetries);

        while (attempts < maxRetries)
        {
            attempts++;
            if (ct.IsCancellationRequested)
            {
                return GeocodingResult.Cancelled(Name);
            }

            try
            {
                using var releaser = await _rateLimiter.AcquireAsync(ct);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!request.Headers.Contains("User-Agent") && !string.IsNullOrWhiteSpace(_options.UserAgent))
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", _options.UserAgent);
                }

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync(ct);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("display_name", out var displayNameProp))
                    {
                        var address = displayNameProp.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(address))
                        {
                            if (_cache != null)
                            {
                                await _cache.SetAddressAsync(coordinate, _options.CachePrecision, Name, address, found: true, ct);
                            }
                            return GeocodingResult.Succeeded(address, Name);
                        }
                    }

                    if (root.TryGetProperty("error", out var errorProp))
                    {
                        var errorMsg = errorProp.GetString() ?? "Coordenada não encontrada.";
                        if (_cache != null)
                        {
                            await _cache.SetAddressAsync(coordinate, _options.CachePrecision, Name, null, found: false, ct);
                        }
                        return GeocodingResult.NotFound(Name, errorMsg);
                    }

                    return GeocodingResult.NotFound(Name);
                }

                if (response.StatusCode == (HttpStatusCode)429)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta;
                    await _rateLimiter.NotifyRateLimitAsync(retryAfter, ct);

                    if (attempts >= maxRetries)
                    {
                        return GeocodingResult.RateLimited(Name, retryAfter);
                    }

                    continue;
                }

                if ((int)response.StatusCode >= 500)
                {
                    if (attempts >= maxRetries)
                    {
                        return GeocodingResult.TransientError(
                            Name,
                            GeocodingErrorCodes.ProviderError,
                            $"Erro do servidor de geocodificação (HTTP {(int)response.StatusCode}).",
                            (int)response.StatusCode);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempts), ct);
                    continue;
                }

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    return GeocodingResult.PermanentError(
                        Name,
                        GeocodingErrorCodes.Unauthorized,
                        $"Acesso não autorizado pelo provedor (HTTP {(int)response.StatusCode}).",
                        (int)response.StatusCode);
                }

                return GeocodingResult.PermanentError(
                    Name,
                    GeocodingErrorCodes.ProviderError,
                    $"Resposta inesperada do provedor (HTTP {(int)response.StatusCode}).",
                    (int)response.StatusCode);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return GeocodingResult.Cancelled(Name);
            }
            catch (OperationCanceledException)
            {
                // Timeout da requisição individual
                if (attempts >= maxRetries)
                {
                    return GeocodingResult.TransientError(
                        Name,
                        GeocodingErrorCodes.Timeout,
                        "Tempo limite de requisição excedido.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempts), ct);
            }
            catch (HttpRequestException ex)
            {
                if (attempts >= maxRetries)
                {
                    return GeocodingResult.TransientError(
                        Name,
                        GeocodingErrorCodes.NetworkError,
                        $"Falha de conexão com o serviço de geocodificação: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempts), ct);
            }
        }

        return GeocodingResult.TransientError(
            Name,
            GeocodingErrorCodes.ProviderError,
            "Tentativas esgotadas sem sucesso.");
    }
}
