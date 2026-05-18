using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Lab2;
using SideSeat.Security;
using System.Diagnostics;

namespace SideSeat.Controllers
{
    /// <summary>
    /// Prikazuje pocetni dashboard s osnovnim statistikama iz mock podataka.
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        private readonly SideSeatDbContext _db;

        public HomeController(SideSeatDbContext db)
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

            var isAdmin = User.IsInRole("Admin");
            var model = new Lab2DashboardViewModel
            {
                IsAdmin = isAdmin,
                BrojGradova = isAdmin ? _db.Gradovi.Count() : 0,
                BrojKorisnika = isAdmin ? _db.Korisnici.Count() : 0,
                BrojVozila = isAdmin ? _db.Vozila.Count() : 0,
                BrojVoznji = isAdmin ? _db.Voznje.Count() : 0,
                BrojRezervacija = isAdmin ? _db.Rezervacije.Count() : 0,
                BrojPlacanja = isAdmin ? _db.Placanja.Count() : 0,
                BrojOcjena = isAdmin ? _db.Ocjene.Count() : 0,
                BrojMojihVoznji = _db.Voznje.Count(v => v.VozacId == userId.Value),
                BrojMojihRezervacija = _db.Rezervacije.Count(r => r.PutnikId == userId.Value),
                BrojAktivnihVoznji = _db.Voznje.Count(v => v.Status == StatusVoznje.Planirana)
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
