using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Repositories;
using SideSeat.Models.ViewModels;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu gradova i detalje pojedinog grada iz mock podataka.
/// </summary>
[Authorize(Roles = "Admin")]
public class GradController : Controller
{
	private readonly SideSeatEfRepository _repository;
	private readonly SideSeatDbContext _db;

	public GradController(SideSeatEfRepository repository, SideSeatDbContext db)
	{
		_repository = repository;
		_db = db;
	}

	public IActionResult Index(string? search, int? pageSize)
	{
		var gradovi = _repository.GetGradovi();
		if (!string.IsNullOrWhiteSpace(search))
		{
			var normalizedSearch = search.Trim();
			gradovi = gradovi.Where(grad =>
				grad.Naziv.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
				grad.Drzava.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
				grad.PostanskiBroj.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
				grad.Id.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
		}

		ViewBag.Search = search;
		ViewBag.PageSize = pageSize;

		if (pageSize.HasValue)
		{
			var normalized = PageSizeOptions.Normalize(pageSize.Value);
			if (normalized > 0)
			{
				gradovi = gradovi.Take(normalized).ToList();
			}
		}

		return View(gradovi);
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

	public IActionResult Create()
	{
		return View(new Grad());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(Grad model)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		_db.Gradovi.Add(model);
		await _db.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}

	public IActionResult Edit(int id)
	{
		var grad = _db.Gradovi.AsNoTracking().FirstOrDefault(g => g.Id == id);
		if (grad is null)
		{
			return NotFound();
		}

		return View(grad);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, Grad model)
	{
		if (id != model.Id)
		{
			return BadRequest();
		}

		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var grad = await _db.Gradovi.FirstOrDefaultAsync(g => g.Id == id);
		if (grad is null)
		{
			return NotFound();
		}

		if (!await TryUpdateModelAsync(grad, string.Empty, g => g.Naziv, g => g.Drzava, g => g.PostanskiBroj))
		{
			return View(model);
		}

		await _db.SaveChangesAsync();
		return RedirectToAction(nameof(Details), new { id = grad.Id });
	}

	public IActionResult Delete(int id)
	{
		var grad = _repository.GetGradById(id);
		if (grad is null)
		{
			return NotFound();
		}

		return View(grad);
	}

	[HttpPost, ActionName("Delete")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteConfirmed(int id)
	{
		var grad = await _db.Gradovi.FirstOrDefaultAsync(g => g.Id == id);
		if (grad is null)
		{
			return NotFound();
		}

		var hasRelatedTrips = await _db.Voznje.AnyAsync(v => v.PolazniGradId == id || v.OdredisniGradId == id);
		if (hasRelatedTrips)
		{
			ModelState.AddModelError(string.Empty, "Grad nije moguce obrisati dok postoje povezane voznje.");
			return View("Delete", grad);
		}

		_db.Gradovi.Remove(grad);
		await _db.SaveChangesAsync();

		return RedirectToAction(nameof(Index));
	}
}
