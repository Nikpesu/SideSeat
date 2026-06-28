using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public sealed class CityGeocodingServiceTests
{
    [Fact]
    public async Task ManualCoordinates_BypassNominatim()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = CreateService(handler);

        var result = await service.ResolveAsync(
            "Zagreb",
            "Hrvatska",
            "10000",
            45.815010m,
            15.981919m);

        Assert.True(result.Succeeded);
        Assert.Equal(45.815010m, result.Latitude);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SingleManualCoordinate_IsRejected()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(handler);

        var result = await service.ResolveAsync(
            "Zagreb",
            "Hrvatska",
            "10000",
            45.815010m,
            null);

        Assert.False(result.Succeeded);
        Assert.Contains("obje koordinate", result.Error);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task NominatimResult_IsParsedAndCached()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""[{"lat":"45.815010","lon":"15.981919"}]"""));
        var service = CreateService(handler);

        var first = await service.ResolveAsync("Zagreb", "Hrvatska", "10000", null, null);
        var second = await service.ResolveAsync("Zagreb", "Hrvatska", "10000", null, null);

        Assert.True(first.Succeeded);
        Assert.Equal(45.815010m, first.Latitude);
        Assert.Equal(15.981919m, first.Longitude);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task EmptyNominatimResult_ReturnsBusinessFailure()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("[]"));
        var service = CreateService(handler);

        var result = await service.ResolveAsync(
            "Nepoznati Grad",
            "Hrvatska",
            "99999",
            null,
            null);

        Assert.False(result.Succeeded);
        Assert.Contains("nije pronađena", result.Error);
    }

    [Fact]
    public async Task NominatimTimeout_ReturnsBusinessFailure()
    {
        var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException());
        var service = CreateService(handler);

        var result = await service.ResolveAsync(
            "Zagreb",
            "Hrvatska",
            "10000",
            null,
            null);

        Assert.False(result.Succeeded);
        Assert.Contains("istekao", result.Error);
    }

    private static NominatimCityGeocodingService CreateService(StubHttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        return new NominatimCityGeocodingService(
            new StubHttpClientFactory(client),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new MapsOptions
            {
                NominatimBaseUrl = "https://nominatim.test",
                NominatimUserAgent = "SideSeat tests"
            }),
            NullLogger<NominatimCityGeocodingService>.Instance);
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
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responseFactory(request));
        }
    }
}
