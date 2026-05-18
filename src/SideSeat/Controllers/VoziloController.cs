using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje sva vozila i detalje pojedinog vozila.
/// </summary>
[Authorize(Roles = "Admin")]
public class VoziloController : Controller
{
    private readonly SideSeatEfRepository _repository;

    public VoziloController(SideSeatEfRepository repository)
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
