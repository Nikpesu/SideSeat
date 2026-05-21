using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.ViewModels;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje popis placanja i detalje odabranog placanja.
/// </summary>
[Authorize(Roles = "Admin")]
public class PlacanjeController : Controller
{
    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;

    public PlacanjeController(SideSeatEfRepository repository, SideSeatDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public IActionResult Index(string? search, DateTime? date, int? pageSize)
    {
        var placanja = _repository.GetPlacanja();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            placanja = placanja.Where(placanje =>
                placanje.RezervacijaId.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                placanje.Rezervacija?.Putnik?.Ime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                placanje.Rezervacija?.Putnik?.Prezime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                placanje.NacinPlacanja.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                placanje.Uspjesno.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            placanja = placanja.Where(p => p.VrijemePlacanja.Date == selectedDate).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Date = date;
        ViewBag.PageSize = pageSize;

        if (pageSize.HasValue)
        {
            var normalized = PageSizeOptions.Normalize(pageSize.Value);
            if (normalized > 0)
            {
                placanja = placanja.Take(normalized).ToList();
            }
        }

        return View(placanja);
    }

    public IActionResult Details(int id)
    {
        var placanje = _repository.GetPlacanjeById(id);
        if (placanje is null)
        {
            return NotFound();
        }

        return View(placanje);
    }

    public IActionResult Create()
    {
        return View(new PlacanjeFormViewModel
        {
            VrijemePlacanja = DateTime.UtcNow
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlacanjeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rezervacija = await _db.Rezervacije.FirstOrDefaultAsync(r => r.Id == model.RezervacijaId);
        if (rezervacija is null)
        {
            ModelState.AddModelError(nameof(model.RezervacijaId), "Rezervacija ne postoji.");
            return View(model);
        }

        var placanje = new Placanje
        {
            RezervacijaId = model.RezervacijaId,
            Iznos = model.Iznos,
            VrijemePlacanja = model.VrijemePlacanja,
            NacinPlacanja = model.NacinPlacanja,
            Uspjesno = model.Uspjesno
        };

        _db.Placanja.Add(placanje);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = placanje.Id });
    }

    public IActionResult Edit(int id)
    {
        var placanje = _db.Placanja.AsNoTracking().FirstOrDefault(p => p.Id == id);
        if (placanje is null)
        {
            return NotFound();
        }

        return View(new PlacanjeFormViewModel
        {
            Id = placanje.Id,
            RezervacijaId = placanje.RezervacijaId,
            Iznos = placanje.Iznos,
            VrijemePlacanja = placanje.VrijemePlacanja,
            NacinPlacanja = placanje.NacinPlacanja,
            Uspjesno = placanje.Uspjesno
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PlacanjeFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var placanje = await _db.Placanja.FirstOrDefaultAsync(p => p.Id == id);
        if (placanje is null)
        {
            return NotFound();
        }

        var rezervacijaExists = await _db.Rezervacije.AnyAsync(r => r.Id == model.RezervacijaId);
        if (!rezervacijaExists)
        {
            ModelState.AddModelError(nameof(model.RezervacijaId), "Rezervacija ne postoji.");
            return View(model);
        }

        placanje.RezervacijaId = model.RezervacijaId;
        placanje.Iznos = model.Iznos;
        placanje.VrijemePlacanja = model.VrijemePlacanja;
        placanje.NacinPlacanja = model.NacinPlacanja;
        placanje.Uspjesno = model.Uspjesno;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = placanje.Id });
    }

    public IActionResult Delete(int id)
    {
        var placanje = _repository.GetPlacanjeById(id);
        if (placanje is null)
        {
            return NotFound();
        }

        return View(placanje);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var placanje = await _db.Placanja.FirstOrDefaultAsync(p => p.Id == id);
        if (placanje is null)
        {
            return NotFound();
        }

        _db.Placanja.Remove(placanje);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult SearchReservations(string q)
    {
        var query = _db.Rezervacije
            .AsNoTracking()
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(r =>
                EF.Functions.Like(r.Putnik.Ime, $"%{normalized}%") ||
                EF.Functions.Like(r.Putnik.Prezime, $"%{normalized}%"));

            if (int.TryParse(normalized, out var resId))
            {
                query = query.Where(r => r.Id == resId);
            }
        }

        var results = query
            .OrderBy(r => r.Putnik.Prezime)
            .ThenBy(r => r.Putnik.Ime)
            .ThenBy(r => r.Voznja.PolazniGrad.Naziv)
            .ThenBy(r => r.Voznja.OdredisniGrad.Naziv)
            .ThenBy(r => r.Id)
            .Take(50)
            .Select(r => new
            {
                id = r.Id.ToString(),
                text = $"Rezervacija #{r.Id}",
                subtext = $"{r.Putnik.Ime} {r.Putnik.Prezime} | {r.Voznja.PolazniGrad.Naziv} -> {r.Voznja.OdredisniGrad.Naziv}"
            })
            .ToList();

        return Json(results);
    }
}
