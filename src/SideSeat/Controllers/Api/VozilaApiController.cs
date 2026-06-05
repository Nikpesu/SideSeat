using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/vozila")]
public class VozilaApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;

    public VozilaApiController(SideSeatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VoziloDto>>> GetAll(string? q = null)
    {
        IQueryable<Vozilo> query = _db.Vozila.AsNoTracking().Include(v => v.Vlasnik);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(v => v.Marka.Contains(term) || v.Model.Contains(term) || v.Registracija.Contains(term));
        }

        var vozila = await query.OrderBy(v => v.Marka).ThenBy(v => v.Model).ToListAsync();
        return Ok(vozila.Select(v => v.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VoziloDto>> Get(int id)
    {
        var vozilo = await _db.Vozila.AsNoTracking().Include(v => v.Vlasnik).FirstOrDefaultAsync(v => v.Id == id);
        return vozilo is null ? NotFound() : Ok(vozilo.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<VoziloDto>> Post(VoziloRequest request)
    {
        if (request.VlasnikId.HasValue && !await _db.Korisnici.AnyAsync(k => k.Id == request.VlasnikId.Value))
        {
            return BadRequest(new { message = "Vlasnik ne postoji." });
        }

        var vozilo = new Vozilo();
        Apply(request, vozilo);
        _db.Vozila.Add(vozilo);
        await _db.SaveChangesAsync();
        await _db.Entry(vozilo).Reference(v => v.Vlasnik).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = vozilo.Id }, vozilo.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<VoziloDto>> Put(int id, VoziloRequest request)
    {
        var vozilo = await _db.Vozila.Include(v => v.Vlasnik).FirstOrDefaultAsync(v => v.Id == id);
        if (vozilo is null)
        {
            return NotFound();
        }

        if (request.VlasnikId.HasValue && !await _db.Korisnici.AnyAsync(k => k.Id == request.VlasnikId.Value))
        {
            return BadRequest(new { message = "Vlasnik ne postoji." });
        }

        Apply(request, vozilo);
        await _db.SaveChangesAsync();
        await _db.Entry(vozilo).Reference(v => v.Vlasnik).LoadAsync();
        return Ok(vozilo.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var vozilo = await _db.Vozila.FirstOrDefaultAsync(v => v.Id == id);
        if (vozilo is null)
        {
            return NotFound();
        }

        var inUse = await _db.Korisnici.AnyAsync(k => k.VoziloId == id);
        if (inUse)
        {
            return UnprocessableEntity(new { message = "Vozilo je povezano s korisnikom." });
        }

        _db.Vozila.Remove(vozilo);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(VoziloRequest request, Vozilo vozilo)
    {
        vozilo.Marka = request.Marka.Trim();
        vozilo.Model = request.Model.Trim();
        vozilo.Registracija = request.Registracija.Trim();
        vozilo.GodinaProizvodnje = request.GodinaProizvodnje;
        vozilo.BrojSjedala = request.BrojSjedala;
        vozilo.Boja = request.Boja.Trim();
        vozilo.ProsjecnaPotrosnja = request.ProsjecnaPotrosnja;
        vozilo.VlasnikId = request.VlasnikId;
    }
}
