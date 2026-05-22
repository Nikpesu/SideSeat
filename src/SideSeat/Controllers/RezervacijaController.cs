using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Lab3;
using SideSeat.Models.ViewModels;
using SideSeat.Security;

namespace SideSeat.Controllers;

[Authorize]
public class RezervacijaController : Controller
{
    private readonly SideSeatDbContext _db;

    public RezervacijaController(SideSeatDbContext db)
    {
        _db = db;
    }

    public IActionResult Index(string? search, DateTime? date, int? pageSize)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var query = _db.Rezervacije
            .AsNoTracking()
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.Vozac)
            .OrderByDescending(r => r.VrijemeRezervacije)
            .AsQueryable();

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(r => r.PutnikId == userId.Value || r.Voznja.VozacId == userId.Value);
        }

        var rezervacije = query.ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            rezervacije = rezervacije.Where(rezervacija =>
                rezervacija.Putnik.Ime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                rezervacija.Putnik.Prezime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                rezervacija.Voznja.PolazniGrad.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                rezervacija.Voznja.OdredisniGrad.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                rezervacija.Status.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                rezervacija.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            rezervacije = rezervacije.Where(r => r.VrijemeRezervacije.Date == selectedDate).ToList();
        }
        var ratedReservationIds = _db.Ocjene
            .AsNoTracking()
            .Where(o => o.AutorId == userId.Value)
            .Select(o => o.RezervacijaId)
            .ToHashSet();

        var items = rezervacije.Select(rezervacija =>
        {
            var canRate = rezervacija.Voznja?.Status == StatusVoznje.Zavrsena
                          && rezervacija.Status != StatusRezervacije.Otkazana
                          && (rezervacija.PutnikId == userId.Value || rezervacija.Voznja?.VozacId == userId.Value)
                          && !ratedReservationIds.Contains(rezervacija.Id);

            return new RezervacijaListItemViewModel
            {
                Rezervacija = rezervacija,
                CanRate = canRate,
                HasRated = ratedReservationIds.Contains(rezervacija.Id),
                RateTargetLabel = rezervacija.Voznja?.VozacId == userId.Value ? "Ocijeni putnika" : "Ocijeni vozaca"
            };
        }).ToList();

        ViewBag.Search = search;
        ViewBag.Date = date;
        ViewBag.PageSize = pageSize;

        if (pageSize.HasValue)
        {
            var normalized = PageSizeOptions.Normalize(pageSize.Value);
            if (normalized > 0)
            {
                items = items.Take(normalized).ToList();
            }
        }

        return View(items);
    }

    public IActionResult Details(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var rezervacija = _db.Rezervacije
            .AsNoTracking()
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .FirstOrDefault(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        var canAccess = User.IsInRole("Admin")
                        || rezervacija.PutnikId == userId.Value
                        || rezervacija.Voznja.VozacId == userId.Value;
        if (!canAccess)
        {
            return Forbid();
        }

        return View(rezervacija);
    }

    public IActionResult Create(int voznjaId)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = _db.Voznje
            .AsNoTracking()
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .FirstOrDefault(v => v.Id == voznjaId);
        if (voznja is null || voznja.Status != StatusVoznje.Planirana)
        {
            return NotFound();
        }

        if (voznja.SlobodnaMjesta <= 0)
        {
            return BadRequest("Voznja nema slobodnih mjesta.");
        }

        ViewBag.Voznja = voznja;
        return View(new RezervacijaFormViewModel { VoznjaId = voznjaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(RezervacijaFormViewModel model)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = _db.Voznje
            .FirstOrDefault(v => v.Id == model.VoznjaId);
        if (voznja is null || voznja.Status != StatusVoznje.Planirana)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Voznja = _db.Voznje
                .AsNoTracking()
                .Include(v => v.PolazniGrad)
                .Include(v => v.OdredisniGrad)
                .First(v => v.Id == model.VoznjaId);
            return View(model);
        }

        if (model.BrojMjesta > voznja.SlobodnaMjesta)
        {
            ModelState.AddModelError(nameof(model.BrojMjesta), "Nema dovoljno slobodnih mjesta za trazeni broj.");
            ViewBag.Voznja = _db.Voznje
                .AsNoTracking()
                .Include(v => v.PolazniGrad)
                .Include(v => v.OdredisniGrad)
                .First(v => v.Id == model.VoznjaId);
            return View(model);
        }

        var rezervacija = new Rezervacija
        {
            VoznjaId = voznja.Id,
            PutnikId = userId.Value,
            BrojMjesta = model.BrojMjesta,
            CijenaUkupno = voznja.CijenaPoMjestu * model.BrojMjesta,
            VrijemeRezervacije = DateTime.UtcNow,
            Status = StatusRezervacije.Aktivna,
            Napomena = model.Napomena.Trim()
        };

        voznja.SlobodnaMjesta -= model.BrojMjesta;
        _db.Rezervacije.Add(rezervacija);
        _db.SaveChanges();

        return RedirectToAction("Reservation", "Confirmation", new { id = rezervacija.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirm(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var rezervacija = _db.Rezervacije
            .Include(r => r.Voznja)
            .FirstOrDefault(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        var canConfirm = User.IsInRole("Admin") || rezervacija.Voznja.VozacId == userId.Value;
        if (!canConfirm)
        {
            return Forbid();
        }

        rezervacija.Status = StatusRezervacije.Potvrdena;
        _db.SaveChanges();

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminCreate()
    {
        return View(new RezervacijaAdminFormViewModel
        {
            Status = StatusRezervacije.Aktivna,
            VrijemeRezervacije = DateTime.UtcNow
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminCreate(RezervacijaAdminFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var voznja = await _db.Voznje.FirstOrDefaultAsync(v => v.Id == model.VoznjaId);
        if (voznja is null)
        {
            ModelState.AddModelError(nameof(model.VoznjaId), "Odabrana voznja ne postoji.");
            return View(model);
        }

        var putnikExists = await _db.Korisnici.AnyAsync(k => k.Id == model.PutnikId);
        if (!putnikExists)
        {
            ModelState.AddModelError(nameof(model.PutnikId), "Odabrani putnik ne postoji.");
            return View(model);
        }

        if (model.BrojMjesta > voznja.SlobodnaMjesta)
        {
            ModelState.AddModelError(nameof(model.BrojMjesta), "Nema dovoljno slobodnih mjesta.");
            return View(model);
        }

        var rezervacija = new Rezervacija
        {
            VoznjaId = voznja.Id,
            PutnikId = model.PutnikId,
            BrojMjesta = model.BrojMjesta,
            CijenaUkupno = voznja.CijenaPoMjestu * model.BrojMjesta,
            VrijemeRezervacije = model.VrijemeRezervacije,
            Status = model.Status,
            Napomena = model.Napomena.Trim()
        };

        voznja.SlobodnaMjesta -= model.BrojMjesta;
        _db.Rezervacije.Add(rezervacija);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = rezervacija.Id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminEdit(int id)
    {
        var rezervacija = _db.Rezervacije
            .AsNoTracking()
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .FirstOrDefault(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        return View(new RezervacijaAdminFormViewModel
        {
            Id = rezervacija.Id,
            VoznjaId = rezervacija.VoznjaId,
            PutnikId = rezervacija.PutnikId,
            BrojMjesta = rezervacija.BrojMjesta,
            Status = rezervacija.Status,
            VrijemeRezervacije = rezervacija.VrijemeRezervacije,
            Napomena = rezervacija.Napomena,
            CijenaUkupno = rezervacija.CijenaUkupno,
            PutnikNaziv = rezervacija.Putnik is null ? string.Empty : $"{rezervacija.Putnik.Ime} {rezervacija.Putnik.Prezime}",
            VoznjaNaziv = rezervacija.Voznja is null
                ? string.Empty
                : $"{rezervacija.Voznja.PolazniGrad?.Naziv} -> {rezervacija.Voznja.OdredisniGrad?.Naziv}"
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminEdit(int id, RezervacijaAdminFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rezervacija = await _db.Rezervacije
            .Include(r => r.Voznja)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        var putnikExists = await _db.Korisnici.AnyAsync(k => k.Id == model.PutnikId);
        if (!putnikExists)
        {
            ModelState.AddModelError(nameof(model.PutnikId), "Odabrani putnik ne postoji.");
            return View(model);
        }

        if (rezervacija.VoznjaId == model.VoznjaId)
        {
            var delta = model.BrojMjesta - rezervacija.BrojMjesta;
            if (delta > 0 && rezervacija.Voznja.SlobodnaMjesta < delta)
            {
                ModelState.AddModelError(nameof(model.BrojMjesta), "Nema dovoljno slobodnih mjesta za povecanje.");
                return View(model);
            }

            rezervacija.Voznja.SlobodnaMjesta -= delta;
        }
        else
        {
            var novaVoznja = await _db.Voznje.FirstOrDefaultAsync(v => v.Id == model.VoznjaId);
            if (novaVoznja is null)
            {
                ModelState.AddModelError(nameof(model.VoznjaId), "Odabrana voznja ne postoji.");
                return View(model);
            }

            if (novaVoznja.SlobodnaMjesta < model.BrojMjesta)
            {
                ModelState.AddModelError(nameof(model.BrojMjesta), "Nema dovoljno slobodnih mjesta na novoj voznji.");
                return View(model);
            }

            rezervacija.Voznja.SlobodnaMjesta += rezervacija.BrojMjesta;
            novaVoznja.SlobodnaMjesta -= model.BrojMjesta;
            rezervacija.VoznjaId = model.VoznjaId;
            rezervacija.Voznja = novaVoznja;
        }

        rezervacija.PutnikId = model.PutnikId;
        rezervacija.BrojMjesta = model.BrojMjesta;
        rezervacija.Status = model.Status;
        rezervacija.VrijemeRezervacije = model.VrijemeRezervacije;
        rezervacija.Napomena = model.Napomena.Trim();
        rezervacija.CijenaUkupno = rezervacija.Voznja.CijenaPoMjestu * model.BrojMjesta;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = rezervacija.Id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminDelete(int id)
    {
        var rezervacija = _db.Rezervacije
            .AsNoTracking()
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .FirstOrDefault(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        return View(rezervacija);
    }

    [HttpPost, ActionName("AdminDelete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminDeleteConfirmed(int id)
    {
        var rezervacija = await _db.Rezervacije
            .Include(r => r.Voznja)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        var ocjene = await _db.Ocjene
            .Where(o => o.RezervacijaId == id)
            .ToListAsync();
        if (ocjene.Count > 0)
        {
            _db.Ocjene.RemoveRange(ocjene);
        }

        var placanja = await _db.Placanja
            .Where(p => p.RezervacijaId == id)
            .ToListAsync();
        if (placanja.Count > 0)
        {
            _db.Placanja.RemoveRange(placanja);
        }

        rezervacija.Voznja.SlobodnaMjesta += rezervacija.BrojMjesta;
        _db.Rezervacije.Remove(rezervacija);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult SearchUsers(string q)
    {
        var query = _db.Korisnici.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(k =>
                EF.Functions.Like(k.Ime, $"%{normalized}%") ||
                EF.Functions.Like(k.Prezime, $"%{normalized}%") ||
                EF.Functions.Like(k.Email, $"%{normalized}%"));
        }

        var results = query
            .OrderBy(k => k.Prezime)
            .ThenBy(k => k.Ime)
            .Take(50)
            .Select(k => new
            {
                id = k.Id.ToString(),
                text = $"{k.Ime} {k.Prezime}",
                subtext = k.Email
            })
            .ToList();

        return Json(results);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult SearchRides(string q)
    {
        var query = _db.Voznje
            .AsNoTracking()
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(v =>
                EF.Functions.Like(v.PolazniGrad.Naziv, $"%{normalized}%") ||
                EF.Functions.Like(v.OdredisniGrad.Naziv, $"%{normalized}%"));

            if (int.TryParse(normalized, out var rideId))
            {
                query = query.Where(v => v.Id == rideId);
            }
        }

        var results = query
            .OrderBy(v => v.PolazniGrad.Naziv)
            .ThenBy(v => v.OdredisniGrad.Naziv)
            .ThenBy(v => v.Polazak)
            .ThenBy(v => v.Id)
            .Take(50)
            .Select(v => new
            {
                id = v.Id.ToString(),
                text = $"{v.PolazniGrad.Naziv} -> {v.OdredisniGrad.Naziv}",
                subtext = v.Polazak.ToString("dd.MM.yyyy HH:mm")
            })
            .ToList();

        return Json(results);
    }
}
