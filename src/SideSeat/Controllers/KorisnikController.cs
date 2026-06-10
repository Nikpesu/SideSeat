using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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
    private const long MaxProfileImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedProfileImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly Regex CardNumberPattern = new(@"^\d{4} \d{4} \d{4} \d{4}$", RegexOptions.Compiled);
    private static readonly Regex CardExpiryPattern = new(@"^\d{2}/\d{2}$", RegexOptions.Compiled);
    private static readonly Regex CardCvvPattern = new(@"^\d{3,4}$", RegexOptions.Compiled);

    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly INotificationService _notifications;

    public KorisnikController(
        SideSeatEfRepository repository,
        SideSeatDbContext db,
        IPasswordHashingService passwordHashingService,
        IWebHostEnvironment webHostEnvironment,
        INotificationService notifications)
    {
        _repository = repository;
        _db = db;
        _passwordHashingService = passwordHashingService;
        _webHostEnvironment = webHostEnvironment;
        _notifications = notifications;
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

        var canViewFullDetails = User.IsInRole("Admin") || userId.Value == id;

        var korisnikQuery = _db.Korisnici.AsNoTracking().AsQueryable();
        if (canViewFullDetails)
        {
            korisnikQuery = korisnikQuery
                .Include(k => k.Vozilo)
                .Include(k => k.KreiraneVoznje)
                .Include(k => k.Rezervacije);
        }

        var korisnik = korisnikQuery.FirstOrDefault(k => k.Id == id);
        if (korisnik is null)
        {
            return NotFound();
        }

        if (!canViewFullDetails)
        {
            return View(new KorisnikProfileViewModel
            {
                User = korisnik,
                CanViewFullDetails = false
            });
        }

        var ocjene = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.AdminFeedbackAutor)
            .Include(o => o.Slike)
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
                Kreirano = o.Kreirano,
                Uredeno = o.Uredeno,
                AdminFeedback = o.AdminFeedback,
                AdminFeedbackAt = o.AdminFeedbackAt,
                AdminFeedbackAuthor = o.AdminFeedbackAutor is null
                    ? null
                    : $"{o.AdminFeedbackAutor.Ime} {o.AdminFeedbackAutor.Prezime}".Trim(),
                Slike = o.Slike
                    .OrderBy(s => s.CreatedAt)
                    .Select(s => new SideSeat.Models.Ocjena.OcjenaSlikaViewModel
                    {
                        Id = s.Id,
                        OcjenaVoznjeId = s.OcjenaVoznjeId,
                        FileName = s.FileName,
                        FilePath = s.FilePath
                    })
                    .ToList()
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
                Kreirano = o.Kreirano,
                Uredeno = o.Uredeno,
                AdminFeedback = o.AdminFeedback,
                AdminFeedbackAt = o.AdminFeedbackAt,
                AdminFeedbackAuthor = o.AdminFeedbackAutor is null
                    ? null
                    : $"{o.AdminFeedbackAutor.Ime} {o.AdminFeedbackAutor.Prezime}".Trim(),
                Slike = o.Slike
                    .OrderBy(s => s.CreatedAt)
                    .Select(s => new SideSeat.Models.Ocjena.OcjenaSlikaViewModel
                    {
                        Id = s.Id,
                        OcjenaVoznjeId = s.OcjenaVoznjeId,
                        FileName = s.FileName,
                        FilePath = s.FilePath
                    })
                    .ToList()
            })
            .ToList();

        var prosjek = primljene.Count == 0 ? 0 : primljene.Average(o => o.BrojZvjezdica);

        return View(new KorisnikProfileViewModel
        {
            User = korisnik,
            CanViewFullDetails = true,
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
            CanDisableRider = korisnik.Tip is not (TipKorisnika.Putnik or TipKorisnika.VozacIPutnik),
            CurrentProfileImagePath = korisnik.ProfilnaSlikaPath
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
        model.CurrentProfileImagePath = korisnik.ProfilnaSlikaPath;
        ValidateProfileImage(model.ProfileImage);

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
        if (model.ProfileImage is { Length: > 0 })
        {
            korisnik.ProfilnaSlikaPath = await SaveProfileImageAsync(
                korisnik.Id,
                model.ProfileImage,
                korisnik.ProfilnaSlikaPath);
        }
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
        if (akcija != "isplata")
        {
            ModelState.AddModelError(nameof(model.Akcija), "Uplata se izvršava kroz mock checkout stranicu.");
            return View(BuildSaldoModel(korisnik.Id, korisnik.Saldo, model));
        }

        var saldoPrije = korisnik.Saldo;
        var rezerviranaSredstva = GetCommittedReservationAmount(korisnik.Id);
        var raspolozivoZaIsplatu = korisnik.Saldo - rezerviranaSredstva;
        if (raspolozivoZaIsplatu < model.Iznos)
        {
            ModelState.AddModelError(
                nameof(model.Iznos),
                $"Za zakazane rezervacije rezervirano je {rezerviranaSredstva:0.00} EUR. Raspoloživo za isplatu: {Math.Max(0, raspolozivoZaIsplatu):0.00} EUR.");
            return View(BuildSaldoModel(korisnik.Id, korisnik.Saldo, model));
        }

        korisnik.Saldo -= model.Iznos;
        TempData["SaldoSaved"] = $"Isplata uspješna: -{model.Iznos:0.00} EUR.";

        _db.SaldoTransakcije.Add(new SaldoTransakcija
        {
            KorisnikId = korisnik.Id,
            Iznos = model.Iznos,
            Tip = akcija,
            SaldoPrije = saldoPrije,
            SaldoPoslije = korisnik.Saldo,
            Vrijeme = DateTime.UtcNow
        });
        _notifications.Add(
            korisnik.Id,
            "Isplata sa salda",
            $"Isplaćeno je {model.Iznos:0.00} EUR. Novi saldo: {korisnik.Saldo:0.00} EUR.",
            "Saldo",
            "/Korisnik/Saldo");

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Saldo));
    }

    [HttpGet]
    public IActionResult Uplata(decimal? amount, string? returnUrl)
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

        var billingAddress = ParseBillingAddress(korisnik.SpremljenaAdresaPlacanja ?? korisnik.Adresa);
        return View(new MockTopUpViewModel
        {
            Iznos = amount.GetValueOrDefault() > 0 ? amount.GetValueOrDefault() : 20m,
            CardholderName = korisnik.SpremljenaKarticaIme,
            CardExpiry = korisnik.SpremljenaKarticaVrijediDo,
            BillingStreet = billingAddress.Street,
            BillingHouseNumber = billingAddress.HouseNumber,
            BillingPostalCode = billingAddress.PostalCode,
            BillingCountry = billingAddress.Country,
            SavedCardDisplay = string.IsNullOrWhiteSpace(korisnik.SpremljenaKarticaZadnjeCetiri)
                ? null
                : $"•••• {korisnik.SpremljenaKarticaZadnjeCetiri}",
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action(nameof(Saldo))
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Uplata(MockTopUpViewModel model)
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

        var nacinPlacanja = model.NacinPlacanja.Trim();
        var supportedMethods = new[] { "Kartica", "PayPal", "Revolut Pay" };
        if (!supportedMethods.Contains(nacinPlacanja, StringComparer.Ordinal))
        {
            ModelState.AddModelError(nameof(model.NacinPlacanja), "Odaberi podržani način plaćanja.");
        }

        if (nacinPlacanja == "Kartica")
        {
            if (string.IsNullOrWhiteSpace(model.CardholderName))
            {
                ModelState.AddModelError(nameof(model.CardholderName), "Unesi ime na kartici.");
            }

            var hasSavedCard = !string.IsNullOrWhiteSpace(korisnik.SpremljenaKarticaZadnjeCetiri);
            var enteredNewCard = !string.IsNullOrWhiteSpace(model.CardNumber);
            if ((!hasSavedCard || enteredNewCard) && !CardNumberPattern.IsMatch(model.CardNumber?.Trim() ?? string.Empty))
            {
                ModelState.AddModelError(nameof(model.CardNumber), "Broj kartice mora biti u formatu 4444 4444 4444 4444.");
            }

            if (!CardExpiryPattern.IsMatch(model.CardExpiry?.Trim() ?? string.Empty))
            {
                ModelState.AddModelError(nameof(model.CardExpiry), "Datum isteka mora biti u formatu MM/GG.");
            }

            if (!CardCvvPattern.IsMatch(model.CardCvv?.Trim() ?? string.Empty))
            {
                ModelState.AddModelError(nameof(model.CardCvv), "CVV mora imati 3 ili 4 znamenke.");
            }
        }
        else if (nacinPlacanja is "PayPal" or "Revolut Pay")
        {
            if (string.IsNullOrWhiteSpace(model.ExternalAccountName))
            {
                ModelState.AddModelError(nameof(model.ExternalAccountName), "Unesi ime računa.");
            }

            if (!model.ExternalPaymentConfirmed)
            {
                ModelState.AddModelError(nameof(model.ExternalPaymentConfirmed), "Potvrdi da je mock transakcija završena.");
            }
        }

        if (string.IsNullOrWhiteSpace(model.BillingStreet))
        {
            ModelState.AddModelError(nameof(model.BillingStreet), "Unesi ulicu.");
        }

        if (string.IsNullOrWhiteSpace(model.BillingHouseNumber))
        {
            ModelState.AddModelError(nameof(model.BillingHouseNumber), "Unesi kućni broj.");
        }

        if (string.IsNullOrWhiteSpace(model.BillingPostalCode))
        {
            ModelState.AddModelError(nameof(model.BillingPostalCode), "Unesi poštanski broj.");
        }

        if (string.IsNullOrWhiteSpace(model.BillingCountry))
        {
            ModelState.AddModelError(nameof(model.BillingCountry), "Unesi državu.");
        }

        if (!ModelState.IsValid)
        {
            model.SavedCardDisplay = string.IsNullOrWhiteSpace(korisnik.SpremljenaKarticaZadnjeCetiri)
                ? null
                : $"•••• {korisnik.SpremljenaKarticaZadnjeCetiri}";
            return View(model);
        }

        if (nacinPlacanja == "Kartica" && model.SaveCard)
        {
            var cardDigits = new string((model.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            if (cardDigits.Length >= 4)
            {
                korisnik.SpremljenaKarticaZadnjeCetiri = cardDigits[^4..];
            }

            korisnik.SpremljenaKarticaIme = model.CardholderName?.Trim();
            korisnik.SpremljenaKarticaVrijediDo = model.CardExpiry?.Trim();
        }

        if (model.SaveBillingAddress)
        {
            korisnik.SpremljenaAdresaPlacanja = FormatBillingAddress(model);
        }

        var saldoPrije = korisnik.Saldo;
        korisnik.Saldo += model.Iznos;
        var komentar = BuildTopUpComment(nacinPlacanja, model, korisnik);
        _db.SaldoTransakcije.Add(new SaldoTransakcija
        {
            KorisnikId = korisnik.Id,
            Iznos = model.Iznos,
            Tip = $"uplata-{NormalizePaymentMethod(nacinPlacanja)}",
            Komentar = komentar,
            SaldoPrije = saldoPrije,
            SaldoPoslije = korisnik.Saldo,
            Vrijeme = DateTime.UtcNow
        });
        _notifications.Add(
            korisnik.Id,
            "Mock uplata na saldo",
            $"Saldo je povećan za {model.Iznos:0.00} EUR. {komentar}. Vanjska naplata nije izvršena.",
            "Saldo",
            "/Korisnik/Saldo");

        await _db.SaveChangesAsync();

        TempData["SaldoSaved"] =
            $"Mock uplata uspješna: +{model.Iznos:0.00} EUR. Nije izvršena stvarna naplata.";

        var returnUrl = Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : Url.Action(nameof(Saldo));
        return Redirect(returnUrl!);
    }

    private static string NormalizePaymentMethod(string value) =>
        value.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

    private static string BuildTopUpComment(string nacinPlacanja, MockTopUpViewModel model, Korisnik korisnik)
    {
        if (nacinPlacanja == "Kartica")
        {
            var digits = new string((model.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            var lastFour = digits.Length >= 4
                ? digits[^4..]
                : korisnik.SpremljenaKarticaZadnjeCetiri ?? "----";
            return $"Uplaćeno sa *{lastFour} kartice";
        }

        var provider = nacinPlacanja == "PayPal" ? "PayPal" : "Revolut";
        return $"Uplaćeno sa {model.ExternalAccountName?.Trim()} {provider} računa";
    }

    private static string FormatBillingAddress(MockTopUpViewModel model) =>
        $"{model.BillingStreet?.Trim()} {model.BillingHouseNumber?.Trim()}, {model.BillingPostalCode?.Trim()}, {model.BillingCountry?.Trim()}";

    private static (string Street, string HouseNumber, string PostalCode, string Country) ParseBillingAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (string.Empty, string.Empty, string.Empty, "Hrvatska");
        }

        var sections = value.Split(',', StringSplitOptions.TrimEntries);
        if (sections.Length < 3)
        {
            return (value.Trim(), string.Empty, string.Empty, "Hrvatska");
        }

        var streetAndNumber = sections[0];
        var lastSpace = streetAndNumber.LastIndexOf(' ');
        return lastSpace > 0
            ? (streetAndNumber[..lastSpace], streetAndNumber[(lastSpace + 1)..], sections[1], sections[2])
            : (streetAndNumber, string.Empty, sections[1], sections[2]);
    }

    private decimal GetCommittedReservationAmount(int korisnikId) =>
        _db.Rezervacije
            .AsNoTracking()
            .Where(r =>
                r.PutnikId == korisnikId &&
                (r.Status == StatusRezervacije.UProcesuPotvrde ||
                 r.Status == StatusRezervacije.Potvrdena))
            .Sum(r => (decimal?)r.CijenaUkupno) ?? 0m;

    private SaldoViewModel BuildSaldoModel(int korisnikId, decimal saldo, SaldoViewModel? current = null)
    {
        var transakcijeRaw = _db.SaldoTransakcije
            .AsNoTracking()
            .Where(t => t.KorisnikId == korisnikId)
            .OrderByDescending(t => t.Vrijeme)
            .Take(100)
            .ToList();

        var rideReservationIds = transakcijeRaw
            .Select(t => TryExtractRideReservationId(t.Tip))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var reservationRideMap = _db.Rezervacije
            .AsNoTracking()
            .Where(r => rideReservationIds.Contains(r.Id))
            .ToDictionary(r => r.Id, r => r.VoznjaId);

        var transakcije = transakcijeRaw
            .Select(t =>
            {
                var reservationId = TryExtractRideReservationId(t.Tip);
                var voznjaId = reservationId.HasValue && reservationRideMap.TryGetValue(reservationId.Value, out var mappedRideId)
                    ? mappedRideId
                    : (int?)null;

                return new SaldoTransakcijaRowViewModel
                {
                    Vrijeme = t.Vrijeme,
                    Tip = t.Tip,
                    Iznos = t.Iznos,
                    SaldoPrije = t.SaldoPrije,
                    SaldoPoslije = t.SaldoPoslije,
                    VoznjaId = voznjaId,
                    Komentar = string.IsNullOrWhiteSpace(t.Komentar)
                        ? BuildTransactionComment(t.Tip, voznjaId)
                        : t.Komentar,
                    ServisPlacanja = GetPaymentService(t.Tip)
                };
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

    private static int? TryExtractRideReservationId(string tip)
    {
        if (tip.StartsWith("priljev-voznja:", StringComparison.OrdinalIgnoreCase))
        {
            var value = tip["priljev-voznja:".Length..];
            return int.TryParse(value, out var reservationId) ? reservationId : null;
        }

        if (tip.StartsWith("naplata-rezervacije:", StringComparison.OrdinalIgnoreCase))
        {
            var value = tip["naplata-rezervacije:".Length..];
            return int.TryParse(value, out var reservationId) ? reservationId : null;
        }

        return null;
    }

    private static string BuildTransactionComment(string tip, int? voznjaId)
    {
        if (tip.Equals("uplata", StringComparison.OrdinalIgnoreCase))
        {
            return "Rucna uplata";
        }

        if (tip.Equals("isplata", StringComparison.OrdinalIgnoreCase))
        {
            return "Rucna isplata";
        }

        if (tip.StartsWith("priljev-voznja:", StringComparison.OrdinalIgnoreCase) && voznjaId.HasValue)
        {
            return $"Voznja #{voznjaId.Value}";
        }

        if (tip.StartsWith("naplata-rezervacije:", StringComparison.OrdinalIgnoreCase))
        {
            return "Naplata nakon voznje";
        }

        return "-";
    }

    private static string? GetPaymentService(string tip)
    {
        if (tip.Equals("uplata-kartica", StringComparison.OrdinalIgnoreCase))
        {
            return "Kartica";
        }

        if (tip.Equals("uplata-paypal", StringComparison.OrdinalIgnoreCase))
        {
            return "PayPal";
        }

        if (tip.Equals("uplata-revolut-pay", StringComparison.OrdinalIgnoreCase))
        {
            return "Revolut Pay";
        }

        return null;
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

    private void ValidateProfileImage(IFormFile? image)
    {
        if (image is null || image.Length == 0)
        {
            return;
        }

        var extension = Path.GetExtension(image.FileName);
        var isImage = !string.IsNullOrWhiteSpace(image.ContentType)
                      && image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        if (!isImage || !AllowedProfileImageExtensions.Contains(extension))
        {
            ModelState.AddModelError(nameof(UserSettingsViewModel.ProfileImage),
                "Dopuštene su samo slike: JPG, PNG, GIF ili WEBP.");
        }

        if (image.Length > MaxProfileImageBytes)
        {
            ModelState.AddModelError(nameof(UserSettingsViewModel.ProfileImage),
                "Profilna slika može imati najviše 5 MB.");
        }
    }

    private async Task<string> SaveProfileImageAsync(int korisnikId, IFormFile image, string? oldRelativePath)
    {
        var webRootPath = _webHostEnvironment.WebRootPath
                          ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        var uploadDirectory = Path.Combine(webRootPath, "uploads", "profili", korisnikId.ToString());
        Directory.CreateDirectory(uploadDirectory);

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var diskPath = Path.Combine(uploadDirectory, storedFileName);
        await using (var stream = new FileStream(diskPath, FileMode.CreateNew))
        {
            await image.CopyToAsync(stream);
        }

        DeleteOldProfileImage(korisnikId, oldRelativePath, webRootPath);
        return $"/uploads/profili/{korisnikId}/{storedFileName}";
    }

    private static void DeleteOldProfileImage(int korisnikId, string? relativePath, string webRootPath)
    {
        var expectedPrefix = $"/uploads/profili/{korisnikId}/";
        if (string.IsNullOrWhiteSpace(relativePath)
            || !relativePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var fileName = Path.GetFileName(relativePath);
        var diskPath = Path.Combine(webRootPath, "uploads", "profili", korisnikId.ToString(), fileName);
        if (System.IO.File.Exists(diskPath))
        {
            System.IO.File.Delete(diskPath);
        }
    }

    private static void ApplySettingsRoleLocks(UserSettingsViewModel model, Korisnik korisnik)
    {
        var hadDriverRole = korisnik.Tip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik;
        var hadRiderRole = korisnik.Tip is TipKorisnika.Putnik or TipKorisnika.VozacIPutnik;

        model.CanDisableDriver = !hadDriverRole;
        model.CanDisableRider = !hadRiderRole;
    }
}
