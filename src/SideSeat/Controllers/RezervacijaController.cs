using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Forms;
using SideSeat.Models.Commands;
using SideSeat.Models.ViewModels;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.Controllers;

[Authorize]
public class RezervacijaController : Controller
{
    private readonly SideSeatDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ISideSeatCommandService _commands;

    public RezervacijaController(
        SideSeatDbContext db,
        INotificationService notifications,
        ISideSeatCommandService commands)
    {
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
        var canViewRideReservations = isAdmin
                                      || User.IsInRole("Driver")
                                      || korisnikTip is TipKorisnika.Vozac or TipKorisnika.VozacIPutnik;
        var selectedView = NormalizeReservationView(view, isAdmin);
        var selectedStatus = NormalizeStatusFilter(status);

        if (!isAdmin && selectedView == "all")
        {
            return Forbid();
        }

        if (!canViewRideReservations && selectedView == "my-rides")
        {
            return Forbid();
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
            .AsQueryable();

        query = selectedView switch
        {
            "mine" => query.Where(r => r.PutnikId == userId.Value),
            "my-rides" => query.Where(r => r.Voznja.VozacId == userId.Value),
            _ => query
        };

        var statusCounts = query
            .Select(r => r.Status)
            .ToList();

        query = selectedStatus switch
        {
            "pending" => query.Where(r => r.Status == StatusRezervacije.UProcesuPotvrde),
            "confirmed" => query.Where(r => r.Status == StatusRezervacije.Potvrdena),
            "rejected" => query.Where(r => r.Status == StatusRezervacije.Odbijena),
            "completed" => query.Where(r => r.Status == StatusRezervacije.Zavrsena),
            _ => query
        };

        var rezervacije = query
            .OrderByDescending(r => r.VrijemeRezervacije)
            .ToList();
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
                          && rezervacija.Status == StatusRezervacije.Zavrsena
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

        var display = GetReservationViewDisplay(selectedView);
        return View(new RezervacijaListViewModel
        {
            Rezervacije = items,
            SelectedView = selectedView,
            Title = display.Title,
            Description = display.Description,
            EmptyMessage = display.EmptyMessage,
            IsAdmin = isAdmin,
            CanViewRideReservations = canViewRideReservations,
            SelectedStatus = selectedStatus,
            AllCount = statusCounts.Count,
            PendingCount = statusCounts.Count(value => value == StatusRezervacije.UProcesuPotvrde),
            ConfirmedCount = statusCounts.Count(value => value == StatusRezervacije.Potvrdena),
            RejectedCount = statusCounts.Count(value => value == StatusRezervacije.Odbijena),
            CompletedCount = statusCounts.Count(value => value == StatusRezervacije.Zavrsena)
        });
    }

    private static string NormalizeStatusFilter(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "pending" => "pending",
            "confirmed" => "confirmed",
            "rejected" => "rejected",
            "completed" => "completed",
            _ => "all"
        };

    private static string NormalizeReservationView(string? view, bool isAdmin)
    {
        var normalized = view?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "all" => "all",
            "mine" => "mine",
            "my-rides" => "my-rides",
            _ => isAdmin ? "all" : "mine"
        };
    }

    private static (string Title, string Description, string EmptyMessage) GetReservationViewDisplay(string view) =>
        view switch
        {
            "mine" => ("Moje rezervacije", "Rezervacije koje si napravio kao putnik.", "Nemas vlastitih rezervacija."),
            "my-rides" => ("Rezervacije mojih voznji", "Rezervacije putnika na voznjama koje vozis.", "Nema rezervacija na tvojim voznjama."),
            _ => ("Sve rezervacije", "Administratorski pregled svih rezervacija.", "Nema unesenih rezervacija.")
        };

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

