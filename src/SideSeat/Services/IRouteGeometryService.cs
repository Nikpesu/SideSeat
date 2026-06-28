namespace SideSeat.Services;

public interface IRouteGeometryService
{
    Task<RouteGeometryResult?> GetRouteAsync(
        decimal startLatitude,
        decimal startLongitude,
        decimal endLatitude,
        decimal endLongitude,
        CancellationToken cancellationToken = default);
}

public sealed record RouteCoordinate(double Latitude, double Longitude);

public sealed record RouteGeometryResult(
    IReadOnlyList<RouteCoordinate> Points,
    double DistanceMeters,
    double DurationSeconds);
