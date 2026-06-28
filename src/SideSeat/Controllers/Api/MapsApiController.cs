using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SideSeat.Services;

namespace SideSeat.Controllers.Api;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("maps")]
[Route("api/maps")]
public sealed class MapsApiController(IRouteGeometryService routeGeometry) : ControllerBase
{
    [HttpGet("route")]
    public async Task<IActionResult> Route(
        decimal startLat,
        decimal startLng,
        decimal endLat,
        decimal endLng,
        CancellationToken cancellationToken)
    {
        if (!ValidCoordinate(startLat, startLng) ||
            !ValidCoordinate(endLat, endLng))
        {
            return ValidationProblem("Koordinate rute nisu valjane.");
        }

        var result = await routeGeometry.GetRouteAsync(
            startLat,
            startLng,
            endLat,
            endLng,
            cancellationToken);
        if (result is null)
        {
            Response.Headers.CacheControl = "no-store";
            return NoContent();
        }

        Response.Headers.CacheControl = "public,max-age=604800";
        return Ok(new
        {
            points = result.Points.Select(point => new[]
            {
                point.Latitude,
                point.Longitude
            }),
            distanceMeters = result.DistanceMeters,
            durationSeconds = result.DurationSeconds
        });
    }

    private static bool ValidCoordinate(decimal latitude, decimal longitude) =>
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180;
}
