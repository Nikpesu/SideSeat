using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Api;

namespace SideSeat.Controllers.Api;

[ApiController]
[Route("api/gradovi")]
public class GradoviApiController : ControllerBase
{
    private readonly SideSeatDbContext _db;

    public GradoviApiController(SideSeatDbContext db)
    {
        _db = db;
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
    public async Task<ActionResult<GradDto>> Post(GradRequest request)
    {
        var grad = new Grad
        {
            Naziv = request.Naziv.Trim(),
            Drzava = request.Drzava.Trim(),
            PostanskiBroj = request.PostanskiBroj.Trim()
        };
        _db.Gradovi.Add(grad);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = grad.Id }, grad.ToDto());
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GradDto>> Put(int id, GradRequest request)
    {
        var grad = await _db.Gradovi.FirstOrDefaultAsync(g => g.Id == id);
        if (grad is null)
        {
            return NotFound();
        }

        grad.Naziv = request.Naziv.Trim();
        grad.Drzava = request.Drzava.Trim();
        grad.PostanskiBroj = request.PostanskiBroj.Trim();
        await _db.SaveChangesAsync();
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
}
