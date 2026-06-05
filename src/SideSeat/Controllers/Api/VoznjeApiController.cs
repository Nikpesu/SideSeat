using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;
using SideSeat.Security;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/voznje")]
public class VoznjeApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;

    public VoznjeApiController(SideSeatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VoznjaDto>>> GetAll(string? q = null, DateTime? date = null)
    {
        var query = IncludeGraph(_db.Voznje.AsNoTracking());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(v =>
                v.Opis.Contains(term) ||
                v.PolazniGrad.Naziv.Contains(term) ||
                v.OdredisniGrad.Naziv.Contains(term) ||
                v.Vozac.Ime.Contains(term) ||
                v.Vozac.Prezime.Contains(term));
        }

        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            query = query.Where(v => v.Polazak.Date == selectedDate);
        }

        var voznje = await query.OrderBy(v => v.Polazak).ToListAsync();
        return Ok(voznje.Select(v => v.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VoznjaDto>> Get(int id)
    {
        var voznja = await IncludeGraph(_db.Voznje.AsNoTracking()).FirstOrDefaultAsync(v => v.Id == id);
        return voznja is null ? NotFound() : Ok(voznja.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<ActionResult<VoznjaDto>> Post(VoznjaRequest request)
    {
        if (!User.IsInRole("Admin") && User.GetKorisnikId() != request.VozacId)
        {
            return Forbid();
        }

        var validation = await ValidateRefs(request);
        if (validation is not null)
        {
            return validation;
        }

        var voznja = new Voznja();
        Apply(request, voznja);
        _db.Voznje.Add(voznja);
        await _db.SaveChangesAsync();
        voznja = await IncludeGraph(_db.Voznje).FirstAsync(v => v.Id == voznja.Id);
        return CreatedAtAction(nameof(Get), new { id = voznja.Id }, voznja.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<ActionResult<VoznjaDto>> Put(int id, VoznjaRequest request)
    {
        var voznja = await IncludeGraph(_db.Voznje).FirstOrDefaultAsync(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        if (!CanManage(voznja))
        {
            return Forbid();
        }

        var validation = await ValidateRefs(request);
        if (validation is not null)
        {
            return validation;
        }

        Apply(request, voznja);
        await _db.SaveChangesAsync();
        voznja = await IncludeGraph(_db.Voznje.AsNoTracking()).FirstAsync(v => v.Id == id);
        return Ok(voznja.ToDto());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Driver")]
    public async Task<IActionResult> Delete(int id)
    {
        var voznja = await _db.Voznje.Include(v => v.Rezervacije).FirstOrDefaultAsync(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        if (!CanManage(voznja))
        {
            return Forbid();
        }

        if (voznja.Rezervacije.Any())
        {
            return UnprocessableEntity(new { message = "Voznja ima rezervacije i ne moze se obrisati." });
        }

        _db.Voznje.Remove(voznja);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private bool CanManage(Voznja voznja) => User.IsInRole("Admin") || User.GetKorisnikId() == voznja.VozacId;

    private async Task<ActionResult<VoznjaDto>?> ValidateRefs(VoznjaRequest request)
    {
        if (!await _db.Korisnici.AnyAsync(k => k.Id == request.VozacId))
        {
            return BadRequest(new { message = "Vozac ne postoji." });
        }

        if (!await _db.Gradovi.AnyAsync(g => g.Id == request.PolazniGradId) ||
            !await _db.Gradovi.AnyAsync(g => g.Id == request.OdredisniGradId))
        {
            return BadRequest(new { message = "Polazni ili odredisni grad ne postoji." });
        }

        if (request.OcekivaniDolazak <= request.Polazak)
        {
            return BadRequest(new { message = "Ocekivani dolazak mora biti nakon polaska." });
        }

        return null;
    }

    private static IQueryable<Voznja> IncludeGraph(IQueryable<Voznja> query) => query
        .Include(v => v.Vozac)
        .Include(v => v.PolazniGrad)
        .Include(v => v.OdredisniGrad);

    private static void Apply(VoznjaRequest request, Voznja voznja)
    {
        voznja.VozacId = request.VozacId;
        voznja.PolazniGradId = request.PolazniGradId;
        voznja.OdredisniGradId = request.OdredisniGradId;
        voznja.Polazak = request.Polazak;
        voznja.OcekivaniDolazak = request.OcekivaniDolazak;
        voznja.CijenaPoMjestu = request.CijenaPoMjestu;
        voznja.UkupnoMjesta = request.UkupnoMjesta;
        voznja.SlobodnaMjesta = request.SlobodnaMjesta;
        voznja.Opis = request.Opis.Trim();
        voznja.Status = request.Status;
    }
}
