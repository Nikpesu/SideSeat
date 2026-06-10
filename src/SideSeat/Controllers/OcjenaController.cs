using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Ocjena;
using SideSeat.Models.ViewModels;
using SideSeat.Repositories;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje sve ocjene voznji i detalje pojedine ocjene.
/// </summary>
[Authorize]
public class OcjenaController : Controller
{
    private const int MaxReviewImageCount = 5;
    private const long MaxReviewImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
    };

    private readonly SideSeatEfRepository _repository;
    private readonly SideSeatDbContext _db;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly INotificationService _notifications;

    public OcjenaController(
        SideSeatEfRepository repository,
        SideSeatDbContext db,
        IWebHostEnvironment webHostEnvironment,
        INotificationService notifications)
    {
        _repository = repository;
        _db = db;
        _webHostEnvironment = webHostEnvironment;
        _notifications = notifications;
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
        var isAdmin = User.IsInRole("Admin");
        var ratingsQuery = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Slike)
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
            .AsQueryable();
        if (!isAdmin)
        {
            ratingsQuery = ratingsQuery.Where(o => reservationIds.Contains(o.RezervacijaId));
        }

        var ratings = ratingsQuery
            .OrderByDescending(o => o.Kreirano)
            .ToList();

        var given = ratings
            .Where(o => o.AutorId == userId.Value)
            .Select(o => MapHistoryRow(o, canDeleteImages: true))
            .ToList();

        var received = ratings
            .Where(o =>
            {
                var targetId = ResolveTargetId(o);
                return targetId == userId.Value;
            })
            .Select(o => MapHistoryRow(o, canDeleteImages: isAdmin))
            .ToList();

        var ratedByMeIds = ratings
            .Where(o => o.AutorId == userId.Value)
            .Select(o => o.RezervacijaId)
            .ToHashSet();

        var pending = relevantReservations
            .Where(r =>
                r.Voznja.Status == StatusVoznje.Zavrsena &&
                r.Status == StatusRezervacije.Zavrsena &&
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
            AdminAll = isAdmin
                ? ratings.Select(o => MapHistoryRow(o, canDeleteImages: true)).ToList()
                : new List<OcjenaHistoryItemViewModel>(),
            GivenAverage = given.Count == 0 ? 0 : given.Average(x => x.BrojZvjezdica),
            ReceivedAverage = received.Count == 0 ? 0 : received.Average(x => x.BrojZvjezdica)
        };

        return View(vm);
    }

    private static int ResolveTargetId(OcjenaVoznje o) =>
        o.AutorId == o.Rezervacija.Voznja.VozacId
            ? o.Rezervacija.PutnikId
            : o.Rezervacija.Voznja.VozacId;

    private static OcjenaHistoryItemViewModel MapHistoryRow(OcjenaVoznje o, bool canDeleteImages)
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
            Uredeno = o.Uredeno,
            Administratorska = o.Administratorska,
            RouteLabel = $"{o.Rezervacija.Voznja.PolazniGrad.Naziv} -> {o.Rezervacija.Voznja.OdredisniGrad.Naziv}",
            Slike = MapImages(o.Slike, canDeleteImages)
        };
    }

    public IActionResult Details(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var ocjena = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Slike)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .FirstOrDefault(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        var targetId = ResolveTargetId(ocjena);
        if (!User.IsInRole("Admin") && ocjena.AutorId != userId.Value && targetId != userId.Value)
        {
            return Forbid();
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

        if (rezervacija.Voznja.Status != StatusVoznje.Zavrsena || rezervacija.Status != StatusRezervacije.Zavrsena)
        {
            return BadRequest("Ocjenjivanje nije dostupno za ovu voznju.");
        }

        if (rezervacija.PutnikId != userId.Value && rezervacija.Voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        var previousReviewCount = _db.Ocjene.Count(o => o.RezervacijaId == rezervacijaId && o.AutorId == userId.Value);

        var targetName = rezervacija.Voznja.VozacId == userId.Value
            ? $"{rezervacija.Putnik.Ime} {rezervacija.Putnik.Prezime}"
            : $"{rezervacija.Voznja.Vozac.Ime} {rezervacija.Voznja.Vozac.Prezime}";

        return View(new CreateOcjenaViewModel
        {
            RezervacijaId = rezervacijaId,
            TargetName = targetName,
            IsAdditional = previousReviewCount > 0
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

        if (rezervacija.Voznja.Status != StatusVoznje.Zavrsena || rezervacija.Status != StatusRezervacije.Zavrsena)
        {
            return BadRequest("Ocjenjivanje nije dostupno za ovu voznju.");
        }

        if (rezervacija.PutnikId != userId.Value && rezervacija.Voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            model.TargetName = rezervacija.Voznja.VozacId == userId.Value
                ? $"{rezervacija.Putnik.Ime} {rezervacija.Putnik.Prezime}"
                : $"{rezervacija.Voznja.Vozac.Ime} {rezervacija.Voznja.Vozac.Prezime}";
            return View(model);
        }

        if (!ValidateReviewImages(model.Slike, nameof(model.Slike)))
        {
            model.TargetName = rezervacija.Voznja.VozacId == userId.Value
                ? $"{rezervacija.Putnik.Ime} {rezervacija.Putnik.Prezime}"
                : $"{rezervacija.Voznja.Vozac.Ime} {rezervacija.Voznja.Vozac.Prezime}";
            return View(model);
        }

        var ocjena = new OcjenaVoznje
        {
            RezervacijaId = model.RezervacijaId,
            AutorId = userId.Value,
            BrojZvjezdica = model.BrojZvjezdica,
            Komentar = model.Komentar.Trim(),
            Kreirano = DateTime.UtcNow
        };

        _db.Ocjene.Add(ocjena);
        await _db.SaveChangesAsync();
        await SaveReviewImagesAsync(ocjena.Id, model.Slike);
        var targetId = rezervacija.Voznja.VozacId == userId.Value
            ? rezervacija.PutnikId
            : rezervacija.Voznja.VozacId;
        _notifications.Add(
            targetId,
            "Nova ocjena",
            $"Dobio si ocjenu {model.BrojZvjezdica}/5 za rezervaciju #{rezervacija.Id}.",
            "Ocjena",
            $"/Ocjena/Details/{ocjena.Id}");
        await _db.SaveChangesAsync();
        return RedirectToAction("Index", "Rezervacija");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var ocjena = await _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Slike)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Putnik)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .ThenInclude(v => v.Vozac)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        if (ocjena.AutorId != userId.Value)
        {
            return Forbid();
        }

        return View(new EditOcjenaViewModel
        {
            Id = ocjena.Id,
            RezervacijaId = ocjena.RezervacijaId,
            TargetName = ResolveTargetName(ocjena.Rezervacija, userId.Value),
            BrojZvjezdica = ocjena.BrojZvjezdica,
            Komentar = ocjena.Komentar,
            PostojeceSlike = MapImages(ocjena.Slike, canDelete: true)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditOcjenaViewModel model)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        if (id != model.Id)
        {
            return BadRequest();
        }

        var ocjena = await _db.Ocjene
            .Include(o => o.Slike)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Putnik)
            .Include(o => o.Rezervacija)
            .ThenInclude(r => r.Voznja)
            .ThenInclude(v => v.Vozac)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        if (ocjena.AutorId != userId.Value)
        {
            return Forbid();
        }

        model.TargetName = ResolveTargetName(ocjena.Rezervacija, userId.Value);
        model.PostojeceSlike = MapImages(ocjena.Slike, canDelete: true);
        if (!ModelState.IsValid ||
            !ValidateReviewImages(model.Slike, nameof(model.Slike), ocjena.Slike.Count))
        {
            return View(model);
        }

        ocjena.BrojZvjezdica = model.BrojZvjezdica;
        ocjena.Komentar = model.Komentar.Trim();
        ocjena.Uredeno = DateTime.UtcNow;
        await SaveReviewImagesAsync(ocjena.Id, model.Slike);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
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

        if (!ValidateReviewImages(model.Slike, nameof(model.Slike)))
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

        var ocjena = new OcjenaVoznje
        {
            RezervacijaId = model.RezervacijaId,
            AutorId = model.AutorId,
            BrojZvjezdica = model.BrojZvjezdica,
            Komentar = model.Komentar.Trim(),
            Kreirano = model.Kreirano,
            Administratorska = true
        };

        _db.Ocjene.Add(ocjena);
        await _db.SaveChangesAsync();
        await SaveReviewImagesAsync(ocjena.Id, model.Slike);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminEdit(int id)
    {
        var ocjena = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Slike)
            .FirstOrDefault(o => o.Id == id);
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
            Kreirano = ocjena.Kreirano,
            PostojeceSlike = MapImages(ocjena.Slike, canDelete: true)
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

        var ocjena = await _db.Ocjene
            .Include(o => o.Slike)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        model.PostojeceSlike = MapImages(ocjena.Slike, canDelete: true);
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

        if (!ValidateReviewImages(model.Slike, nameof(model.Slike), ocjena.Slike.Count))
        {
            return View(model);
        }

        ocjena.RezervacijaId = model.RezervacijaId;
        ocjena.AutorId = model.AutorId;
        ocjena.BrojZvjezdica = model.BrojZvjezdica;
        ocjena.Komentar = model.Komentar.Trim();
        ocjena.Kreirano = model.Kreirano;
        ocjena.Uredeno = DateTime.UtcNow;

        await SaveReviewImagesAsync(ocjena.Id, model.Slike);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = ocjena.Id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminDelete(int id)
    {
        var ocjena = _db.Ocjene
            .AsNoTracking()
            .Include(o => o.Autor)
            .Include(o => o.Slike)
            .FirstOrDefault(o => o.Id == id);
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
        var ocjena = await _db.Ocjene
            .Include(o => o.Slike)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (ocjena is null)
        {
            return NotFound();
        }

        DeleteReviewImageFiles(ocjena.Slike);
        _db.Ocjene.Remove(ocjena);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var slika = await _db.OcjenaSlike
            .Include(s => s.OcjenaVoznje)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (slika is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && slika.OcjenaVoznje.AutorId != userId.Value)
        {
            return Forbid();
        }

        DeleteReviewImageFile(slika);
        _db.OcjenaSlike.Remove(slika);
        slika.OcjenaVoznje.Uredeno = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Json(new { success = true, imageId = id });
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Attachments()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AttachmentList()
    {
        var attachments = await _db.OcjenaSlike
            .AsNoTracking()
            .Include(s => s.OcjenaVoznje)
            .ThenInclude(o => o.Autor)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new AdminOcjenaAttachmentViewModel
            {
                ImageId = s.Id,
                OcjenaId = s.OcjenaVoznjeId,
                RezervacijaId = s.OcjenaVoznje.RezervacijaId,
                Autor = $"{s.OcjenaVoznje.Autor.Ime} {s.OcjenaVoznje.Autor.Prezime}",
                FileName = s.FileName,
                FilePath = s.FilePath,
                FileSize = s.FileSize,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return PartialView("_AttachmentList", attachments);
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

    private static List<OcjenaSlikaViewModel> MapImages(IEnumerable<OcjenaSlika> slike, bool canDelete = false) =>
        slike
            .OrderBy(s => s.CreatedAt)
            .Select(s => new OcjenaSlikaViewModel
            {
                Id = s.Id,
                OcjenaVoznjeId = s.OcjenaVoznjeId,
                FileName = s.FileName,
                FilePath = s.FilePath,
                CanDelete = canDelete
            })
            .ToList();

    private bool ValidateReviewImages(IEnumerable<IFormFile>? files, string fieldName, int existingCount = 0)
    {
        var images = files?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
        if (images.Count == 0)
        {
            return true;
        }

        if (existingCount + images.Count > MaxReviewImageCount)
        {
            ModelState.AddModelError(fieldName, $"Recenzija moze imati najvise {MaxReviewImageCount} slika.");
            return false;
        }

        foreach (var image in images)
        {
            var extension = Path.GetExtension(image.FileName);
            var isImage = !string.IsNullOrWhiteSpace(image.ContentType) &&
                image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            if (!isImage || !AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(fieldName, "Dopustene su samo slike: JPG, PNG, GIF ili WEBP.");
                return false;
            }

            if (image.Length > MaxReviewImageBytes)
            {
                ModelState.AddModelError(fieldName, "Jedna slika moze imati najvise 5 MB.");
                return false;
            }
        }

        return true;
    }

    private async Task SaveReviewImagesAsync(int ocjenaId, IEnumerable<IFormFile>? files)
    {
        var images = files?.Where(f => f.Length > 0).ToList() ?? new List<IFormFile>();
        if (images.Count == 0)
        {
            return;
        }

        var webRootPath = _webHostEnvironment.WebRootPath ??
            Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        var uploadsPath = Path.Combine(webRootPath, "uploads", "ocjene", ocjenaId.ToString());
        Directory.CreateDirectory(uploadsPath);

        foreach (var image in images)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var diskPath = Path.Combine(uploadsPath, storedFileName);

            await using (var stream = new FileStream(diskPath, FileMode.CreateNew))
            {
                await image.CopyToAsync(stream);
            }

            _db.OcjenaSlike.Add(new OcjenaSlika
            {
                OcjenaVoznjeId = ocjenaId,
                FileName = Path.GetFileName(image.FileName),
                FilePath = $"/uploads/ocjene/{ocjenaId}/{storedFileName}",
                ContentType = image.ContentType,
                FileSize = image.Length,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private void DeleteReviewImageFiles(IEnumerable<OcjenaSlika> slike)
    {
        var webRootPath = _webHostEnvironment.WebRootPath ??
            Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");

        foreach (var slika in slike)
        {
            DeleteReviewImageFile(slika, webRootPath);
        }
    }

    private void DeleteReviewImageFile(OcjenaSlika slika, string? webRootPath = null)
    {
        webRootPath ??= _webHostEnvironment.WebRootPath ??
            Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        var relativePath = slika.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var diskPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "ocjene"));
        if (diskPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(diskPath))
        {
            System.IO.File.Delete(diskPath);
        }
    }

    private static string ResolveTargetName(Rezervacija rezervacija, int authorId) =>
        rezervacija.Voznja.VozacId == authorId
            ? $"{rezervacija.Putnik.Ime} {rezervacija.Putnik.Prezime}".Trim()
            : $"{rezervacija.Voznja.Vozac.Ime} {rezervacija.Voznja.Vozac.Prezime}".Trim();
}
