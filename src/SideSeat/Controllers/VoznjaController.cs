using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Forms;
using SideSeat.Repositories;
using SideSeat.Models.ViewModels;
using SideSeat.Models.Rides;
using SideSeat.Models.Commands;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.Controllers;

[Authorize]
public class VoznjaController : Controller
{
    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ISideSeatCommandService _commands;

    public VoznjaController(
        SideSeatEfRepository repository,
        SideSeatDbContext db,
        INotificationService notifications,
        ISideSeatCommandService commands)
    {
        _repository = repository;
        _db = db;
        _notifications = notifications;
        _commands = commands;
    }

    public IActionResult Index(string? view, string? status, string? search, DateTime? date, int? pageSize)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole("Admin");
        var korisnikTip = _db.Korisnici
            .AsNoTracking()
            .Where(k => k.Id == userId.Value)
            .Select(k => (TipKorisnika?)k.Tip)
            .FirstOrDefault();
        var canViewDriving = isAdmin
                             || User.IsInRole("Driver")
                             || korisnikTip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik;
        var selectedView = NormalizeRideView(view, isAdmin);
        var selectedStatus = NormalizeStatusFilter(status);
        if (!isAdmin && selectedView == "all")
        {
            return Forbid();
        }

        var query = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Include(v => v.Rezervacije)
            .AsQueryable();

        query = selectedView switch
        {
            "available" => query.Where(v =>
                v.Status == StatusVoznje.Planirana &&
                v.SlobodnaMjesta > 0 &&
                v.VozacId != userId.Value),
            "driving" => query.Where(v => v.VozacId == userId.Value),
            "ridden" => query.Where(v => v.Rezervacije.Any(r => r.PutnikId == userId.Value)),
            _ => query
        };

        var statusCounts = query
            .Select(v => v.Status)
            .ToList();

        query = selectedStatus switch
        {
            "planned" => query.Where(v => v.Status == StatusVoznje.Planirana),
            "completed" => query.Where(v => v.Status == StatusVoznje.Zavrsena),
            "cancelled" => query.Where(v => v.Status == StatusVoznje.Otkazana),
            _ => query
        };

        var voznje = query
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
                voznja.Status.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                voznja.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (date.HasValue)
        {
            var selectedDate = date.Value.Date;
            voznje = voznje.Where(v => v.Polazak.Date == selectedDate).ToList();
        }

        ViewBag.RideView = selectedView;
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

