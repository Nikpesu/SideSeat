using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje sve ocjene voznji i detalje pojedine ocjene.
/// </summary>
public class OcjenaController : Controller
{
    private readonly LabMockRepository _repository;

    public OcjenaController(LabMockRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        return View(_repository.GetOcjene());
    }

    public IActionResult Details(int id)
    {
        var ocjena = _repository.GetOcjenaById(id);
        if (ocjena is null)
        {
            return NotFound();
        }

        return View(ocjena);
    }
}
