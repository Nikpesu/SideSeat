using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Auth;
using SideSeat.Models.Balance;
using SideSeat.Models.ViewModels;
using SideSeat.Repositories;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu korisnika i detalje odabranog korisnika.
/// </summary>
[Authorize]
public class KorisnikController : Controller
{
    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;
    private readonly IPasswordHashingService _passwordHashingService;

    public KorisnikController(SideSeatEfRepository repository, SideSeatDbContext db, IPasswordHashingService passwordHashingService)
    {
        _repository = repository;
        _db = db;
        _passwordHashingService = passwordHashingService;
    }

    public IActionResult Index(string? search, int? pageSize)
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

        var korisnici = _repository.GetKorisnici();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            korisnici = korisnici.Where(korisnik =>
                korisnik.Ime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                korisnik.Prezime.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                korisnik.Email.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                korisnik.BrojMobitela.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                korisnik.Tip.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                korisnik.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Search = search;
        ViewBag.PageSize = pageSize;

        if (pageSize.HasValue)
        {
            var normalized = PageSizeOptions.Normalize(pageSize.Value);
            if (normalized > 0)
            {
                korisnici = korisnici.Take(normalized).ToList();
            }
        }

        return View(korisnici);
    }

    public IActionResult Details(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
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

        var ocjene = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Putnik)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .ThenInclude(v => v.Vozac)
            .ToList();

        var dane = ocjene
            .Where(o => o.AutorId == id)
            .Select(o => new KorisnikOcjenaRow
            {
                RezervacijaId = o.RezervacijaId,
                Autor = $"{o.Autor.Ime} {o.Autor.Prezime}",
                Primatelj = o.Rezervacija.Voznja.VozacId == o.AutorId
                    ? $"{o.Rezervacija.Putnik.Ime} {o.Rezervacija.Putnik.Prezime}"
                    : $"{o.Rezervacija.Voznja.Vozac.Ime} {o.Rezervacija.Voznja.Vozac.Prezime}",
                BrojZvjezdica = o.BrojZvjezdica,
                Komentar = o.Komentar,
                Kreirano = o.Kreirano
            })
            .ToList();

        var primljene = ocjene
            .Where(o =>
            {
                var targetId = o.Rezervacija.Voznja.VozacId == o.AutorId
                    ? o.Rezervacija.PutnikId
                    : o.Rezervacija.Voznja.VozacId;
                return targetId == id;
            })
            .Select(o => new KorisnikOcjenaRow
            {
                RezervacijaId = o.RezervacijaId,
                Autor = $"{o.Autor.Ime} {o.Autor.Prezime}",
                Primatelj = o.Rezervacija.Voznja.VozacId == o.AutorId
                    ? $"{o.Rezervacija.Putnik.Ime} {o.Rezervacija.Putnik.Prezime}"
                    : $"{o.Rezervacija.Voznja.Vozac.Ime} {o.Rezervacija.Voznja.Vozac.Prezime}",
                BrojZvjezdica = o.BrojZvjezdica,
                Komentar = o.Komentar,
                Kreirano = o.Kreirano
            })
            .ToList();

        var prosjek = primljene.Count == 0 ? 0 : primljene.Average(o => o.BrojZvjezdica);

