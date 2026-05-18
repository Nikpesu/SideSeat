using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Lab3;
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

    public IActionResult Index()
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

        return View(query.ToList());
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
}
