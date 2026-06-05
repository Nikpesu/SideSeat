using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/rezervacije")]
public class RezervacijeApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;

    public RezervacijeApiController(SideSeatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<RezervacijaDto>>> GetAll(string? q = null)
    {
        var query = _db.Rezervacije.AsNoTracking().Include(r => r.Putnik).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r => r.Napomena.Contains(term) || r.Putnik.Ime.Contains(term) || r.Putnik.Prezime.Contains(term));
        }

        var rezervacije = await query.OrderByDescending(r => r.VrijemeRezervacije).ToListAsync();
        return Ok(rezervacije.Select(r => r.ToDto()));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<RezervacijaDto>> Get(int id)
    {
        var rezervacija = await _db.Rezervacije.AsNoTracking().Include(r => r.Putnik).FirstOrDefaultAsync(r => r.Id == id);
        return rezervacija is null ? NotFound() : Ok(rezervacija.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RezervacijaDto>> Post(RezervacijaRequest request)
    {
        var voznja = await _db.Voznje.FirstOrDefaultAsync(v => v.Id == request.VoznjaId);
        if (voznja is null || !await _db.Korisnici.AnyAsync(k => k.Id == request.PutnikId))
        {
            return BadRequest(new { message = "Voznja ili putnik ne postoji." });
        }

        var rezervacija = new Rezervacija();
        Apply(request, rezervacija, voznja.CijenaPoMjestu * request.BrojMjesta);
        _db.Rezervacije.Add(rezervacija);
        await _db.SaveChangesAsync();
        await _db.Entry(rezervacija).Reference(r => r.Putnik).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = rezervacija.Id }, rezervacija.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RezervacijaDto>> Put(int id, RezervacijaRequest request)
    {
        var rezervacija = await _db.Rezervacije.Include(r => r.Putnik).FirstOrDefaultAsync(r => r.Id == id);
        var voznja = await _db.Voznje.FirstOrDefaultAsync(v => v.Id == request.VoznjaId);
        if (rezervacija is null)
        {
            return NotFound();
        }

        if (voznja is null || !await _db.Korisnici.AnyAsync(k => k.Id == request.PutnikId))
        {
            return BadRequest(new { message = "Voznja ili putnik ne postoji." });
        }

        Apply(request, rezervacija, voznja.CijenaPoMjestu * request.BrojMjesta);
        await _db.SaveChangesAsync();
        await _db.Entry(rezervacija).Reference(r => r.Putnik).LoadAsync();
        return Ok(rezervacija.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var rezervacija = await _db.Rezervacije.FirstOrDefaultAsync(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        var inUse = await _db.Placanja.AnyAsync(p => p.RezervacijaId == id) || await _db.Ocjene.AnyAsync(o => o.RezervacijaId == id);
        if (inUse)
        {
            return UnprocessableEntity(new { message = "Rezervacija ima placanja ili ocjene." });
        }

        _db.Rezervacije.Remove(rezervacija);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(RezervacijaRequest request, Rezervacija rezervacija, decimal cijenaUkupno)
    {
        rezervacija.VoznjaId = request.VoznjaId;
        rezervacija.PutnikId = request.PutnikId;
        rezervacija.BrojMjesta = request.BrojMjesta;
        rezervacija.CijenaUkupno = cijenaUkupno;
        rezervacija.VrijemeRezervacije = rezervacija.Id == 0 ? DateTime.UtcNow : rezervacija.VrijemeRezervacije;
        rezervacija.Status = request.Status;
        rezervacija.Napomena = request.Napomena.Trim();
    }
}
