using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Lab3;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu voznji i detaljan prikaz jedne voznje.
/// </summary>
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
        return View(_repository.GetVoznje());
    }

    public IActionResult Active()
    {
        var voznje = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Where(v => v.Status == StatusVoznje.Planirana)
            .OrderBy(v => v.Polazak)
            .ToList();

        return View(voznje);
    }

    public IActionResult Details(int id)
    {
        var voznja = _repository.GetVoznjaById(id);
        if (voznja is null)
        {
            return NotFound();
        }

        return View(voznja);
    }

    public IActionResult Create()
    {
        var model = BuildFormViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(VoznjaFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateFormOptions(model);
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

        return RedirectToAction(nameof(Details), new { id = voznja.Id });
    }

    public IActionResult Edit(int id)
    {
        var voznja = _db.Voznje.AsNoTracking().FirstOrDefault(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
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

        PopulateFormOptions(model);
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

        if (!ModelState.IsValid)
        {
            PopulateFormOptions(model);
            return View(model);
        }

        var voznja = _db.Voznje.FirstOrDefault(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
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
        voznja.Status = model.Status;

        _db.SaveChanges();

        return RedirectToAction(nameof(Details), new { id = voznja.Id });
    }

    private VoznjaFormViewModel BuildFormViewModel()
    {
        var model = new VoznjaFormViewModel
        {
            Polazak = DateTime.Now.AddDays(1).Date.AddHours(8),
            OcekivaniDolazak = DateTime.Now.AddDays(1).Date.AddHours(12),
            Status = StatusVoznje.Planirana,
            UkupnoMjesta = 3,
            SlobodnaMjesta = 3
        };

        PopulateFormOptions(model);
        return model;
    }

    private void PopulateFormOptions(VoznjaFormViewModel model)
    {
        model.Vozaci = _db.Korisnici
            .AsNoTracking()
            .Where(k => k.Tip == TipKorisnika.Vozac)
            .OrderBy(k => k.Prezime)
            .ThenBy(k => k.Ime)
            .Select(k => new SelectListItem
            {
                Value = k.Id.ToString(),
                Text = $"{k.Ime} {k.Prezime}"
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
}
