using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public sealed class RouteGeometryServiceTests
{
    [Fact]
    public async Task OsrmGeometry_IsParsedInLatitudeLongitudeOrderAndCached()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            Assert.Contains(
                "/route/v1/driving/15.981919,45.81501;16.440193,43.508133",
                request.RequestUri?.ToString());
            return Task.FromResult(JsonResponse(
                """
                {
                  "code": "Ok",
                  "routes": [{
                    "distance": 410000.5,
                    "duration": 14400.25,
                    "geometry": {
                      "type": "LineString",
                      "coordinates": [
                        [15.981919, 45.815010],
                        [16.100000, 44.800000],
                        [16.440193, 43.508133]
                      ]
                    }
                  }]
                }
                """));
        });
        var service = CreateService(handler);

        var first = await service.GetRouteAsync(
            45.815010m,
            15.981919m,
            43.508133m,
            16.440193m);
        var second = await service.GetRouteAsync(
            45.815010m,
            15.981919m,
            43.508133m,
            16.440193m);

        Assert.NotNull(first);
        Assert.Equal(3, first.Points.Count);
        Assert.Equal(45.815010, first.Points[0].Latitude, 6);
        Assert.Equal(15.981919, first.Points[0].Longitude, 6);
        Assert.Equal(410000.5, first.DistanceMeters);
        Assert.Same(first, second);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SlowRoutingProvider_TimesOutWithinOneSecond()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return JsonResponse("{}");
        });
        var service = CreateService(handler, timeoutMilliseconds: 250);
        var stopwatch = Stopwatch.StartNew();

        var result = await service.GetRouteAsync(
            45.815010m,
            15.981919m,
            43.508133m,
            16.440193m);

        stopwatch.Stop();
        Assert.Null(result);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1), stopwatch.Elapsed.ToString());
    }

    private static OsrmRouteGeometryService CreateService(
        StubHttpMessageHandler handler,
        int timeoutMilliseconds = 750)
    {
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
        return new OsrmRouteGeometryService(
            new StubHttpClientFactory(client),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new MapsOptions
            {
                RoutingBaseUrl = "https://routing.test",
                RoutingTimeoutMilliseconds = timeoutMilliseconds,
                NominatimUserAgent = "SideSeat tests"
            }),
            NullLogger<OsrmRouteGeometryService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return responseFactory(request, cancellationToken);
        }
    }
}
