using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SideSeat.Services;

public sealed class OsrmRouteGeometryService : IRouteGeometryService
{
    private static readonly TimeSpan SuccessCacheDuration = TimeSpan.FromDays(7);
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly MapsOptions _options;
    private readonly ILogger<OsrmRouteGeometryService> _logger;
    private readonly ConcurrentDictionary<string, Task<RouteGeometryResult?>> _inflight = new();

    public OsrmRouteGeometryService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<MapsOptions> options,
        ILogger<OsrmRouteGeometryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RouteGeometryResult?> GetRouteAsync(
        decimal startLatitude,
        decimal startLongitude,
        decimal endLatitude,
        decimal endLongitude,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(
            startLatitude,
            startLongitude,
            endLatitude,
            endLongitude);
        if (_cache.TryGetValue<RouteCacheEntry>(cacheKey, out var cached))
        {
            return cached!.Result;
        }

        var task = _inflight.GetOrAdd(
            cacheKey,
            _ => FetchAndCacheAsync(
                cacheKey,
                startLatitude,
                startLongitude,
                endLatitude,
                endLongitude));
        try
        {
            return await task.WaitAsync(cancellationToken);
        }
        finally
        {
            if (task.IsCompleted)
            {
                _inflight.TryRemove(cacheKey, out _);
            }
        }
    }

    private async Task<RouteGeometryResult?> FetchAndCacheAsync(
        string cacheKey,
        decimal startLatitude,
        decimal startLongitude,
        decimal endLatitude,
        decimal endLongitude)
    {
        RouteGeometryResult? result = null;
        try
        {
            var baseUrl = _options.RoutingBaseUrl.TrimEnd('/');
            var coordinates = string.Create(
                CultureInfo.InvariantCulture,
                $"{startLongitude:0.######},{startLatitude:0.######};{endLongitude:0.######},{endLatitude:0.######}");
            var requestUrl =
                $"{baseUrl}/route/v1/driving/{coordinates}?overview=simplified&geometries=geojson&steps=false";
            var timeoutMilliseconds = Math.Clamp(
                _options.RoutingTimeoutMilliseconds,
                250,
                900);
            using var timeout = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(timeoutMilliseconds));
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.UserAgent.ParseAdd(_options.NominatimUserAgent);
            using var response = await _httpClientFactory
                .CreateClient("Routing")
                .SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Routing provider returned status {StatusCode} for {RouteKey}.",
                    (int)response.StatusCode,
                    cacheKey);
            }
            else
            {
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var document = await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: timeout.Token);
                result = ParseResult(document.RootElement);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Routing provider timed out for {RouteKey}.", cacheKey);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Routing provider failed for {RouteKey}.", cacheKey);
        }

        _cache.Set(
            cacheKey,
            new RouteCacheEntry(result),
            result is null ? FailureCacheDuration : SuccessCacheDuration);
        return result;
    }

    private static RouteGeometryResult? ParseResult(JsonElement root)
    {
        if (!root.TryGetProperty("code", out var code) ||
            !string.Equals(code.GetString(), "Ok", StringComparison.Ordinal) ||
            !root.TryGetProperty("routes", out var routes) ||
            routes.GetArrayLength() == 0)
        {
            return null;
        }

        var route = routes[0];
        if (!route.TryGetProperty("geometry", out var geometry) ||
            !geometry.TryGetProperty("coordinates", out var coordinates))
        {
            return null;
        }

        var points = new List<RouteCoordinate>();
        foreach (var coordinate in coordinates.EnumerateArray())
        {
            if (coordinate.GetArrayLength() < 2)
            {
                continue;
            }

            var longitude = coordinate[0].GetDouble();
            var latitude = coordinate[1].GetDouble();
            if (latitude is >= -90 and <= 90 &&
                longitude is >= -180 and <= 180)
            {
                points.Add(new RouteCoordinate(latitude, longitude));
            }
        }

        if (points.Count < 2)
        {
            return null;
        }

        points = LimitPoints(points, 500);
        return new RouteGeometryResult(
            points,
            route.TryGetProperty("distance", out var distance) ? distance.GetDouble() : 0,
            route.TryGetProperty("duration", out var duration) ? duration.GetDouble() : 0);
    }

    private static List<RouteCoordinate> LimitPoints(
        IReadOnlyList<RouteCoordinate> points,
        int maximum)
    {
        if (points.Count <= maximum)
        {
            return points.ToList();
        }

        var result = new List<RouteCoordinate>(maximum);
        for (var index = 0; index < maximum; index++)
        {
            var sourceIndex = (int)Math.Round(
                index * (points.Count - 1d) / (maximum - 1d));
            result.Add(points[sourceIndex]);
        }
        return result;
    }

    private static string BuildCacheKey(
        decimal startLatitude,
        decimal startLongitude,
        decimal endLatitude,
        decimal endLongitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"route:{startLatitude:0.######}:{startLongitude:0.######}:{endLatitude:0.######}:{endLongitude:0.######}");

    private sealed record RouteCacheEntry(RouteGeometryResult? Result);
}
