using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Home;
using SideSeat.Security;
using System.Diagnostics;

namespace SideSeat.Controllers
{
    /// <summary>
    /// Prikazuje pocetni dashboard s osnovnim statistikama iz mock podataka.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly SideSeatDbContext _db;

        public HomeController(SideSeatDbContext db)
        {
            _db = db;
        }

        [AllowAnonymous]
        public IActionResult Index(int? from, int? to, DateTime? date)
        {
            var userId = User.GetKorisnikId();
            var isAuthenticated = User.Identity?.IsAuthenticated == true;
            var isAdmin = User.IsInRole("Admin");

            var query = _db.Voznje
                .AsNoTracking()
                .Include(v => v.Vozac)
                .Include(v => v.PolazniGrad)
                .Include(v => v.OdredisniGrad)
                .Where(v => v.Status == StatusVoznje.Planirana && v.SlobodnaMjesta > 0)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(v => v.PolazniGradId == from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(v => v.OdredisniGradId == to.Value);
            }

            if (date.HasValue)
            {
                var start = date.Value.Date;
                var end = start.AddDays(1);
                query = query.Where(v => v.Polazak >= start && v.Polazak < end);
            }

            var searchResults = query
                .OrderBy(v => v.Polazak)
                .Take(20)
                .Select(v => new HomeVoznjaSearchRow
                {
                    Id = v.Id,
                    Polazak = v.Polazak,
                    PolazniGrad = v.PolazniGrad.Naziv,
                    OdredisniGrad = v.OdredisniGrad.Naziv,
                    PolazniGradLatitude = v.PolazniGrad.Latitude,
                    PolazniGradLongitude = v.PolazniGrad.Longitude,
                    OdredisniGradLatitude = v.OdredisniGrad.Latitude,
                    OdredisniGradLongitude = v.OdredisniGrad.Longitude,
                    SlobodnaMjesta = v.SlobodnaMjesta,
                    CijenaPoMjestu = v.CijenaPoMjestu,
                    Vozac = v.Vozac.Ime + " " + v.Vozac.Prezime
                })
                .ToList();

            var fromText = from.HasValue
                ? _db.Gradovi.AsNoTracking().Where(g => g.Id == from.Value).Select(g => g.Naziv).FirstOrDefault() ?? string.Empty
                : string.Empty;
            var toText = to.HasValue
                ? _db.Gradovi.AsNoTracking().Where(g => g.Id == to.Value).Select(g => g.Naziv).FirstOrDefault() ?? string.Empty
                : string.Empty;

            return View(new HomeDashboardViewModel
            {
                IsAuthenticated = isAuthenticated,
                IsAdmin = isAdmin,
                BrojGradova = _db.Gradovi.Count(),
                BrojKorisnika = _db.Korisnici.Count(),
                BrojVoznji = _db.Voznje.Count(),
                BrojRezervacija = _db.Rezervacije.Count(),
                BrojOcjena = _db.Ocjene.Count(),
                BrojAktivnihVoznji = _db.Voznje.Count(v => v.Status == StatusVoznje.Planirana && v.SlobodnaMjesta > 0),
                BrojMojihVoznji = userId is null ? 0 : _db.Voznje.Count(v => v.VozacId == userId.Value),
                BrojMojihRezervacija = userId is null ? 0 : _db.Rezervacije.Count(r => r.PutnikId == userId.Value),
                SearchFrom = from,
                SearchTo = to,
                SearchFromText = fromText,
                SearchToText = toText,
                SearchDate = date,
                SearchResults = searchResults
            });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SearchRouteLocations(string q, string mode = "from", int? from = null, int? to = null, string? fromText = null, string? toText = null)
        {
            var normalizedMode = (mode ?? "from").Trim().ToLowerInvariant();
            if (normalizedMode is not ("from" or "to"))
            {
                normalizedMode = "from";
            }

            if (!from.HasValue && !string.IsNullOrWhiteSpace(fromText))
            {
                var normalized = fromText.Trim();
                from = _db.Gradovi
                    .AsNoTracking()
                    .Where(g => g.Naziv == normalized)
                    .Select(g => (int?)g.Id)
                    .FirstOrDefault();
            }

            if (!to.HasValue && !string.IsNullOrWhiteSpace(toText))
            {
                var normalized = toText.Trim();
                to = _db.Gradovi
                    .AsNoTracking()
                    .Where(g => g.Naziv == normalized)
                    .Select(g => (int?)g.Id)
                    .FirstOrDefault();
            }

            var rides = _db.Voznje
                .AsNoTracking()
                .Where(v => v.Status == StatusVoznje.Planirana && v.SlobodnaMjesta > 0);

            if (normalizedMode == "from")
            {
                if (to.HasValue)
                {
                    rides = rides.Where(v => v.OdredisniGradId == to.Value);
                }

                var query = _db.Gradovi
                    .AsNoTracking()
                    .Where(g => rides.Select(v => v.PolazniGradId).Distinct().Contains(g.Id));

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var text = q.Trim();
                    query = query.Where(g => EF.Functions.Like(g.Naziv, $"%{text}%"));
                }

                var results = query
                    .OrderBy(g => g.Naziv)
                    .Take(15)
                    .Select(g => new
                    {
                        id = g.Id.ToString(),
                        text = g.Naziv,
                        subtext = $"{g.Drzava}, {g.PostanskiBroj}"
                    })
                    .ToList();

                return Json(results);
            }

            if (from.HasValue)
            {
                rides = rides.Where(v => v.PolazniGradId == from.Value);
            }

            var toQuery = _db.Gradovi
                .AsNoTracking()
                .Where(g => rides.Select(v => v.OdredisniGradId).Distinct().Contains(g.Id));

            if (!string.IsNullOrWhiteSpace(q))
            {
                var text = q.Trim();
                toQuery = toQuery.Where(g => EF.Functions.Like(g.Naziv, $"%{text}%"));
            }

            var toResults = toQuery
                .OrderBy(g => g.Naziv)
                .Take(15)
                .Select(g => new
                {
                    id = g.Id.ToString(),
                    text = g.Naziv,
                    subtext = $"{g.Drzava}, {g.PostanskiBroj}"
                })
                .ToList();

            return Json(toResults);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Vodic()
        {
            return View();
        }

        [AllowAnonymous]
        [Route("Home/HttpStatus/{code:int}")]
        public IActionResult HttpStatus(int code)
        {
            Response.StatusCode = code;
            ViewBag.StatusCode = code;
            ViewBag.StatusTitle = code switch
            {
                403 => "Pristup odbijen",
                404 => "Stranica nije pronađena",
                _ => "Greška"
            };
            ViewBag.StatusMessage = code switch
            {
                403 => "Nemate permisije za pristup ovoj stranici.",
                404 => "Tražena stranica ne postoji ili je uklonjena.",
                _ => "Dogodila se neočekivana greška."
            };
            return View("HttpStatus");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string? requestId = null)
        {
            return View(new ErrorViewModel
            {
                RequestId = requestId ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
