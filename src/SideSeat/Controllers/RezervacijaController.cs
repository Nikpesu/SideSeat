using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje rezervacije i detalje pojedine rezervacije.
/// </summary>
public class RezervacijaController : Controller
{
    private readonly LabMockRepository _repository;

    public RezervacijaController(LabMockRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        return View(_repository.GetRezervacije());
    }

    public IActionResult Details(int id)
    {
        var rezervacija = _repository.GetRezervacijaById(id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        return View(rezervacija);
    }
}
