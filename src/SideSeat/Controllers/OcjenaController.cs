using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Ocjena;
using SideSeat.Models.ViewModels;
using SideSeat.Repositories;
using SideSeat.Security;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje sve ocjene voznji i detalje pojedine ocjene.
/// </summary>
[Authorize]
public class OcjenaController : Controller
{
    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;

    public OcjenaController(SideSeatEfRepository repository, SideSeatDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public IActionResult Index()
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var relevantReservations = _db.Rezervacije
            .AsNoTracking()
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.Vozac)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .Where(r => r.PutnikId == userId.Value || r.Voznja.VozacId == userId.Value)
            .ToList();

        var reservationIds = relevantReservations.Select(r => r.Id).ToHashSet();
        var ratings = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Putnik)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .ThenInclude(v => v.Vozac)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .Where(o => reservationIds.Contains(o.RezervacijaId))
            .OrderByDescending(o => o.Kreirano)
            .ToList();

        var given = ratings
            .Where(o => o.AutorId == userId.Value)
            .Select(MapHistoryRow)
            .ToList();

        var received = ratings
            .Where(o =>
            {
                var targetId = ResolveTargetId(o);
                return targetId == userId.Value;
            })
            .Select(MapHistoryRow)
            .ToList();

        var ratedByMeIds = ratings
            .Where(o => o.AutorId == userId.Value)
            .Select(o => o.RezervacijaId)
            .ToHashSet();

        var pending = relevantReservations
            .Where(r =>
                r.Voznja.Status == StatusVoznje.Zavrsena &&
                r.Status != StatusRezervacije.Otkazana &&
                !ratedByMeIds.Contains(r.Id))
            .OrderByDescending(r => r.Voznja.Polazak)
            .Select(r => new OcjenaPendingItemViewModel
            {
                RezervacijaId = r.Id,
                TargetName = r.Voznja.VozacId == userId.Value
                    ? $"{r.Putnik.Ime} {r.Putnik.Prezime}".Trim()
                    : $"{r.Voznja.Vozac.Ime} {r.Voznja.Vozac.Prezime}".Trim(),
                RouteLabel = $"{r.Voznja.PolazniGrad.Naziv} -> {r.Voznja.OdredisniGrad.Naziv}",
                RideDate = r.Voznja.Polazak
            })
            .ToList();

        var vm = new OcjenaTabViewModel
        {
            Pending = pending,
            Given = given,
            Received = received,
            GivenAverage = given.Count == 0 ? 0 : given.Average(x => x.BrojZvjezdica),
            ReceivedAverage = received.Count == 0 ? 0 : received.Average(x => x.BrojZvjezdica)
        };

        return View(vm);
    }

    private static int ResolveTargetId(OcjenaVoznje o) =>
        o.AutorId == o.Rezervacija.Voznja.VozacId
            ? o.Rezervacija.PutnikId
            : o.Rezervacija.Voznja.VozacId;

    private static OcjenaHistoryItemViewModel MapHistoryRow(OcjenaVoznje o)
    {
        var targetIsPassenger = o.AutorId == o.Rezervacija.Voznja.VozacId;
        var targetName = targetIsPassenger
            ? $"{o.Rezervacija.Putnik.Ime} {o.Rezervacija.Putnik.Prezime}".Trim()
            : $"{o.Rezervacija.Voznja.Vozac.Ime} {o.Rezervacija.Voznja.Vozac.Prezime}".Trim();

        return new OcjenaHistoryItemViewModel
        {
            OcjenaId = o.Id,
            RezervacijaId = o.RezervacijaId,
            AuthorName = $"{o.Autor.Ime} {o.Autor.Prezime}".Trim(),
            TargetName = targetName,
            BrojZvjezdica = o.BrojZvjezdica,
            Komentar = o.Komentar,
            Kreirano = o.Kreirano,
            RouteLabel = $"{o.Rezervacija.Voznja.PolazniGrad.Naziv} -> {o.Rezervacija.Voznja.OdredisniGrad.Naziv}"
        };
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Details(int id)
    {
        var ocjena = _repository.GetOcjenaById(id);
        if (ocjena is null)
        {
            return NotFound();
        }

        return View(ocjena);
    }

    public IActionResult Create(int rezervacijaId)
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
            .ThenInclude(v => v.Vozac)
            .FirstOrDefault(r => r.Id == rezervacijaId);
        if (rezervacija is null)
        {
            return NotFound();
        }

        if (rezervacija.Voznja.Status != StatusVoznje.Zavrsena || rezervacija.Status == StatusRezervacije.Otkazana)
        {
            return BadRequest("Ocjenjivanje nije dostupno za ovu voznju.");
        }

        if (rezervacija.PutnikId != userId.Value && rezervacija.Voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        var alreadyRated = _db.Ocjene.Any(o => o.RezervacijaId == rezervacijaId && o.AutorId == userId.Value);
        if (alreadyRated)
        {
            return RedirectToAction("Index", "Rezervacija");
        }

        var targetName = rezervacija.Voznja.VozacId == userId.Value
            ? $"{rezervacija.Putnik.Ime} {rezervacija.Putnik.Prezime}"
            : $"{rezervacija.Voznja.Vozac.Ime} {rezervacija.Voznja.Vozac.Prezime}";

        return View(new CreateOcjenaViewModel
        {
            RezervacijaId = rezervacijaId,
            TargetName = targetName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOcjenaViewModel model)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var rezervacija = await _db.Rezervacije
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.Vozac)
            .FirstOrDefaultAsync(r => r.Id == model.RezervacijaId);
        if (rezervacija is null)
        {
            return NotFound();
        }

        if (rezervacija.Voznja.Status != StatusVoznje.Zavrsena || rezervacija.Status == StatusRezervacije.Otkazana)
        {
            return BadRequest("Ocjenjivanje nije dostupno za ovu voznju.");
        }

        if (rezervacija.PutnikId != userId.Value && rezervacija.Voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        var alreadyRated = await _db.Ocjene.AnyAsync(o => o.RezervacijaId == model.RezervacijaId && o.AutorId == userId.Value);
        if (alreadyRated)
        {
            return RedirectToAction("Index", "Rezervacija");
        }

        if (!ModelState.IsValid)
        {
            model.TargetName = rezervacija.Voznja.VozacId == userId.Value
                ? $"{rezervacija.Putnik.Ime} {rezervacija.Putnik.Prezime}"
                : $"{rezervacija.Voznja.Vozac.Ime} {rezervacija.Voznja.Vozac.Prezime}";
            return View(model);
        }

        _db.Ocjene.Add(new OcjenaVoznje
        {
            RezervacijaId = model.RezervacijaId,
            AutorId = userId.Value,
            BrojZvjezdica = model.BrojZvjezdica,
            Komentar = model.Komentar.Trim(),
            Kreirano = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Rezervacija");
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminCreate()
    {
        return View(new OcjenaAdminFormViewModel
        {
            Kreirano = DateTime.UtcNow
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminCreate(OcjenaAdminFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rezervacijaExists = await _db.Rezervacije.AnyAsync(r => r.Id == model.RezervacijaId);
        if (!rezervacijaExists)
        {
            ModelState.AddModelError(nameof(model.RezervacijaId), "Rezervacija ne postoji.");
            return View(model);
        }

        var autorExists = await _db.Korisnici.AnyAsync(k => k.Id == model.AutorId);
        if (!autorExists)
        {
            ModelState.AddModelError(nameof(model.AutorId), "Autor ne postoji.");
            return View(model);
        }

        _db.Ocjene.Add(new OcjenaVoznje
        {
            RezervacijaId = model.RezervacijaId,
            AutorId = model.AutorId,
            BrojZvjezdica = model.BrojZvjezdica,
            Komentar = model.Komentar.Trim(),
            Kreirano = model.Kreirano
        });

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminEdit(int id)
    {
        var ocjena = _db.Ocjene.AsNoTracking().FirstOrDefault(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        return View(new OcjenaAdminFormViewModel
        {
            Id = ocjena.Id,
            RezervacijaId = ocjena.RezervacijaId,
            AutorId = ocjena.AutorId,
            BrojZvjezdica = ocjena.BrojZvjezdica,
            Komentar = ocjena.Komentar,
            Kreirano = ocjena.Kreirano
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminEdit(int id, OcjenaAdminFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ocjena = await _db.Ocjene.FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        var rezervacijaExists = await _db.Rezervacije.AnyAsync(r => r.Id == model.RezervacijaId);
        if (!rezervacijaExists)
        {
            ModelState.AddModelError(nameof(model.RezervacijaId), "Rezervacija ne postoji.");
            return View(model);
        }

        var autorExists = await _db.Korisnici.AnyAsync(k => k.Id == model.AutorId);
        if (!autorExists)
        {
            ModelState.AddModelError(nameof(model.AutorId), "Autor ne postoji.");
            return View(model);
        }

        ocjena.RezervacijaId = model.RezervacijaId;
        ocjena.AutorId = model.AutorId;
        ocjena.BrojZvjezdica = model.BrojZvjezdica;
        ocjena.Komentar = model.Komentar.Trim();
        ocjena.Kreirano = model.Kreirano;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = ocjena.Id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminDelete(int id)
    {
        var ocjena = _repository.GetOcjenaById(id);
        if (ocjena is null)
        {
            return NotFound();
        }

        return View(ocjena);
    }

    [HttpPost, ActionName("AdminDelete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminDeleteConfirmed(int id)
    {
        var ocjena = await _db.Ocjene.FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        _db.Ocjene.Remove(ocjena);
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
