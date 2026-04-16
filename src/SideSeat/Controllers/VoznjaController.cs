using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu voznji i detaljan prikaz jedne voznje.
/// </summary>
public class VoznjaController : Controller
{
    private readonly LabMockRepository _repository;

    public VoznjaController(LabMockRepository repository)
    {
        _repository = repository;
    }

    public IActionResult Index()
    {
        return View(_repository.GetVoznje());
    }

    public IActionResult Details(int id)
    {
        var voznja = _repository.GetVoznjaById(id);
        if (voznja is null)
        {
            return NotFound();
        }

        return View(voznja);
    }
}
