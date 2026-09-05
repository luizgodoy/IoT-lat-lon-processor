using System.Net;
using System.Text;
using IoT_lat_lon_processor.Configuration;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Infrastructure.Geocoding;
using IoT_lat_lon_processor.Infrastructure.Http;
using IoT_lat_lon_processor.Infrastructure.Persistence;
using Xunit;

namespace IoT_lat_lon_processor.Tests.Geocoding;

public class GeocodingProviderTests
{
    [Fact]
    public async Task TC10_Provider_Returns_Success_Address_Populated()
    {
        var json = "{\"display_name\":\"Avenida Paulista, Bela Vista, São Paulo, SP, Brasil\"}";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler);

        var options = new GeocodingOptions { MaxRetries = 1, RequestIntervalMs = 0 };
        var rateLimiter = new RateLimiter(intervalMs: 0, maxConcurrency: 1);
        var provider = new NominatimGeocodingProvider(httpClient, options, rateLimiter);

        var result = await provider.ReverseAsync(new Coordinate(-23.56m, -46.65m));

        Assert.True(result.Success);
        Assert.Equal("Avenida Paulista, Bela Vista, São Paulo, SP, Brasil", result.DisplayAddress);
        Assert.Equal("Nominatim", result.Provider);
        Assert.Single(handler.RecordedRequests);
        Assert.Contains("User-Agent", handler.RecordedRequests[0].Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task TC11_Provider_Returns_NotFound_Result_Classified()
    {
        var json = "{\"error\":\"Unable to geocode\"}";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler);

        var options = new GeocodingOptions { MaxRetries = 1, RequestIntervalMs = 0 };
        var rateLimiter = new RateLimiter(intervalMs: 0, maxConcurrency: 1);
        var provider = new NominatimGeocodingProvider(httpClient, options, rateLimiter);

        var result = await provider.ReverseAsync(new Coordinate(0m, 0m));

        Assert.False(result.Success);
        Assert.Equal(GeocodingErrorCodes.NotFound, result.ErrorCode);
        Assert.Null(result.DisplayAddress);
    }

    [Fact]
    public async Task TC12_Provider_Returns_429_Respects_RateLimit()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.TooManyRequests, "Rate limited");
        var httpClient = new HttpClient(handler);

        var options = new GeocodingOptions { MaxRetries = 2, RequestIntervalMs = 0 };
        var rateLimiter = new RateLimiter(intervalMs: 0, maxConcurrency: 1);
        var provider = new NominatimGeocodingProvider(httpClient, options, rateLimiter);

        var result = await provider.ReverseAsync(new Coordinate(-23.55m, -46.63m));

        Assert.False(result.Success);
        Assert.Equal(GeocodingErrorCodes.RateLimited, result.ErrorCode);
        Assert.True(rateLimiter.PauseCount > 0);
        Assert.Equal(2, handler.RecordedRequests.Count);
    }

    [Fact]
    public async Task TC09_Cache_Hit_Bypasses_Http_Call()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);

        var options = new GeocodingOptions { MaxRetries = 1, RequestIntervalMs = 0, CachePrecision = 6 };
        var rateLimiter = new RateLimiter(intervalMs: 0, maxConcurrency: 1);
        var mockCache = new InMemoryCacheRepository();

        var coord = new Coordinate(-23.550520m, -46.633308m);
        await mockCache.SetAddressAsync(coord, 6, "Nominatim", "Endereço em Cache", found: true);

        var provider = new NominatimGeocodingProvider(httpClient, options, rateLimiter, mockCache);
        var result = await provider.ReverseAsync(coord);

        Assert.True(result.Success);
        Assert.Equal("Endereço em Cache", result.DisplayAddress);
        Assert.Empty(handler.RecordedRequests); // Nenhuma chamada HTTP foi feita!
    }

    [Fact]
    public async Task TC22_Internet_Failure_Classified_As_Transient_Network_Error()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("DNS resolution failed"));
        var httpClient = new HttpClient(handler);

        var options = new GeocodingOptions { MaxRetries = 1, RequestIntervalMs = 0 };
        var rateLimiter = new RateLimiter(intervalMs: 0, maxConcurrency: 1);
        var provider = new NominatimGeocodingProvider(httpClient, options, rateLimiter);

        var result = await provider.ReverseAsync(new Coordinate(-23.55m, -46.63m));

        Assert.False(result.Success);
        Assert.Equal(GeocodingErrorCodes.NetworkError, result.ErrorCode);
        Assert.True(result.IsTransientError);
    }

    [Fact]
    public async Task TC23_Cancellation_Returns_Cancelled_Result()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);

        var options = new GeocodingOptions { MaxRetries = 1, RequestIntervalMs = 0 };
        var rateLimiter = new RateLimiter(intervalMs: 0, maxConcurrency: 1);
        var provider = new NominatimGeocodingProvider(httpClient, options, rateLimiter);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await provider.ReverseAsync(new Coordinate(-23.55m, -46.63m), cts.Token);

        Assert.False(result.Success);
        Assert.Equal(GeocodingErrorCodes.Cancelled, result.ErrorCode);
    }

    private sealed class InMemoryCacheRepository : ICoordinateCacheRepository
    {
        private readonly Dictionary<string, string?> _cache = new();

        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<string?> GetAddressAsync(Coordinate coordinate, int precision, string provider, CancellationToken ct = default)
        {
            var key = coordinate.GetCacheKey(precision, provider);
            _cache.TryGetValue(key, out var val);
            return Task.FromResult(val);
        }

        public Task SetAddressAsync(Coordinate coordinate, int precision, string provider, string? address, bool found, CancellationToken ct = default)
        {
            var key = coordinate.GetCacheKey(precision, provider);
            _cache[key] = address;
            return Task.CompletedTask;
        }
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;
        public List<HttpRequestMessage> RecordedRequests { get; } = [];

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RecordedRequests.Add(request);
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            };

            if (_statusCode == (HttpStatusCode)429)
            {
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(10));
            }

            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHttpMessageHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}
