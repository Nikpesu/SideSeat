using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje popis placanja i detalje odabranog placanja.
/// </summary>
public class PlacanjeController : Controller
{
    private readonly LabMockRepository _repository;

    public PlacanjeController(LabMockRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        return View(_repository.GetPlacanja());
    }

    public IActionResult Details(int id)
    {
        var placanje = _repository.GetPlacanjeById(id);
        if (placanje is null)
        {
            return NotFound();
        }

        return View(placanje);
    }
}
