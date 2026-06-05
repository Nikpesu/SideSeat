using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/korisnici")]
public class KorisniciApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;

    public KorisniciApiController(SideSeatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<KorisnikDto>>> GetAll(string? q = null)
    {
        var query = _db.Korisnici.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(k => k.Ime.Contains(term) || k.Prezime.Contains(term) || k.Email.Contains(term));
        }

        var korisnici = await query.OrderBy(k => k.Prezime).ThenBy(k => k.Ime).ToListAsync();
        return Ok(korisnici.Select(k => k.ToDto()));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<KorisnikDto>> Get(int id)
    {
        var korisnik = await _db.Korisnici.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id);
        return korisnik is null ? NotFound() : Ok(korisnik.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<KorisnikDto>> Post(KorisnikRequest request)
    {
        var korisnik = new Korisnik
        {
            Ime = request.Ime.Trim(),
            Prezime = request.Prezime.Trim(),
            Email = request.Email.Trim(),
            Adresa = request.Adresa.Trim(),
            BrojMobitela = request.BrojMobitela.Trim(),
            DatumRegistracije = DateTime.UtcNow,
            Tip = request.Tip,
            JeAktivan = request.JeAktivan,
            LozinkaHash = string.Empty
        };
        _db.Korisnici.Add(korisnik);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = korisnik.Id }, korisnik.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<KorisnikDto>> Put(int id, KorisnikRequest request)
    {
        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == id);
        if (korisnik is null)
        {
            return NotFound();
        }

        korisnik.Ime = request.Ime.Trim();
        korisnik.Prezime = request.Prezime.Trim();
        korisnik.Email = request.Email.Trim();
        korisnik.Adresa = request.Adresa.Trim();
        korisnik.BrojMobitela = request.BrojMobitela.Trim();
        korisnik.Tip = request.Tip;
        korisnik.JeAktivan = request.JeAktivan;
        await _db.SaveChangesAsync();
        return Ok(korisnik.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == id);
        if (korisnik is null)
        {
            return NotFound();
        }

        var inUse = await _db.Voznje.AnyAsync(v => v.VozacId == id) || await _db.Rezervacije.AnyAsync(r => r.PutnikId == id);
        if (inUse)
        {
            return UnprocessableEntity(new { message = "Korisnik ima povezane voznje ili rezervacije." });
        }

        _db.Korisnici.Remove(korisnik);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