        var display = GetRideViewDisplay(selectedView);
        return View(new VoznjaListViewModel
        {
            Voznje = voznje,
            SelectedView = selectedView,
            Title = display.Title,
            Description = display.Description,
            EmptyMessage = display.EmptyMessage,
            IsAdmin = isAdmin,
            CanViewDriving = canViewDriving,
            SelectedStatus = selectedStatus,
            AllCount = statusCounts.Count,
            PlannedCount = statusCounts.Count(value => value == StatusVoznje.Planirana),
            CompletedCount = statusCounts.Count(value => value == StatusVoznje.Zavrsena),
            CancelledCount = statusCounts.Count(value => value == StatusVoznje.Otkazana)
        });
    }

    public IActionResult Active(string? search, DateTime? date, int? pageSize)
    {
        return RedirectToAction(nameof(Index), new { view = "available", search, date, pageSize });
    }

    private static string NormalizeStatusFilter(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "planned" => "planned",
            "completed" => "completed",
            "cancelled" => "cancelled",
            _ => "all"
        };

    private static string NormalizeRideView(string? view, bool isAdmin)
    {
        var normalized = view?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "all" => "all",
            "available" or "active" => "available",
            "driving" or "mine" or "mine-active" => "driving",
            "ridden" => "ridden",
            _ => isAdmin ? "all" : "available"
        };
    }

    private static (string Title, string Description, string EmptyMessage) GetRideViewDisplay(string view) =>
        view switch
        {
            "available" => ("Dostupne vožnje", "Planirane vožnje s dostupnim mjestima koje možeš rezervirati.", "Trenutno nema dostupnih vožnji."),
            "driving" => ("Moje vožnje", "Vožnje na kojima si vozač.", "Nemaš kreiranih vožnji."),
            "ridden" => ("Moja voženja", "Vožnje na kojima sudjeluješ kao putnik.", "Nemaš rezerviranih vožnji."),
            _ => ("Sve voznje", "Administratorski pregled svih voznji i statusa.", "Nema unesenih voznji.")
        };

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
            .Include(o => o.AdminFeedbackAutor)
            .Include(o => o.Slike)
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
        }).ToList();

        var ocjeneVozaca = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.AdminFeedbackAutor)
            .Include(o => o.Slike)
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
                PutnikTelefon = r.Putnik.BrojMobitela,
                Status = r.Status,
                BrojMjesta = r.BrojMjesta,
                NacinPlacanja = r.NacinPlacanja,
                CijenaUkupno = r.CijenaUkupno,
                Napojnica = r.Napojnica,
                CheckInAtUtc = r.CheckInAtUtc,
                LastLatitude = r.LastLatitude,
                LastLongitude = r.LastLongitude,
                LastLocationAtUtc = r.LastLocationAtUtc,
                CashCollectedAtUtc = r.CashCollectedAtUtc,
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
            ProsjecnaOcjenaVozaca = ocjeneVozacaRows.Count == 0 ? 0 : ocjeneVozacaRows.Average(x => x.BrojZvjezdica),
            CashDue = putnici
                .Where(item => item.Status == StatusRezervacije.Potvrdena && item.NacinPlacanja == NacinPlacanja.Gotovina)
                .Sum(item => item.CijenaUkupno + item.Napojnica),
            SaldoDue = putnici
                .Where(item => item.Status == StatusRezervacije.Potvrdena && item.NacinPlacanja == NacinPlacanja.SideSeatSaldo)
                .Sum(item => item.CijenaUkupno + item.Napojnica),
            AllConfirmedPassengersReady = putnici.Any(item => item.Status == StatusRezervacije.Potvrdena) &&
                putnici
                    .Where(item => item.Status == StatusRezervacije.Potvrdena)
                    .All(item => item.CheckInAtUtc.HasValue)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Izvrsi(int id, CancellationToken cancellationToken)
    {
        var result = await _commands.ExecuteAsync(
            SideSeatActionTypes.FinishRide,
            new FinishRideCommand(id, CashCollected: true),
            User,
            "MVC",
            cancellationToken);
        TempData["RideExecuted"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var now = DateTime.Now;
        var from = now.AddHours(-4);
        var to = now.AddHours(8);
        var query = _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Include(v => v.Rezervacije)
            .ThenInclude(r => r.Putnik)
            .Where(v =>
                v.Status != StatusVoznje.Otkazana &&
                v.Status != StatusVoznje.Zavrsena &&
                v.Polazak >= from &&
                v.Polazak <= to);

        if (!User.IsInRole("Admin"))
        {
            query = query.Where(v => v.VozacId == userId.Value);
        }

        var rides = await query
            .OrderBy(v => v.Polazak)
            .ToListAsync(cancellationToken);
        var rideIds = rides.Select(v => v.Id).ToList();
        var messages = await _db.RideChatMessages
            .AsNoTracking()
            .Include(message => message.Sender)
            .Where(message => rideIds.Contains(message.VoznjaId))
            .OrderByDescending(message => message.CreatedAtUtc)
            .Take(150)
            .ToListAsync(cancellationToken);

        return View(new CurrentRideViewModel
        {
            Rides = rides.Select(ride =>
            {
                var putnici = BuildPassengerRows(ride);
                return new CurrentRideItemViewModel
                {
                    Voznja = ride,
                    Putnici = putnici,
                    Messages = messages
                        .Where(message => message.VoznjaId == ride.Id)
                        .OrderBy(message => message.CreatedAtUtc)
                        .ToList(),
                    AllReady = putnici.Any(item => item.Status == StatusRezervacije.Potvrdena) &&
                               putnici
                                   .Where(item => item.Status == StatusRezervacije.Potvrdena)
                                   .All(item => item.CheckInAtUtc.HasValue),
                    CashDue = putnici
                        .Where(item => item.Status == StatusRezervacije.Potvrdena &&
                                       item.NacinPlacanja == NacinPlacanja.Gotovina)
                        .Sum(item => item.CijenaUkupno + item.Napojnica)
                };
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id, CancellationToken cancellationToken)
    {
        var result = await _commands.ExecuteAsync(
            SideSeatActionTypes.StartRide,
            new StartRideCommand(id),
            User,
            "MVC",
            cancellationToken);
        TempData["RideExecuted"] = result.Message;
        return RedirectToAction(nameof(Current));
    }

    public async Task<IActionResult> Finish(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var ride = await _db.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Include(v => v.Rezervacije)
            .ThenInclude(r => r.Putnik)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (ride is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && ride.VozacId != userId.Value)
        {
            return Forbid();
        }

        var putnici = BuildPassengerRows(ride);
        return View(new RideSettlementViewModel
        {
            Voznja = ride,
            Putnici = putnici,
            CashDue = putnici
                .Where(item => item.Status == StatusRezervacije.Potvrdena && item.NacinPlacanja == NacinPlacanja.Gotovina)
                .Sum(item => item.CijenaUkupno + item.Napojnica),
            SaldoDue = putnici
                .Where(item => item.Status == StatusRezervacije.Potvrdena && item.NacinPlacanja == NacinPlacanja.SideSeatSaldo)
                .Sum(item => item.CijenaUkupno + item.Napojnica)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int id, bool cashCollected, CancellationToken cancellationToken)
    {
        var result = await _commands.ExecuteAsync(
            SideSeatActionTypes.FinishRide,
            new FinishRideCommand(id, cashCollected),
            User,
            "MVC",
            cancellationToken);
        TempData["RideExecuted"] = result.Message;
        return RedirectToAction(result.Succeeded ? nameof(Details) : nameof(Finish), new { id });
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
    public async Task<IActionResult> Create(
        VoznjaFormViewModel model,
        CancellationToken cancellationToken)
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

        var result = await _commands.ExecuteAsync(
            SideSeatActionTypes.CreateRide,
            new CreateRideCommand(
                model.VozacId,
                model.PolazniGradId,
                model.OdredisniGradId,
                model.Polazak,
                model.OcekivaniDolazak,
                model.CijenaPoMjestu,
                model.UkupnoMjesta,
                model.SlobodnaMjesta,
                model.Opis),
            User,
            "MVC",
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

        return RedirectToAction("Ride", "Confirmation", new { id = result.EntityId });
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
    public async Task<IActionResult> Edit(int id, VoznjaFormViewModel model, CancellationToken cancellationToken)
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

        var result = await _commands.ExecuteAsync(
            SideSeatActionTypes.UpdateRide,
            new UpdateRideCommand(
                voznja.Id,
                model.VozacId,
                model.PolazniGradId,
                model.OdredisniGradId,
                model.Polazak,
                model.OcekivaniDolazak,
                model.CijenaPoMjestu,
                model.UkupnoMjesta,
                model.SlobodnaMjesta,
                model.Opis,
                model.Status),
            User,
            "MVC",
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            PopulateFormOptions(model, isAdmin, userId.Value);
            return View(model);
        }

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

        var rezervacijaIds = voznja.Rezervacije.Select(r => r.Id).ToList();
        if (rezervacijaIds.Count > 0)
        {
            var ocjene = await _db.Ocjene
                .Where(o => rezervacijaIds.Contains(o.RezervacijaId))
                .ToListAsync();
            var placanja = await _db.Placanja
                .Where(p => rezervacijaIds.Contains(p.RezervacijaId))
                .ToListAsync();

            if (ocjene.Count > 0)
            {
                _db.Ocjene.RemoveRange(ocjene);
            }
            if (placanja.Count > 0)
            {
                _db.Placanja.RemoveRange(placanja);
            }

            _db.Rezervacije.RemoveRange(voznja.Rezervacije);
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

    private bool CanManageRide(Voznja voznja)
    {
        var userId = User.GetKorisnikId();
        return User.IsInRole("Admin") || (userId.HasValue && voznja.VozacId == userId.Value);
    }

    private static List<VoznjaPassengerRow> BuildPassengerRows(Voznja ride) =>
        ride.Rezervacije
            .OrderBy(reservation => reservation.VrijemeRezervacije)
            .Select(reservation => new VoznjaPassengerRow
            {
                RezervacijaId = reservation.Id,
                PutnikId = reservation.PutnikId,
                PutnikIme = $"{reservation.Putnik?.Ime} {reservation.Putnik?.Prezime}".Trim(),
                PutnikTelefon = reservation.Putnik?.BrojMobitela,
                Status = reservation.Status,
                BrojMjesta = reservation.BrojMjesta,
                NacinPlacanja = reservation.NacinPlacanja,
                CijenaUkupno = reservation.CijenaUkupno,
                Napojnica = reservation.Napojnica,
                CheckInAtUtc = reservation.CheckInAtUtc,
                LastLatitude = reservation.LastLatitude,
                LastLongitude = reservation.LastLongitude,
                LastLocationAtUtc = reservation.LastLocationAtUtc,
                CashCollectedAtUtc = reservation.CashCollectedAtUtc
            })
            .ToList();

    private async Task<IDbContextTransaction?> BeginSerializableTransactionAsync()
    {
        return _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
    }
}
