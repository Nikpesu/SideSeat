using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Lab3;
using SideSeat.Repositories;
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

    public IActionResult Index()
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        if (User.IsInRole("Admin"))
        {
            return View(_repository.GetVoznje());
        }

        var voznje = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Where(v => v.VozacId == userId.Value || v.Rezervacije.Any(r => r.PutnikId == userId.Value))
            .OrderBy(v => v.Polazak)
            .ToList();

        return View(voznje);
    }

    public IActionResult Active()
    {
        var voznje = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Where(v => v.Status == StatusVoznje.Planirana && v.SlobodnaMjesta > 0)
            .OrderBy(v => v.Polazak)
            .ToList();

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

        return View(voznja);
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
