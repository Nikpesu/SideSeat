using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SideSeat.Services;

public sealed class NominatimCityGeocodingService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<MapsOptions> options,
    ILogger<NominatimCityGeocodingService> logger) : ICityGeocodingService
{
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

    public async Task<CityCoordinateResult> ResolveAsync(
        string name,
        string country,
        string postalCode,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken = default)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            return CityCoordinateResult.Failure(
                "Potrebno je unijeti obje koordinate ili obje ostaviti praznima.");
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            return CoordinatesAreValid(latitude.Value, longitude.Value)
                ? CityCoordinateResult.Success(latitude.Value, longitude.Value)
                : CityCoordinateResult.Failure("Koordinate nisu u dopuštenom rasponu.");
        }

        var normalizedName = name.Trim();
        var normalizedCountry = country.Trim();
        var normalizedPostalCode = postalCode.Trim();
        var cacheKey = string.Join(
            '|',
            "city-geocoding",
            normalizedName.ToUpperInvariant(),
            normalizedCountry.ToUpperInvariant(),
            normalizedPostalCode.ToUpperInvariant());

        if (cache.TryGetValue<CityCoordinateResult>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue<CityCoordinateResult>(cacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var wait = TimeSpan.FromSeconds(1) - (DateTimeOffset.UtcNow - _lastRequestAt);
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken);
            }

            _lastRequestAt = DateTimeOffset.UtcNow;
            var result = await RequestCoordinatesAsync(
                normalizedName,
                normalizedCountry,
                normalizedPostalCode,
                cancellationToken);

            cache.Set(
                cacheKey,
                result,
                result.Succeeded ? TimeSpan.FromDays(30) : TimeSpan.FromHours(1));
            return result;
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private async Task<CityCoordinateResult> RequestCoordinatesAsync(
        string name,
        string country,
        string postalCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var mapOptions = options.Value;
            var query = new Dictionary<string, string?>
            {
                ["format"] = "jsonv2",
                ["limit"] = "1",
                ["city"] = name,
                ["country"] = country,
                ["postalcode"] = postalCode
            };
            if (!string.IsNullOrWhiteSpace(mapOptions.ContactEmail))
            {
                query["email"] = mapOptions.ContactEmail.Trim();
            }

            var endpoint = QueryHelpers.AddQueryString(
                $"{mapOptions.NominatimBaseUrl.TrimEnd('/')}/search",
                query);
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.TryAddWithoutValidation(
                "User-Agent",
                mapOptions.NominatimUserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var client = httpClientFactory.CreateClient("Nominatim");
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Nominatim geocoding returned status {StatusCode} for {City}, {Country}.",
                    response.StatusCode,
                    name,
                    country);
                return CityCoordinateResult.Failure(
                    "Koordinate nisu dohvaćene. Unesite ih ručno i pokušajte ponovno.");
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var first = document.RootElement.ValueKind == JsonValueKind.Array &&
                document.RootElement.GetArrayLength() > 0
                    ? document.RootElement[0]
                    : default;
            if (first.ValueKind != JsonValueKind.Object ||
                !first.TryGetProperty("lat", out var latitudeValue) ||
                !first.TryGetProperty("lon", out var longitudeValue) ||
                !decimal.TryParse(
                    latitudeValue.GetString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var latitude) ||
                !decimal.TryParse(
                    longitudeValue.GetString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var longitude) ||
                !CoordinatesAreValid(latitude, longitude))
            {
                return CityCoordinateResult.Failure(
                    "Lokacija grada nije pronađena. Unesite koordinate ručno.");
            }

            return CityCoordinateResult.Success(latitude, longitude);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CityCoordinateResult.Failure(
                "Dohvat koordinata je istekao. Unesite koordinate ručno.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Nominatim geocoding request failed.");
            return CityCoordinateResult.Failure(
                "Koordinate trenutačno nisu dostupne. Unesite ih ručno.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Nominatim returned an invalid response.");
            return CityCoordinateResult.Failure(
                "Servis koordinata vratio je neispravan odgovor. Unesite koordinate ručno.");
        }
    }

    private static bool CoordinatesAreValid(decimal latitude, decimal longitude) =>
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180;
}
