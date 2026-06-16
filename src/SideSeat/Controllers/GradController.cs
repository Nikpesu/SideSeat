using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Repositories;
using SideSeat.Models.ViewModels;
using SideSeat.Services;

namespace SideSeat.Controllers;

/// <summary>
/// Prikazuje listu gradova i detalje pojedinog grada iz mock podataka.
/// </summary>
[Authorize(Roles = "Admin")]
public class GradController : Controller
{
	private readonly SideSeatEfRepository _repository;
	private readonly SideSeatDbContext _db;
	private readonly ICityGeocodingService _cityGeocoding;

	public GradController(
		SideSeatEfRepository repository,
		SideSeatDbContext db,
		ICityGeocodingService cityGeocoding)
	{
		_repository = repository;
		_db = db;
		_cityGeocoding = cityGeocoding;
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
	public async Task<IActionResult> Create(Grad model, CancellationToken cancellationToken)
	{
		if (!ModelState.IsValid)
		{
			return View(model);
		}

		model.Naziv = model.Naziv.Trim();
		model.Drzava = model.Drzava.Trim();
		model.PostanskiBroj = model.PostanskiBroj.Trim();
		var coordinates = await _cityGeocoding.ResolveAsync(
			model.Naziv,
			model.Drzava,
			model.PostanskiBroj,
			model.Latitude,
			model.Longitude,
			cancellationToken);
		if (!coordinates.Succeeded)
		{
			ModelState.AddModelError(string.Empty, coordinates.Error ?? "Koordinate grada nisu dostupne.");
			return View(model);
		}

		model.Latitude = coordinates.Latitude;
		model.Longitude = coordinates.Longitude;
		_db.Gradovi.Add(model);
		await _db.SaveChangesAsync(cancellationToken);

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
	public async Task<IActionResult> Edit(int id, Grad model, CancellationToken cancellationToken)
	{
		if (id != model.Id)
		{
			return BadRequest();
		}

		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var grad = await _db.Gradovi.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
		if (grad is null)
		{
			return NotFound();
		}

		var name = model.Naziv.Trim();
		var country = model.Drzava.Trim();
		var postalCode = model.PostanskiBroj.Trim();
		var identityChanged =
			!string.Equals(grad.Naziv, name, StringComparison.Ordinal) ||
			!string.Equals(grad.Drzava, country, StringComparison.Ordinal) ||
			!string.Equals(grad.PostanskiBroj, postalCode, StringComparison.Ordinal);
		var coordinatesChanged =
			grad.Latitude != model.Latitude ||
			grad.Longitude != model.Longitude;
		var bothCoordinatesOmitted = !model.Latitude.HasValue && !model.Longitude.HasValue;
		var latitude = !identityChanged && bothCoordinatesOmitted
			? grad.Latitude
			: identityChanged && !coordinatesChanged
				? null
				: model.Latitude;
		var longitude = !identityChanged && bothCoordinatesOmitted
			? grad.Longitude
			: identityChanged && !coordinatesChanged
				? null
				: model.Longitude;
		var coordinates = await _cityGeocoding.ResolveAsync(
			name,
			country,
			postalCode,
			latitude,
			longitude,
			cancellationToken);
		if (!coordinates.Succeeded)
		{
			ModelState.AddModelError(string.Empty, coordinates.Error ?? "Koordinate grada nisu dostupne.");
			return View(model);
		}

		grad.Naziv = name;
		grad.Drzava = country;
		grad.PostanskiBroj = postalCode;
		grad.Latitude = coordinates.Latitude;
		grad.Longitude = coordinates.Longitude;
		await _db.SaveChangesAsync(cancellationToken);
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
