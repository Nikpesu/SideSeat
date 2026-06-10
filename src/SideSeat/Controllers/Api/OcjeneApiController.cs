using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/ocjene")]
public class OcjeneApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;

    public OcjeneApiController(SideSeatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OcjenaDto>>> GetAll(string? q = null)
    {
        var query = _db.Ocjene.AsNoTracking().Include(o => o.Autor).Include(o => o.Slike).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(o => o.Komentar.Contains(term) || o.Autor.Ime.Contains(term) || o.Autor.Prezime.Contains(term));
        }

        var ocjene = await query.OrderByDescending(o => o.Kreirano).ToListAsync();
        return Ok(ocjene.Select(o => o.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OcjenaDto>> Get(int id)
    {
        var ocjena = await _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Slike)
            .FirstOrDefaultAsync(o => o.Id == id);
        return ocjena is null ? NotFound() : Ok(ocjena.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OcjenaDto>> Post(OcjenaRequest request)
    {
        if (!await _db.Rezervacije.AnyAsync(r => r.Id == request.RezervacijaId) ||
            !await _db.Korisnici.AnyAsync(k => k.Id == request.AutorId))
        {
            return BadRequest(new { message = "Rezervacija ili autor ne postoji." });
        }

        var ocjena = new OcjenaVoznje();
        Apply(request, ocjena);
        ocjena.Administratorska = true;
        _db.Ocjene.Add(ocjena);
        await _db.SaveChangesAsync();
        await _db.Entry(ocjena).Reference(o => o.Autor).LoadAsync();
        await _db.Entry(ocjena).Collection(o => o.Slike).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = ocjena.Id }, ocjena.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OcjenaDto>> Put(int id, OcjenaRequest request)
    {
        var ocjena = await _db.Ocjene.Include(o => o.Autor).Include(o => o.Slike).FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        if (!await _db.Rezervacije.AnyAsync(r => r.Id == request.RezervacijaId) ||
            !await _db.Korisnici.AnyAsync(k => k.Id == request.AutorId))
        {
            return BadRequest(new { message = "Rezervacija ili autor ne postoji." });
        }

        Apply(request, ocjena);
        ocjena.Uredeno = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _db.Entry(ocjena).Reference(o => o.Autor).LoadAsync();
        await _db.Entry(ocjena).Collection(o => o.Slike).LoadAsync();
        return Ok(ocjena.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ocjena = await _db.Ocjene.FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        _db.Ocjene.Remove(ocjena);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(OcjenaRequest request, OcjenaVoznje ocjena)
    {
        ocjena.RezervacijaId = request.RezervacijaId;
        ocjena.AutorId = request.AutorId;
        ocjena.BrojZvjezdica = request.BrojZvjezdica;
        ocjena.Komentar = request.Komentar.Trim();
        ocjena.Kreirano = ocjena.Id == 0 ? DateTime.UtcNow : ocjena.Kreirano;
    }
}
