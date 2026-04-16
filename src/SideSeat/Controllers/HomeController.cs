using Microsoft.AspNetCore.Mvc;
using SideSeat.Models;
using SideSeat.Models.Lab2;
using SideSeat.Repositories;
using System.Diagnostics;

namespace SideSeat.Controllers
{
    /// <summary>
    /// Prikazuje pocetni dashboard s osnovnim statistikama iz mock podataka.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly LabMockRepository _repository;

        public HomeController(LabMockRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            var model = new Lab2DashboardViewModel
            {
                BrojGradova = _repository.GetGradovi().Count,
                BrojKorisnika = _repository.GetKorisnici().Count,
                BrojVozila = _repository.GetVozila().Count,
                BrojVoznji = _repository.GetVoznje().Count,
                BrojRezervacija = _repository.GetRezervacije().Count,
                BrojPlacanja = _repository.GetPlacanja().Count,
                BrojOcjena = _repository.GetOcjene().Count
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
