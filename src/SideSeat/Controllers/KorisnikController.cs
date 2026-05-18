using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Auth;
using SideSeat.Repositories;
using SideSeat.Security;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu korisnika i detalje odabranog korisnika.
/// </summary>
[Authorize]
public class KorisnikController : Controller
{
    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;

    public KorisnikController(SideSeatEfRepository repository, SideSeatDbContext db)
    {
        _repository = repository;
        _db = db;
    }

    public IActionResult Index()
    {
        if (!User.IsInRole("Admin"))
        {
            var userId = User.GetKorisnikId();
            if (userId is null)
            {
                return Challenge();
            }

            return RedirectToAction(nameof(Details), new { id = userId.Value });
        }

        return View(_repository.GetKorisnici());
    }

    public IActionResult Details(int id)
    {
        var userId = User.GetKorisnikId();
        if (!User.IsInRole("Admin") && userId != id)
        {
            return Forbid();
        }

        var korisnik = _db.Korisnici
            .AsNoTracking()
            .Include(k => k.Vozilo)
            .Include(k => k.KreiraneVoznje)
            .Include(k => k.Rezervacije)
            .FirstOrDefault(k => k.Id == id);
        if (korisnik is null)
        {
            return NotFound();
        }

        return View(korisnik);
    }

    public IActionResult Settings()
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var korisnik = _db.Korisnici.AsNoTracking().FirstOrDefault(k => k.Id == userId.Value);
        if (korisnik is null)
        {
            return NotFound();
        }

        var model = new UserSettingsViewModel
        {
            Address = korisnik.Adresa,
            IsDriver = korisnik.Tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik,
            IsRider = korisnik.Tip is TipKorisnika.Putnik or TipKorisnika.VozacIPutnik,
            KycPodnesen = korisnik.KycPodnesen
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(UserSettingsViewModel model)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!model.IsDriver && !model.IsRider)
        {
            ModelState.AddModelError(string.Empty, "Morate odabrati barem jednu aktivnu ulogu.");
            return View(model);
        }

        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == userId.Value);
        if (korisnik is null)
        {
            return NotFound();
        }

        var hadDriverRole = korisnik.Tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik;
        korisnik.Adresa = model.Address.Trim();

        if (model.IsDriver && !hadDriverRole && !korisnik.KycPodnesen)
        {
            await _db.SaveChangesAsync();
            TempData["KycRequired"] = "Za aktivaciju vozacke uloge prvo ispunite KYC obrazac.";
            return RedirectToAction(nameof(Kyc));
        }

        korisnik.Tip = ResolveTip(korisnik.Tip, model.IsDriver, model.IsRider);
        await _db.SaveChangesAsync();

        TempData["SettingsSaved"] = "Postavke su spremljene.";
        return RedirectToAction(nameof(Settings));
    }

    public IActionResult Kyc()
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var korisnik = _db.Korisnici.AsNoTracking().FirstOrDefault(k => k.Id == userId.Value);
        if (korisnik is null)
        {
            return NotFound();
        }

        var model = new DriverKycViewModel
        {
            Oib = korisnik.KycOib ?? string.Empty,
            BrojOsobne = korisnik.KycBrojOsobne ?? string.Empty,
            BrojVozacke = korisnik.KycBrojVozacke ?? string.Empty,
            DatumRodenja = korisnik.KycDatumRodenja ?? DateTime.UtcNow.AddYears(-20).Date
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Kyc(DriverKycViewModel model)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == userId.Value);
        if (korisnik is null)
        {
            return NotFound();
        }

        korisnik.KycPodnesen = true;
        korisnik.KycOib = model.Oib.Trim();
        korisnik.KycBrojOsobne = model.BrojOsobne.Trim();
        korisnik.KycBrojVozacke = model.BrojVozacke.Trim();
        korisnik.KycDatumRodenja = model.DatumRodenja.Date;

        if (korisnik.Tip == TipKorisnika.Putnik)
        {
            korisnik.Tip = TipKorisnika.VozacIPutnik;
        }
        else if (korisnik.Tip != TipKorisnika.Admin)
        {
            korisnik.Tip = TipKorisnika.Vozac;
        }

        await _db.SaveChangesAsync();

        TempData["SettingsSaved"] = "KYC podaci su spremljeni i vozacka uloga je aktivirana.";
        return RedirectToAction(nameof(Settings));
    }

    private static TipKorisnika ResolveTip(TipKorisnika existingTip, bool isDriver, bool isRider)
    {
        if (existingTip == TipKorisnika.Admin)
        {
            return TipKorisnika.Admin;
        }

        if (isDriver && isRider)
        {
            return TipKorisnika.VozacIPutnik;
        }

        if (isDriver)
        {
            return TipKorisnika.Vozac;
        }

        return TipKorisnika.Putnik;
    }
}