        return View(new KorisnikProfileViewModel
        {
            User = korisnik,
            ProsjecnaOcjena = prosjek,
            BrojPrimljenihOcjena = primljene.Count,
            PrimljeneOcjene = primljene,
            DaneOcjene = dane
        });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create()
    {
        return View(new KorisnikFormViewModel
        {
            DatumRegistracije = DateTime.UtcNow,
            Tip = TipKorisnika.Putnik,
            JeAktivan = true
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KorisnikFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Lozinka je obavezna.");
            return View(model);
        }

        if (model.VoziloId.HasValue)
        {
            var voziloExists = await _db.Vozila.AnyAsync(v => v.Id == model.VoziloId.Value);
            if (!voziloExists)
            {
                ModelState.AddModelError(nameof(model.VoziloId), "Odabrano vozilo ne postoji.");
                return View(model);
            }
        }

        var korisnik = new Korisnik
        {
            Ime = model.Ime.Trim(),
            Prezime = model.Prezime.Trim(),
            Email = model.Email.Trim(),
            Adresa = model.Adresa.Trim(),
            BrojMobitela = model.BrojMobitela.Trim(),
            DatumRegistracije = model.DatumRegistracije,
            Tip = model.Tip,
            JeAktivan = model.JeAktivan,
            KycPodnesen = model.KycPodnesen,
            KycOib = string.IsNullOrWhiteSpace(model.KycOib) ? null : model.KycOib.Trim(),
            KycBrojOsobne = string.IsNullOrWhiteSpace(model.KycBrojOsobne) ? null : model.KycBrojOsobne.Trim(),
            KycBrojVozacke = string.IsNullOrWhiteSpace(model.KycBrojVozacke) ? null : model.KycBrojVozacke.Trim(),
            KycDatumRodenja = model.KycDatumRodenja,
            VoziloId = model.VoziloId,
            LozinkaHash = _passwordHashingService.Hash(model.Password.Trim())
        };

        _db.Korisnici.Add(korisnik);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = korisnik.Id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Edit(int id)
    {
        var korisnik = _db.Korisnici
            .AsNoTracking()
            .Include(k => k.Vozilo)
            .FirstOrDefault(k => k.Id == id);
        if (korisnik is null)
        {
            return NotFound();
        }

        return View(new KorisnikFormViewModel
        {
            Id = korisnik.Id,
            Ime = korisnik.Ime,
            Prezime = korisnik.Prezime,
            Email = korisnik.Email,
            Adresa = korisnik.Adresa,
            BrojMobitela = korisnik.BrojMobitela,
            DatumRegistracije = korisnik.DatumRegistracije,
            Tip = korisnik.Tip,
            JeAktivan = korisnik.JeAktivan,
            KycPodnesen = korisnik.KycPodnesen,
            KycOib = korisnik.KycOib,
            KycBrojOsobne = korisnik.KycBrojOsobne,
            KycBrojVozacke = korisnik.KycBrojVozacke,
            KycDatumRodenja = korisnik.KycDatumRodenja,
            VoziloId = korisnik.VoziloId,
            VoziloNaziv = korisnik.Vozilo is null ? string.Empty : $"{korisnik.Vozilo.Marka} {korisnik.Vozilo.Model}"
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, KorisnikFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == id);
        if (korisnik is null)
        {
            return NotFound();
        }

        if (model.VoziloId.HasValue)
        {
            var voziloExists = await _db.Vozila.AnyAsync(v => v.Id == model.VoziloId.Value);
            if (!voziloExists)
            {
                ModelState.AddModelError(nameof(model.VoziloId), "Odabrano vozilo ne postoji.");
                return View(model);
            }
        }

        korisnik.Ime = model.Ime.Trim();
        korisnik.Prezime = model.Prezime.Trim();
        korisnik.Email = model.Email.Trim();
        korisnik.Adresa = model.Adresa.Trim();
        korisnik.BrojMobitela = model.BrojMobitela.Trim();
        korisnik.DatumRegistracije = model.DatumRegistracije;
        korisnik.Tip = model.Tip;
        korisnik.JeAktivan = model.JeAktivan;
        korisnik.KycPodnesen = model.KycPodnesen;
        korisnik.KycOib = string.IsNullOrWhiteSpace(model.KycOib) ? null : model.KycOib.Trim();
        korisnik.KycBrojOsobne = string.IsNullOrWhiteSpace(model.KycBrojOsobne) ? null : model.KycBrojOsobne.Trim();
        korisnik.KycBrojVozacke = string.IsNullOrWhiteSpace(model.KycBrojVozacke) ? null : model.KycBrojVozacke.Trim();
        korisnik.KycDatumRodenja = model.KycDatumRodenja;
        korisnik.VoziloId = model.VoziloId;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            korisnik.LozinkaHash = _passwordHashingService.Hash(model.Password.Trim());
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = korisnik.Id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var korisnik = _repository.GetKorisnikById(id);
        if (korisnik is null)
        {
            return NotFound();
        }

        return View(korisnik);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == id);
        if (korisnik is null)
        {
            return NotFound();
        }

        var hasTrips = await _db.Voznje.AnyAsync(v => v.VozacId == id);
        var hasReservations = await _db.Rezervacije.AnyAsync(r => r.PutnikId == id);
        if (hasTrips || hasReservations)
        {
            ModelState.AddModelError(string.Empty, "Korisnik ima povezane voznje ili rezervacije i ne moze se obrisati.");
            return View("Delete", korisnik);
        }

        _db.Korisnici.Remove(korisnik);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
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
            PhoneNumber = korisnik.BrojMobitela,
            IsDriver = korisnik.Tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik,
            IsRider = korisnik.Tip is TipKorisnika.Putnik or TipKorisnika.VozacIPutnik,
            KycPodnesen = korisnik.KycPodnesen,
            CanDisableDriver = korisnik.Tip is not (TipKorisnika.Vozac or TipKorisnika.VozacIPutnik),
            CanDisableRider = korisnik.Tip is not (TipKorisnika.Putnik or TipKorisnika.VozacIPutnik)
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

        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == userId.Value);
        if (korisnik is null)
        {
            return NotFound();
        }
        ApplySettingsRoleLocks(model, korisnik);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var hadDriverRole = korisnik.Tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik;
        var hadRiderRole = korisnik.Tip is TipKorisnika.Putnik or TipKorisnika.VozacIPutnik;
        korisnik.Adresa = model.Address.Trim();
        korisnik.BrojMobitela = model.PhoneNumber.Trim();

        // Jednom aktivirana uloga ne moze se ugasiti kroz postavke.
        if (hadDriverRole)
        {
            model.IsDriver = true;
        }
        if (hadRiderRole)
        {
            model.IsRider = true;
        }

        if (!model.IsDriver && !model.IsRider)
        {
            ModelState.AddModelError(string.Empty, "Morate odabrati barem jednu aktivnu ulogu.");
            return View(model);
        }

        if (model.IsDriver && !hadDriverRole && !korisnik.KycPodnesen)
        {
            await _db.SaveChangesAsync();
            TempData["KycRequired"] = "Za aktivaciju vozacke uloge prvo ispunite KYC obrazac.";
            return RedirectToAction(nameof(Kyc));
        }

        var wantsPasswordChange = !string.IsNullOrWhiteSpace(model.CurrentPassword)
                                  || !string.IsNullOrWhiteSpace(model.NewPassword)
                                  || !string.IsNullOrWhiteSpace(model.ConfirmNewPassword);
        if (wantsPasswordChange)
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                string.IsNullOrWhiteSpace(model.NewPassword) ||
                string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
            {
                ModelState.AddModelError(string.Empty, "Za promjenu lozinke potrebno je ispuniti sva tri polja.");
                return View(model);
            }

            if (!_passwordHashingService.Verify(model.CurrentPassword, korisnik.LozinkaHash))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Trenutna lozinka nije ispravna.");
                return View(model);
            }

            if (!string.Equals(model.NewPassword, model.ConfirmNewPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.ConfirmNewPassword), "Potvrda lozinke se ne podudara.");
                return View(model);
            }

