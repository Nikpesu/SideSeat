using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Lab3;
using SideSeat.Repositories;
using SideSeat.Models.ViewModels;
using SideSeat.Models.Rides;
using SideSeat.Security;

namespace SideSeat.Controllers;

[Authorize]
public class VoznjaController : Controller
{
    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;

    public VoznjaController(SideSeatEfRepository repository, SideSeatDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public IActionResult Index(string? search, DateTime? date, int? pageSize)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        if (User.IsInRole("Admin"))
        {
            var adminVoznje = _repository.GetVoznje();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                adminVoznje = adminVoznje.Where(voznja =>
                    voznja.PolazniGrad?.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                    voznja.OdredisniGrad?.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                    voznja.Vozac?.Ime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                    voznja.Vozac?.Prezime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                    voznja.Status.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    voznja.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (date.HasValue)
            {
                var selectedDate = date.Value.Date;
                adminVoznje = adminVoznje.Where(v => v.Polazak.Date == selectedDate).ToList();
            }

            ViewBag.Search = search;
            ViewBag.Date = date;
            ViewBag.PageSize = pageSize;

            if (pageSize.HasValue)
            {
                var normalized = PageSizeOptions.Normalize(pageSize.Value);
                if (normalized > 0)
                {
                    adminVoznje = adminVoznje.Take(normalized).ToList();
                }
            }

            return View(adminVoznje);
        }

        var userVoznje = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Where(v => v.VozacId == userId.Value || v.Rezervacije.Any(r => r.PutnikId == userId.Value))
            .OrderBy(v => v.Polazak)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            userVoznje = userVoznje.Where(voznja =>
                voznja.PolazniGrad?.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.OdredisniGrad?.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.Vozac?.Ime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.Vozac?.Prezime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.Status.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                voznja.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            userVoznje = userVoznje.Where(v => v.Polazak.Date == selectedDate).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Date = date;
        ViewBag.PageSize = pageSize;

        if (pageSize.HasValue)
        {
            var normalized = PageSizeOptions.Normalize(pageSize.Value);
            if (normalized > 0)
            {
                userVoznje = userVoznje.Take(normalized).ToList();
            }
        }

        return View(userVoznje);
    }

    public IActionResult Active(string? search, DateTime? date, int? pageSize)
    {
        var voznje = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Where(v => v.Status == StatusVoznje.Planirana && v.SlobodnaMjesta > 0)
            .OrderBy(v => v.Polazak)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            voznje = voznje.Where(voznja =>
                voznja.PolazniGrad?.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.OdredisniGrad?.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.Vozac?.Ime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.Vozac?.Prezime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true ||
                voznja.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            voznje = voznje.Where(v => v.Polazak.Date == selectedDate).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Date = date;
        ViewBag.PageSize = pageSize;

        if (pageSize.HasValue)
        {
            var normalized = PageSizeOptions.Normalize(pageSize.Value);
            if (normalized > 0)
            {
                voznje = voznje.Take(normalized).ToList();
            }
        }

        return View(voznje);
    }

    public IActionResult Details(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Include(v => v.Rezervacije)
            .ThenInclude(r => r.Putnik)
            .FirstOrDefault(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        var canAccess = User.IsInRole("Admin")
                        || voznja.VozacId == userId.Value
                        || voznja.Rezervacije.Any(r => r.PutnikId == userId.Value)
                        || voznja.Status == StatusVoznje.Planirana;
        if (!canAccess)
        {
            return Forbid();
        }

        var ocjeneVoznje = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Putnik)
            .Where(o => o.Rezervacija.VoznjaId == id)
            .OrderByDescending(o => o.Kreirano)
            .ToList();

        var ocjeneVoznjeRows = ocjeneVoznje.Select(o => new VoznjaRatingRow
        {
            OcjenaId = o.Id,
            RezervacijaId = o.RezervacijaId,
            AutorIme = $"{o.Autor.Ime} {o.Autor.Prezime}".Trim(),
            PrimateljIme = o.AutorId == voznja.VozacId
                ? $"{o.Rezervacija.Putnik.Ime} {o.Rezervacija.Putnik.Prezime}".Trim()
                : $"{voznja.Vozac.Ime} {voznja.Vozac.Prezime}".Trim(),
            BrojZvjezdica = o.BrojZvjezdica,
            Komentar = o.Komentar,
            Kreirano = o.Kreirano
        }).ToList();

        var ocjeneVozaca = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .Where(o => o.Rezervacija.Voznja.VozacId == voznja.VozacId && o.AutorId != voznja.VozacId)
            .OrderByDescending(o => o.Kreirano)
            .ToList();

        var ocjeneVozacaRows = ocjeneVozaca.Select(o => new VoznjaRatingRow
        {
            OcjenaId = o.Id,
            RezervacijaId = o.RezervacijaId,
            AutorIme = $"{o.Autor.Ime} {o.Autor.Prezime}".Trim(),
            PrimateljIme = $"{voznja.Vozac.Ime} {voznja.Vozac.Prezime}".Trim(),
            BrojZvjezdica = o.BrojZvjezdica,
            Komentar = o.Komentar,
            Kreirano = o.Kreirano
        }).ToList();

        var ocjenaByRezervacijaAutor = ocjeneVoznje
            .Select(o => (o.RezervacijaId, o.AutorId))
            .ToHashSet();

        var putnici = voznja.Rezervacije
            .OrderBy(r => r.VrijemeRezervacije)
            .Select(r => new VoznjaPassengerRow
            {
                RezervacijaId = r.Id,
                PutnikId = r.PutnikId,
                PutnikIme = $"{r.Putnik.Ime} {r.Putnik.Prezime}".Trim(),
                Status = r.Status,
                BrojMjesta = r.BrojMjesta,
                VozacJeOcijenio = ocjenaByRezervacijaAutor.Contains((r.Id, voznja.VozacId)),
                PutnikJeOcijenio = ocjenaByRezervacijaAutor.Contains((r.Id, r.PutnikId))
            })
            .ToList();

        var model = new VoznjaDetailsViewModel
        {
            Voznja = voznja,
            Putnici = putnici,
            OcjeneVoznje = ocjeneVoznjeRows,
            BrojOcjenaVoznje = ocjeneVoznjeRows.Count,
            ProsjecnaOcjenaVoznje = ocjeneVoznjeRows.Count == 0 ? 0 : ocjeneVoznjeRows.Average(x => x.BrojZvjezdica),
            OcjeneVozaca = ocjeneVozacaRows,
            BrojOcjenaVozaca = ocjeneVozacaRows.Count,
            ProsjecnaOcjenaVozaca = ocjeneVozacaRows.Count == 0 ? 0 : ocjeneVozacaRows.Average(x => x.BrojZvjezdica)
        };

        return View(model);
    }

    public IActionResult Create()
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && !CurrentUserCanDrive(userId.Value))
        {
            TempData["KycRequired"] = "Za kreiranje voznje aktivirajte vozacku ulogu u postavkama.";
            return RedirectToAction("Settings", "Korisnik");
        }

        var model = BuildFormViewModel(isAdmin, userId.Value);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(VoznjaFormViewModel model)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && !CurrentUserCanDrive(userId.Value))
        {
            TempData["KycRequired"] = "Za kreiranje voznje aktivirajte vozacku ulogu u postavkama.";
            return RedirectToAction("Settings", "Korisnik");
        }

        if (!isAdmin)
        {
            model.VozacId = userId.Value;
            model.Status = StatusVoznje.Planirana;
        }

        if (!ModelState.IsValid)
        {
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        if (model.PolazniGradId == model.OdredisniGradId)
        {
            ModelState.AddModelError(nameof(model.OdredisniGradId), "Polazni i odredisni grad moraju biti razliciti.");
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        if (model.OcekivaniDolazak <= model.Polazak)
        {
            ModelState.AddModelError(nameof(model.OcekivaniDolazak), "Ocekivani dolazak mora biti nakon polaska.");
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        if (model.SlobodnaMjesta > model.UkupnoMjesta)
        {
            ModelState.AddModelError(nameof(model.SlobodnaMjesta), "Slobodna mjesta ne mogu biti veca od ukupnog broja mjesta.");
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        var voznja = new Voznja
        {
            VozacId = model.VozacId,
            PolazniGradId = model.PolazniGradId,
            OdredisniGradId = model.OdredisniGradId,
            Polazak = model.Polazak,
            OcekivaniDolazak = model.OcekivaniDolazak,
            CijenaPoMjestu = model.CijenaPoMjestu,
            UkupnoMjesta = model.UkupnoMjesta,
            SlobodnaMjesta = model.SlobodnaMjesta,
            Opis = model.Opis,
            Status = model.Status
        };

        _db.Voznje.Add(voznja);
        _db.SaveChanges();

        return RedirectToAction("Ride", "Confirmation", new { id = voznja.Id });
    }

    public IActionResult Edit(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = _db.Voznje.AsNoTracking().FirstOrDefault(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        var model = new VoznjaFormViewModel
        {
            Id = voznja.Id,
            VozacId = voznja.VozacId,
            PolazniGradId = voznja.PolazniGradId,
            OdredisniGradId = voznja.OdredisniGradId,
            Polazak = voznja.Polazak,
            OcekivaniDolazak = voznja.OcekivaniDolazak,
            CijenaPoMjestu = voznja.CijenaPoMjestu,
            UkupnoMjesta = voznja.UkupnoMjesta,
            SlobodnaMjesta = voznja.SlobodnaMjesta,
            Opis = voznja.Opis,
            Status = voznja.Status
        };

        PopulateFormOptions(model, isAdmin, userId.Value);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, VoznjaFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = _db.Voznje.FirstOrDefault(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        if (!isAdmin)
        {
            model.VozacId = voznja.VozacId;
            model.Status = voznja.Status;
        }

        if (!ModelState.IsValid)
        {
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        if (model.PolazniGradId == model.OdredisniGradId)
        {
            ModelState.AddModelError(nameof(model.OdredisniGradId), "Polazni i odredisni grad moraju biti razliciti.");
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        if (model.OcekivaniDolazak <= model.Polazak)
        {
            ModelState.AddModelError(nameof(model.OcekivaniDolazak), "Ocekivani dolazak mora biti nakon polaska.");
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        if (model.SlobodnaMjesta > model.UkupnoMjesta)
        {
            ModelState.AddModelError(nameof(model.SlobodnaMjesta), "Slobodna mjesta ne mogu biti veca od ukupnog broja mjesta.");
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        voznja.VozacId = model.VozacId;
        voznja.PolazniGradId = model.PolazniGradId;
        voznja.OdredisniGradId = model.OdredisniGradId;
        voznja.Polazak = model.Polazak;
        voznja.OcekivaniDolazak = model.OcekivaniDolazak;
        voznja.CijenaPoMjestu = model.CijenaPoMjestu;
        voznja.UkupnoMjesta = model.UkupnoMjesta;
        voznja.SlobodnaMjesta = model.SlobodnaMjesta;
        voznja.Opis = model.Opis;
        if (isAdmin)
        {
            voznja.Status = model.Status;
        }

        _db.SaveChanges();

        return RedirectToAction(nameof(Details), new { id = voznja.Id });
    }

    public IActionResult Delete(int id)
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
            .FirstOrDefault(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        return View(voznja);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = await _db.Voznje
            .Include(v => v.Rezervacije)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        if (voznja.Rezervacije.Count > 0)
        {
            ModelState.AddModelError(string.Empty, "Voznja ima rezervacije i ne moze se obrisati.");
            return View("Delete", voznja);
        }

        _db.Voznje.Remove(voznja);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult SearchDrivers(string q)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var query = _db.Korisnici
            .AsNoTracking()
            .Where(k => k.Tip == TipKorisnika.Vozac || k.Tip == TipKorisnika.VozacIPutnik);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(k =>
                EF.Functions.Like(k.Ime, $"%{normalized}%") ||
                EF.Functions.Like(k.Prezime, $"%{normalized}%") ||
                EF.Functions.Like(k.Ime + " " + k.Prezime, $"%{normalized}%") ||
                EF.Functions.Like(k.Email, $"%{normalized}%"));
        }

        var results = query
            .OrderBy(k => k.Prezime)
            .ThenBy(k => k.Ime)
            .Take(8)
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
    public IActionResult SearchCities(string q)
    {
        var query = _db.Gradovi.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q.Trim();
            query = query.Where(g =>
                EF.Functions.Like(g.Naziv, $"%{normalized}%") ||
                EF.Functions.Like(g.Drzava, $"%{normalized}%") ||
                EF.Functions.Like(g.PostanskiBroj, $"%{normalized}%"));
        }

        var results = query
            .OrderBy(g => g.Naziv)
            .Take(8)
            .Select(g => new
            {
                id = g.Id.ToString(),
                text = g.Naziv,
                subtext = $"{g.Drzava}, {g.PostanskiBroj}"
            })
            .ToList();

        return Json(results);
    }

    private VoznjaFormViewModel BuildFormViewModel(bool isAdmin, int currentUserId)
    {
        var model = new VoznjaFormViewModel
        {
            VozacId = currentUserId,
            Polazak = DateTime.Now.AddDays(1).Date.AddHours(8),
            OcekivaniDolazak = DateTime.Now.AddDays(1).Date.AddHours(12),
            Status = StatusVoznje.Planirana,
            UkupnoMjesta = 3,
            SlobodnaMjesta = 3
        };

        PopulateFormOptions(model, isAdmin, currentUserId);
        return model;
    }

    private void PopulateFormOptions(VoznjaFormViewModel model, bool isAdmin, int currentUserId)
    {
        model.CanSelectDriver = isAdmin;
        model.Vozaci = _db.Korisnici
            .AsNoTracking()
            .Where(k => isAdmin
                ? k.Tip == TipKorisnika.Vozac || k.Tip == TipKorisnika.VozacIPutnik
                : k.Id == currentUserId)
            .OrderBy(k => k.Prezime)
            .ThenBy(k => k.Ime)
            .Select(k => new SelectListItem
            {
                Value = k.Id.ToString(),
                Text = $"{k.Ime} {k.Prezime}".Trim()
            })
            .ToList();

        model.Gradovi = _db.Gradovi
            .AsNoTracking()
            .OrderBy(g => g.Naziv)
            .Select(g => new SelectListItem
            {
                Value = g.Id.ToString(),
                Text = g.Naziv
            })
            .ToList();
    }

    private bool CurrentUserCanDrive(int userId)
    {
        var user = _db.Korisnici.AsNoTracking().FirstOrDefault(k => k.Id == userId);
        return user is not null &&
               user.Tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik &&
               user.KycPodnesen;
    }
}