    public IActionResult Payment(int id) => RedirectToAction(nameof(Details), new { id });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(
        int id,
        NacinPlacanja nacinPlacanja,
        CancellationToken cancellationToken)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var rezervacija = await _db.Rezervacije
            .Include(r => r.Voznja)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rezervacija is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && rezervacija.PutnikId != userId.Value)
        {
            return Forbid();
        }

        if (rezervacija.Status == StatusRezervacije.Zavrsena ||
            rezervacija.Voznja.Status == StatusVoznje.Zavrsena ||
            nacinPlacanja is not (NacinPlacanja.SideSeatSaldo or NacinPlacanja.Gotovina))
        {
            TempData["ReservationStatus"] = "Način plaćanja nije valjan za ovu rezervaciju.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (nacinPlacanja == NacinPlacanja.SideSeatSaldo)
        {
            var korisnik = await _db.Korisnici
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == rezervacija.PutnikId, cancellationToken);
            var committed = GetCommittedReservationAmount(rezervacija.PutnikId) -
                            (rezervacija.NacinPlacanja == NacinPlacanja.SideSeatSaldo
                                ? rezervacija.CijenaUkupno
                                : 0);
            if (korisnik is null || korisnik.Saldo < committed + rezervacija.CijenaUkupno)
            {
                TempData["ReservationStatus"] = "Nema dovoljno raspoloživog salda za odabrani način plaćanja.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        rezervacija.NacinPlacanja = nacinPlacanja;
        await _db.SaveChangesAsync(cancellationToken);
        TempData["ReservationStatus"] = "Plaćanje je ažurirano.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tip(int id, decimal napojnica, CancellationToken cancellationToken)
    {
        _ = napojnica;
        await Task.CompletedTask;
        TempData["ReservationStatus"] = "Napojnica se dodaje pri ocjenjivanju vozača karticom.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(
        int id,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken)
    {
        var result = await _commands.ExecuteAsync(
            SideSeatActionTypes.CheckInReservation,
            new CheckInReservationCommand(id, latitude, longitude),
            User,
            "MVC",
            cancellationToken);
        TempData["ReservationStatus"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
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
    public async Task<IActionResult> Create(RezervacijaFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = await _db.Voznje
            .FirstOrDefaultAsync(v => v.Id == model.VoznjaId, cancellationToken);
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

        var cijenaNoveRezervacije = voznja.CijenaPoMjestu * model.BrojMjesta;
        var korisnik = await _db.Korisnici
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == userId.Value, cancellationToken);
        if (korisnik is null)
        {
            return NotFound();
        }

        var rezerviranaSredstva = GetCommittedReservationAmount(userId.Value);
        var potrebanSaldo = rezerviranaSredstva + cijenaNoveRezervacije;
        if (model.NacinPlacanja == NacinPlacanja.SideSeatSaldo &&
            korisnik.Saldo < potrebanSaldo)
        {
            var nedostaje = potrebanSaldo - korisnik.Saldo;
            TempData["FundsToastMessage"] =
                $"Za ovu rezervaciju i već zakazane rezervacije nedostaje ti {nedostaje:0.00} EUR.";
            TempData["FundsToastHref"] = Url.Action(
                "Uplata",
                "Korisnik",
                new
                {
                    amount = nedostaje,
                    returnUrl = Url.Action(nameof(Create), "Rezervacija", new { voznjaId = voznja.Id })
                });

            ViewBag.Voznja = _db.Voznje
                .AsNoTracking()
                .Include(v => v.PolazniGrad)
                .Include(v => v.OdredisniGrad)
                .First(v => v.Id == model.VoznjaId);
            return View(model);
        }

        var result = await _commands.ExecuteAsync(
            SideSeatActionTypes.CreateReservation,
            new CreateReservationCommand(
                model.VoznjaId,
                model.BrojMjesta,
                model.Napomena,
                model.NacinPlacanja,
                0),
            User,
            "MVC",
            cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.Voznja = _db.Voznje
                .AsNoTracking()
                .Include(v => v.PolazniGrad)
                .Include(v => v.OdredisniGrad)
                .First(v => v.Id == model.VoznjaId);
            return View(model);
        }

        return RedirectToAction("Reservation", "Confirmation", new { id = result.EntityId });
    }

    private decimal GetCommittedReservationAmount(int korisnikId) =>
        _db.Rezervacije
            .AsNoTracking()
            .Where(r =>
                r.PutnikId == korisnikId &&
                r.NacinPlacanja == NacinPlacanja.SideSeatSaldo &&
                (r.Status == StatusRezervacije.UProcesuPotvrde ||
                 r.Status == StatusRezervacije.Potvrdena))
            .Sum(r => (decimal?)r.CijenaUkupno) ?? 0m;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, string? returnUrl)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        await using var transaction = await BeginSerializableTransactionAsync();
        var rezervacija = await _db.Rezervacije
            .Include(r => r.Voznja)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        var canConfirm = User.IsInRole("Admin") || rezervacija.Voznja.VozacId == userId.Value;
        if (!canConfirm)
        {
            return Forbid();
        }

        if (rezervacija.Status != StatusRezervacije.UProcesuPotvrde)
        {
            TempData["ReservationStatus"] = "Samo rezervacija u procesu potvrde može biti potvrđena.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (rezervacija.Voznja.SlobodnaMjesta < rezervacija.BrojMjesta)
        {
            TempData["ReservationStatus"] = "Nema dovoljno slobodnih mjesta za potvrdu rezervacije.";
            return RedirectToAction(nameof(Details), new { id });
        }

        rezervacija.Voznja.SlobodnaMjesta -= rezervacija.BrojMjesta;
        rezervacija.Status = StatusRezervacije.Potvrdena;
        _notifications.Add(
            rezervacija.PutnikId,
            "Rezervacija potvrđena",
            $"Vozač je potvrdio rezervaciju #{rezervacija.Id}.",
            "Rezervacija",
            $"/Rezervacija/Details/{rezervacija.Id}");
        await _db.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return RedirectAfterReservationAction(returnUrl, id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? returnUrl)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        await using var transaction = await BeginSerializableTransactionAsync();
        var rezervacija = await _db.Rezervacije
            .Include(r => r.Voznja)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && rezervacija.Voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        if (rezervacija.Status != StatusRezervacije.UProcesuPotvrde)
        {
            TempData["ReservationStatus"] = "Samo rezervacija u procesu potvrde može biti odbijena.";
            return RedirectToAction(nameof(Details), new { id });
        }

        rezervacija.Status = StatusRezervacije.Odbijena;
        _notifications.Add(
            rezervacija.PutnikId,
            "Rezervacija odbijena",
            $"Vozač je odbio rezervaciju #{rezervacija.Id}.",
            "Rezervacija",
            $"/Rezervacija/Details/{rezervacija.Id}");
        await _db.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }
        return RedirectAfterReservationAction(returnUrl, id);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminCreate()
    {
        return View(new RezervacijaAdminFormViewModel
        {
            Status = StatusRezervacije.UProcesuPotvrde,
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

        var reservedSeats = UsesRideCapacity(model.Status) ? model.BrojMjesta : 0;
        if (reservedSeats > voznja.SlobodnaMjesta)
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

        voznja.SlobodnaMjesta -= reservedSeats;
        _db.Rezervacije.Add(rezervacija);
        await _db.SaveChangesAsync();
        _notifications.Add(
            rezervacija.PutnikId,
            "Rezervacija kreirana",
            $"Administrator je kreirao rezervaciju #{rezervacija.Id}.",
            "Rezervacija",
            $"/Rezervacija/Details/{rezervacija.Id}");
        if (rezervacija.Status == StatusRezervacije.UProcesuPotvrde)
        {
            _notifications.Add(
                voznja.VozacId,
                "Nova rezervacija",
                $"Rezervacija #{rezervacija.Id} čeka tvoju potvrdu.",
                "Rezervacija",
                $"/Voznja/Details/{voznja.Id}");
        }
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

        var oldStatus = rezervacija.Status;
        var oldReservedSeats = UsesRideCapacity(rezervacija.Status) ? rezervacija.BrojMjesta : 0;
        var newReservedSeats = UsesRideCapacity(model.Status) ? model.BrojMjesta : 0;

        if (rezervacija.VoznjaId == model.VoznjaId)
        {
            var delta = newReservedSeats - oldReservedSeats;
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

            if (novaVoznja.SlobodnaMjesta < newReservedSeats)
            {
                ModelState.AddModelError(nameof(model.BrojMjesta), "Nema dovoljno slobodnih mjesta na novoj voznji.");
                return View(model);
            }

            rezervacija.Voznja.SlobodnaMjesta += oldReservedSeats;
            novaVoznja.SlobodnaMjesta -= newReservedSeats;
            rezervacija.VoznjaId = model.VoznjaId;
            rezervacija.Voznja = novaVoznja;
        }

        rezervacija.PutnikId = model.PutnikId;
        rezervacija.BrojMjesta = model.BrojMjesta;
        rezervacija.Status = model.Status;
        rezervacija.VrijemeRezervacije = model.VrijemeRezervacije;
        rezervacija.Napomena = model.Napomena.Trim();
        rezervacija.CijenaUkupno = rezervacija.Voznja.CijenaPoMjestu * model.BrojMjesta;

        if (oldStatus != model.Status)
        {
            _notifications.Add(
                rezervacija.PutnikId,
                "Promjena rezervacije",
                $"Status rezervacije #{rezervacija.Id} promijenjen je u {model.Status.ToDisplayName()}.",
                "Rezervacija",
                $"/Rezervacija/Details/{rezervacija.Id}");
        }
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

        if (UsesRideCapacity(rezervacija.Status))
        {
            rezervacija.Voznja.SlobodnaMjesta += rezervacija.BrojMjesta;
        }
        _notifications.Add(
            rezervacija.PutnikId,
            "Rezervacija obrisana",
            $"Rezervacija #{rezervacija.Id} je obrisana.",
            "Rezervacija");
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

    private static bool UsesRideCapacity(StatusRezervacije status) =>
        status is StatusRezervacije.Potvrdena or StatusRezervacije.Zavrsena;

    private IActionResult RedirectAfterReservationAction(string? returnUrl, int rezervacijaId)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Details), new { id = rezervacijaId });
    }

    private async Task<IDbContextTransaction?> BeginSerializableTransactionAsync()
    {
        return _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
    }
}
