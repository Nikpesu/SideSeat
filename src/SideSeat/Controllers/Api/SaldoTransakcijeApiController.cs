using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/saldo-transakcije")]
[Authorize(Roles = "Admin")]
public class SaldoTransakcijeApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;

    public SaldoTransakcijeApiController(SideSeatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaldoTransakcijaDto>>> GetAll(string? q = null)
    {
        var query = _db.SaldoTransakcije.AsNoTracking().Include(t => t.Korisnik).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(t => t.Tip.Contains(term) || t.Korisnik.Email.Contains(term));
        }

        var transakcije = await query.OrderByDescending(t => t.Vrijeme).ToListAsync();
        return Ok(transakcije.Select(t => t.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaldoTransakcijaDto>> Get(int id)
    {
        var transakcija = await _db.SaldoTransakcije.AsNoTracking().Include(t => t.Korisnik).FirstOrDefaultAsync(t => t.Id == id);
        return transakcija is null ? NotFound() : Ok(transakcija.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<SaldoTransakcijaDto>> Post(SaldoTransakcijaRequest request)
    {
        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == request.KorisnikId);
        if (korisnik is null)
        {
            return BadRequest(new { message = "Korisnik ne postoji." });
        }

        var transakcija = BuildTransaction(request, korisnik);
        _db.SaldoTransakcije.Add(transakcija);
        await _db.SaveChangesAsync();
        await _db.Entry(transakcija).Reference(t => t.Korisnik).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = transakcija.Id }, transakcija.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SaldoTransakcijaDto>> Put(int id, SaldoTransakcijaRequest request)
    {
        var transakcija = await _db.SaldoTransakcije.Include(t => t.Korisnik).FirstOrDefaultAsync(t => t.Id == id);
        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == request.KorisnikId);
        if (transakcija is null)
        {
            return NotFound();
        }

        if (korisnik is null)
        {
            return BadRequest(new { message = "Korisnik ne postoji." });
        }

        transakcija.KorisnikId = request.KorisnikId;
        transakcija.Iznos = request.Iznos;
        transakcija.Tip = request.Tip.Trim();
        transakcija.SaldoPrije = korisnik.Saldo;
        transakcija.SaldoPoslije = korisnik.Saldo + request.Iznos;
        transakcija.Vrijeme = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _db.Entry(transakcija).Reference(t => t.Korisnik).LoadAsync();
        return Ok(transakcija.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var transakcija = await _db.SaldoTransakcije.FirstOrDefaultAsync(t => t.Id == id);
        if (transakcija is null)
        {
            return NotFound();
        }

        _db.SaldoTransakcije.Remove(transakcija);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static SaldoTransakcija BuildTransaction(SaldoTransakcijaRequest request, Korisnik korisnik) => new()
    {
        KorisnikId = request.KorisnikId,
        Iznos = request.Iznos,
        Tip = request.Tip.Trim(),
        SaldoPrije = korisnik.Saldo,
        SaldoPoslije = korisnik.Saldo + request.Iznos,
        Vrijeme = DateTime.UtcNow
    };
}
