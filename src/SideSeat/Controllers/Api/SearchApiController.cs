using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;

namespace SideSeat.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/search")]
public sealed class SearchApiController(SideSeatDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(string? q, CancellationToken cancellationToken)
    {
        var term = q?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Ok(Array.Empty<object>());
        }

        term = term.Length > 100 ? term[..100] : term;
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");
        var isDriver = User.IsInRole("Driver") || isAdmin;
        var results = BuildPageResults(term, isAdmin, isDriver);

        var ridesQuery = dbContext.Voznje
            .AsNoTracking()
            .Where(ride =>
                isAdmin ||
                ride.Status == StatusVoznje.Planirana ||
                ride.VozacId == userId.Value ||
                ride.Rezervacije.Any(reservation => reservation.PutnikId == userId.Value))
            .Where(ride =>
                ride.Opis.Contains(term) ||
                ride.PolazniGrad.Naziv.Contains(term) ||
                ride.OdredisniGrad.Naziv.Contains(term));
        results.AddRange(await ridesQuery
            .OrderBy(ride => ride.Polazak)
            .Take(8)
            .Select(ride => new SearchResult(
                "Vožnje",
                $"{ride.PolazniGrad.Naziv} → {ride.OdredisniGrad.Naziv}",
                $"Polazak {ride.Polazak:dd.MM.yyyy HH:mm}",
                $"/Voznja/Details/{ride.Id}"))
            .ToListAsync(cancellationToken));

        var reservations = await dbContext.Rezervacije
            .AsNoTracking()
            .Where(reservation =>
                isAdmin ||
                reservation.PutnikId == userId.Value ||
                reservation.Voznja.VozacId == userId.Value)
            .Where(reservation =>
                reservation.Napomena.Contains(term) ||
                reservation.Voznja.PolazniGrad.Naziv.Contains(term) ||
                reservation.Voznja.OdredisniGrad.Naziv.Contains(term))
            .OrderByDescending(reservation => reservation.VrijemeRezervacije)
            .Take(8)
            .Select(reservation => new SearchResult(
                "Rezervacije",
                $"Rezervacija #{reservation.Id}",
                $"{reservation.Voznja.PolazniGrad.Naziv} → {reservation.Voznja.OdredisniGrad.Naziv}",
                $"/Rezervacija/Details/{reservation.Id}"))
            .ToListAsync(cancellationToken);
        results.AddRange(reservations);

        if (isAdmin)
        {
            results.AddRange(await dbContext.Gradovi
                .AsNoTracking()
                .Where(city =>
                    city.Naziv.Contains(term) ||
                    city.Drzava.Contains(term) ||
                    city.PostanskiBroj.Contains(term))
                .OrderBy(city => city.Naziv)
                .Take(6)
                .Select(city => new SearchResult(
                    "Gradovi",
                    city.Naziv,
                    $"{city.Drzava}, {city.PostanskiBroj}",
                    $"/Grad/Details/{city.Id}"))
                .ToListAsync(cancellationToken));

            results.AddRange(await dbContext.Korisnici
                .AsNoTracking()
                .Where(user =>
                    user.Ime.Contains(term) ||
                    user.Prezime.Contains(term) ||
                    user.Email.Contains(term))
                .OrderBy(user => user.Prezime)
                .Take(6)
                .Select(user => new SearchResult(
                    "Korisnici",
                    user.Ime + " " + user.Prezime,
                    user.Email,
                    $"/Korisnik/Details/{user.Id}"))
                .ToListAsync(cancellationToken));

            results.AddRange(await dbContext.Vozila
                .AsNoTracking()
                .Where(vehicle =>
                    vehicle.Marka.Contains(term) ||
                    vehicle.Model.Contains(term) ||
                    vehicle.Registracija.Contains(term))
                .OrderBy(vehicle => vehicle.Marka)
                .Take(6)
                .Select(vehicle => new SearchResult(
                    "Vozila",
                    vehicle.Marka + " " + vehicle.Model,
                    vehicle.Registracija,
                    $"/Vozilo/Details/{vehicle.Id}"))
                .ToListAsync(cancellationToken));
        }

        return Ok(results.Take(30));
    }

    private static List<SearchResult> BuildPageResults(string term, bool admin, bool driver)
    {
        var pages = new List<SearchResult>
        {
            new("Stranice", "Početna", "Pretraga dostupnih vožnji", "/"),
            new("Stranice", "Vožnje", "Dostupne i vlastite vožnje", "/Voznja"),
            new("Stranice", "Rezervacije", "Pregled rezervacija", "/Rezervacija"),
            new("Stranice", "Ocjene", "Ocjene i recenzije", "/Ocjena"),
            new("Stranice", "Saldo", "Saldo i transakcije", "/Korisnik/Saldo"),
            new("Stranice", "Postavke", "Postavke profila", "/Korisnik/Settings")
        };
        if (driver)
        {
            pages.Add(new("Stranice", "Nova vožnja", "Objavi novu vožnju", "/Voznja/Create"));
        }

        if (admin)
        {
            pages.AddRange([
                new("Stranice", "Gradovi", "Administracija gradova", "/Grad"),
                new("Stranice", "Korisnici", "Administracija korisnika", "/Korisnik"),
                new("Stranice", "Vozila", "Administracija vozila", "/Vozilo"),
                new("Stranice", "Plaćanja", "Administracija plaćanja", "/Placanje"),
                new("Stranice", "Audit", "Sigurnosni i poslovni zapis", "/Audit")
            ]);
        }

        return pages
            .Where(page =>
                page.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                page.Subtitle.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private sealed record SearchResult(string Group, string Title, string Subtitle, string Url);
}
