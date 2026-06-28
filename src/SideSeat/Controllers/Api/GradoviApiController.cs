using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;
using SideSeat.Services;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/gradovi")]
public class GradoviApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;
    private readonly ICityGeocodingService _cityGeocoding;

    public GradoviApiController(
        SideSeatDbContext db,
        ICityGeocodingService cityGeocoding)
    {
        _db = db;
        _cityGeocoding = cityGeocoding;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GradDto>>> GetAll(string? q = null)
    {
        var query = _db.Gradovi.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(g => g.Naziv.Contains(term) || g.Drzava.Contains(term) || g.PostanskiBroj.Contains(term));
        }

        var gradovi = await query.OrderBy(g => g.Naziv).ToListAsync();
        return Ok(gradovi.Select(g => g.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GradDto>> Get(int id)
    {
        var grad = await _db.Gradovi.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        return grad is null ? NotFound() : Ok(grad.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GradDto>> Post(
        GradRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Naziv.Trim();
        var country = request.Drzava.Trim();
        var postalCode = request.PostanskiBroj.Trim();
        var coordinates = await _cityGeocoding.ResolveAsync(
            name,
            country,
            postalCode,
            request.Latitude,
            request.Longitude,
            cancellationToken);
        if (!coordinates.Succeeded)
        {
            return GeocodingProblem(coordinates.Error);
        }

        var grad = new Grad
        {
            Naziv = name,
            Drzava = country,
            PostanskiBroj = postalCode,
            Latitude = coordinates.Latitude,
            Longitude = coordinates.Longitude
        };
        _db.Gradovi.Add(grad);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = grad.Id }, grad.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GradDto>> Put(
        int id,
        GradRequest request,
        CancellationToken cancellationToken)
    {
        var grad = await _db.Gradovi.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (grad is null)
        {
            return NotFound();
        }

        var name = request.Naziv.Trim();
        var country = request.Drzava.Trim();
        var postalCode = request.PostanskiBroj.Trim();
        var identityChanged =
            !string.Equals(grad.Naziv, name, StringComparison.Ordinal) ||
            !string.Equals(grad.Drzava, country, StringComparison.Ordinal) ||
            !string.Equals(grad.PostanskiBroj, postalCode, StringComparison.Ordinal);
        var manualCoordinatesProvided =
            request.Latitude.HasValue ||
            request.Longitude.HasValue;
        var latitude = !identityChanged && !manualCoordinatesProvided
            ? grad.Latitude
            : request.Latitude;
        var longitude = !identityChanged && !manualCoordinatesProvided
            ? grad.Longitude
            : request.Longitude;
        var coordinates = await _cityGeocoding.ResolveAsync(
            name,
            country,
            postalCode,
            latitude,
            longitude,
            cancellationToken);
        if (!coordinates.Succeeded)
        {
            return GeocodingProblem(coordinates.Error);
        }

        grad.Naziv = name;
        grad.Drzava = country;
        grad.PostanskiBroj = postalCode;
        grad.Latitude = coordinates.Latitude;
        grad.Longitude = coordinates.Longitude;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(grad.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var grad = await _db.Gradovi.FirstOrDefaultAsync(g => g.Id == id);
        if (grad is null)
        {
            return NotFound();
        }

        var inUse = await _db.Voznje.AnyAsync(v => v.PolazniGradId == id || v.OdredisniGradId == id);
        if (inUse)
        {
            return UnprocessableEntity(new { message = "Grad se ne moze obrisati jer je povezan s voznjama." });
        }

        _db.Gradovi.Remove(grad);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private UnprocessableEntityObjectResult GeocodingProblem(string? detail) =>
        UnprocessableEntity(new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Koordinate grada nisu dostupne",
            Detail = detail ?? "Unesite ispravne koordinate i pokušajte ponovno."
        });
}
