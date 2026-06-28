namespace SideSeat.Services;

public sealed record CityCoordinateResult(
    bool Succeeded,
    decimal? Latitude,
    decimal? Longitude,
    string? Error)
{
    public static CityCoordinateResult Success(decimal latitude, decimal longitude) =>
        new(true, latitude, longitude, null);

    public static CityCoordinateResult Failure(string error) =>
        new(false, null, null, error);
}

public interface ICityGeocodingService
{
    Task<CityCoordinateResult> ResolveAsync(
        string name,
        string country,
        string postalCode,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken = default);
}