            korisnik.LozinkaHash = _passwordHashingService.Hash(model.NewPassword);
        }

        korisnik.Tip = ResolveTip(korisnik.Tip, model.IsDriver, model.IsRider);
        await _db.SaveChangesAsync();

        TempData["SettingsSaved"] = wantsPasswordChange
            ? "Postavke su spremljene i lozinka je promijenjena."
            : "Postavke su spremljene.";
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

    [HttpGet]
    public IActionResult Saldo()
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

        return View(BuildSaldoModel(korisnik.Id, korisnik.Saldo));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Saldo(SaldoViewModel model)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var korisnik = await _db.Korisnici.FirstOrDefaultAsync(k => k.Id == userId.Value);
        if (korisnik is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(BuildSaldoModel(korisnik.Id, korisnik.Saldo, model));
        }

        var akcija = (model.Akcija ?? string.Empty).Trim().ToLowerInvariant();
        if (akcija is not ("uplata" or "isplata"))
        {
            ModelState.AddModelError(nameof(model.Akcija), "Neispravna akcija.");
            return View(BuildSaldoModel(korisnik.Id, korisnik.Saldo, model));
        }

        var saldoPrije = korisnik.Saldo;
        if (akcija == "uplata")
        {
            korisnik.Saldo += model.Iznos;
            TempData["SaldoSaved"] = $"Uplata uspjesna: +{model.Iznos:0.00} EUR.";
        }
        else
        {
            if (korisnik.Saldo < model.Iznos)
            {
                ModelState.AddModelError(nameof(model.Iznos), "Nedovoljan saldo za isplatu.");
                return View(BuildSaldoModel(korisnik.Id, korisnik.Saldo, model));
            }

            korisnik.Saldo -= model.Iznos;
            TempData["SaldoSaved"] = $"Isplata uspjesna: -{model.Iznos:0.00} EUR.";
        }

        _db.SaldoTransakcije.Add(new SaldoTransakcija
        {
            KorisnikId = korisnik.Id,
            Iznos = model.Iznos,
            Tip = akcija,
            SaldoPrije = saldoPrije,
            SaldoPoslije = korisnik.Saldo,
            Vrijeme = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Saldo));
    }

    private SaldoViewModel BuildSaldoModel(int korisnikId, decimal saldo, SaldoViewModel? current = null)
    {
        var transakcije = _db.SaldoTransakcije
            .AsNoTracking()
            .Where(t => t.KorisnikId == korisnikId)
            .OrderByDescending(t => t.Vrijeme)
            .Take(100)
            .Select(t => new SaldoTransakcijaRowViewModel
            {
                Vrijeme = t.Vrijeme,
                Tip = t.Tip,
                Iznos = t.Iznos,
                SaldoPrije = t.SaldoPrije,
                SaldoPoslije = t.SaldoPoslije
            })
            .ToList();

        return new SaldoViewModel
        {
            TrenutniSaldo = saldo,
            Iznos = current?.Iznos ?? 0,
            Akcija = current?.Akcija ?? "uplata",
            Transakcije = transakcije
        };
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

    private static void ApplySettingsRoleLocks(UserSettingsViewModel model, Korisnik korisnik)
    {
        var hadDriverRole = korisnik.Tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik;
        var hadRiderRole = korisnik.Tip is TipKorisnika.Putnik or TipKorisnika.VozacIPutnik;

        model.CanDisableDriver = !hadDriverRole;
        model.CanDisableRider = !hadRiderRole;
    }
}
