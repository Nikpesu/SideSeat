using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu korisnika i detalje odabranog korisnika.
/// </summary>
public class KorisnikController : Controller
{
    private readonly SideSeatEfRepository _repository;

    public KorisnikController(SideSeatEfRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        return View(_repository.GetKorisnici());
    }

    public IActionResult Details(int id)
    {
        var korisnik = _repository.GetKorisnikById(id);
        if (korisnik is null)
        {
            return NotFound();
        }

        return View(korisnik);
    }
}
