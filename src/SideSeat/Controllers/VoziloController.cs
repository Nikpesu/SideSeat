using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Repositories;
using SideSeat.Models.ViewModels;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje sva vozila i detalje pojedinog vozila.
/// </summary>
[Authorize(Roles = "Admin")]
public class VoziloController : Controller
{
    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;

    public VoziloController(SideSeatEfRepository repository, SideSeatDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public IActionResult Index(string? search, int? pageSize)
    {
        var vozila = _repository.GetVozila();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            vozila = vozila.Where(vozilo =>
                vozilo.Marka.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                vozilo.Model.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                vozilo.Registracija.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                vozilo.Boja.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                vozilo.Vlasnik?.Ime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                vozilo.Vlasnik?.Prezime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                vozilo.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Search = search;
        ViewBag.PageSize = pageSize;

        if (pageSize.HasValue)
        {
            var normalized = PageSizeOptions.Normalize(pageSize.Value);
            if (normalized > 0)
            {
                vozila = vozila.Take(normalized).ToList();
            }
        }

        return View(vozila);
    }

    public IActionResult Details(int id)
    {
        var vozilo = _repository.GetVoziloById(id);
        if (vozilo is null)
        {
            return NotFound();
        }

        return View(vozilo);
    }

    public IActionResult Create()
    {
        return View(new Vozilo());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Vozilo voziloInput)
    {
        if (!ModelState.IsValid)
        {
            return View(voziloInput);
        }

        _db.Vozila.Add(voziloInput);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var vozilo = _db.Vozila
            .AsNoTracking()
            .Include(v => v.Vlasnik)
            .FirstOrDefault(v => v.Id == id);
        if (vozilo is null)
        {
            return NotFound();
        }

        return View(vozilo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Vozilo voziloInput)
    {
        if (id != voziloInput.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(voziloInput);
        }

        var vozilo = await _db.Vozila.FirstOrDefaultAsync(v => v.Id == id);
        if (vozilo is null)
        {
            return NotFound();
        }

        if (voziloInput.VlasnikId.HasValue)
        {
            var ownerExists = await _db.Korisnici.AnyAsync(k => k.Id == voziloInput.VlasnikId.Value);
            if (!ownerExists)
            {
                ModelState.AddModelError(nameof(voziloInput.VlasnikId), "Odabrani vlasnik ne postoji.");
                return View(voziloInput);
            }
        }

        vozilo.Marka = voziloInput.Marka.Trim();
        vozilo.Model = voziloInput.Model.Trim();
        vozilo.Registracija = voziloInput.Registracija.Trim();
        vozilo.GodinaProizvodnje = voziloInput.GodinaProizvodnje;
        vozilo.BrojSjedala = voziloInput.BrojSjedala;
        vozilo.Boja = voziloInput.Boja.Trim();
        vozilo.ProsjecnaPotrosnja = voziloInput.ProsjecnaPotrosnja;
        vozilo.VlasnikId = voziloInput.VlasnikId;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = vozilo.Id });
    }

    public IActionResult Delete(int id)
    {
        var vozilo = _repository.GetVoziloById(id);
        if (vozilo is null)
        {
            return NotFound();
        }

        return View(vozilo);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var vozilo = await _db.Vozila.FirstOrDefaultAsync(v => v.Id == id);
        if (vozilo is null)
        {
            return NotFound();
        }

        var linkedUsers = await _db.Korisnici.Where(k => k.VoziloId == id).ToListAsync();
        foreach (var korisnik in linkedUsers)
        {
            korisnik.VoziloId = null;
        }

        _db.Vozila.Remove(vozilo);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult SearchVehicles(string q)
    {
        var query = _db.Vozila.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.Marka, $"%{normalized}%") ||
                EF.Functions.Like(v.Model, $"%{normalized}%") ||
                EF.Functions.Like(v.Registracija, $"%{normalized}%"));
        }

        var results = query
            .OrderBy(v => v.Marka)
            .ThenBy(v => v.Model)
            .ThenBy(v => v.Registracija)
            .ThenBy(v => v.Id)
            .Take(50)
            .Select(v => new
            {
                id = v.Id.ToString(),
                text = $"{v.Marka} {v.Model}",
                subtext = v.Registracija
            })
            .ToList();

        return Json(results);
    }
}
