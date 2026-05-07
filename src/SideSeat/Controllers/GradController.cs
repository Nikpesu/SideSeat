using Microsoft.AspNetCore.Mvc;
using SideSeat.Repositories;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu gradova i detalje pojedinog grada iz mock podataka.
/// </summary>
public class GradController : Controller
{
	private readonly SideSeatEfRepository _repository;

	public GradController(SideSeatEfRepository repository)
	{
		_repository = repository;
	}

	public IActionResult Index()
	{
		return View(_repository.GetGradovi());
	}

	public IActionResult Details(int id)
	{
		var grad = _repository.GetGradById(id);
		if (grad is null)
		{
			return NotFound();
		}

		return View(grad);
	}
}
