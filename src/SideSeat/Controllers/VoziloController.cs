using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje sva vozila i detalje pojedinog vozila.
/// </summary>
public class VoziloController : Controller
{
    private readonly LabMockRepository _repository;

    public VoziloController(LabMockRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        return View(_repository.GetVozila());
    }

    public IActionResult Details(int id)
    {
        var vozilo = _repository.GetVoziloById(id);
        if (vozilo is null)
        {
            return NotFound();
        }

        return View(vozilo);
    }
}
