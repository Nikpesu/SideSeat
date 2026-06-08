using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;
using SideSeat.Services;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/placanja")]
[Authorize(Roles = "Admin")]
public class PlacanjaApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;
    private readonly INotificationService _notifications;

    public PlacanjaApiController(SideSeatDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlacanjeDto>>> GetAll(DateTime? date = null)
    {
        var query = _db.Placanja.AsNoTracking();
        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            query = query.Where(p => p.VrijemePlacanja.Date == selectedDate);
        }

        var placanja = await query.OrderByDescending(p => p.VrijemePlacanja).ToListAsync();
        return Ok(placanja.Select(p => p.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlacanjeDto>> Get(int id)
    {
        var placanje = await _db.Placanja.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return placanje is null ? NotFound() : Ok(placanje.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<PlacanjeDto>> Post(PlacanjeRequest request)
    {
        var rezervacija = await _db.Rezervacije.FirstOrDefaultAsync(r => r.Id == request.RezervacijaId);
        if (rezervacija is null)
        {
            return BadRequest(new { message = "Rezervacija ne postoji." });
        }

        var placanje = new Placanje();
        Apply(request, placanje);
        _db.Placanja.Add(placanje);
        _notifications.Add(
            rezervacija.PutnikId,
            request.Uspjesno ? "Plaćanje evidentirano" : "Plaćanje nije uspjelo",
            $"Plaćanje za rezervaciju #{rezervacija.Id}: {request.Iznos:0.00} EUR.",
            "Naplata",
            $"/Rezervacija/Details/{rezervacija.Id}");
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = placanje.Id }, placanje.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlacanjeDto>> Put(int id, PlacanjeRequest request)
    {
        var placanje = await _db.Placanja.FirstOrDefaultAsync(p => p.Id == id);
        if (placanje is null)
        {
            return NotFound();
        }

        if (!await _db.Rezervacije.AnyAsync(r => r.Id == request.RezervacijaId))
        {
            return BadRequest(new { message = "Rezervacija ne postoji." });
        }

        Apply(request, placanje);
        await _db.SaveChangesAsync();
        return Ok(placanje.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var placanje = await _db.Placanja.FirstOrDefaultAsync(p => p.Id == id);
        if (placanje is null)
        {
            return NotFound();
        }

        _db.Placanja.Remove(placanje);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(PlacanjeRequest request, Placanje placanje)
    {
        placanje.RezervacijaId = request.RezervacijaId;
        placanje.Iznos = request.Iznos;
        placanje.VrijemePlacanja = request.VrijemePlacanja;
        placanje.NacinPlacanja = request.NacinPlacanja;
        placanje.Uspjesno = request.Uspjesno;
    }
}
