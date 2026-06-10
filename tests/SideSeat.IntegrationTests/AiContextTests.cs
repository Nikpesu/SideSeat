using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public class AiContextTests : IClassFixture<SideSeatTestFactory>
{
    private readonly SideSeatTestFactory _factory;

    public AiContextTests(SideSeatTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticatedContext_ContainsOwnDataAndSafeRoutes()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        for (var id = 2; id <= 13; id++)
        {
            db.Voznje.Add(new Voznja
            {
                Id = id,
                VozacId = 1,
                PolazniGradId = 1,
                OdredisniGradId = id % 2 == 0 ? 2 : 3,
                Polazak = DateTime.UtcNow.AddDays(id),
                OcekivaniDolazak = DateTime.UtcNow.AddDays(id).AddHours(3),
                CijenaPoMjestu = 10 + id,
                UkupnoMjesta = 4,
                SlobodnaMjesta = 3,
                Opis = $"AI test vožnja {id}",
                Status = id % 3 == 0 ? StatusVoznje.Zavrsena : StatusVoznje.Planirana
            });
            db.Rezervacije.Add(new Rezervacija
            {
                Id = id,
                VoznjaId = id,
                PutnikId = 2,
                BrojMjesta = 1,
                CijenaUkupno = 10 + id,
                VrijemeRezervacije = DateTime.UtcNow.AddDays(-id),
                Status = StatusRezervacije.Potvrdena,
                Napomena = $"AI rezervacija {id}"
            });
            db.SaldoTransakcije.Add(new SaldoTransakcija
            {
                Id = id + 10,
                KorisnikId = 2,
                Iznos = id,
                Tip = "uplata",
                Komentar = $"AI transakcija {id}",
                SaldoPrije = 50,
                SaldoPoslije = 50 + id,
                Vrijeme = DateTime.UtcNow.AddMinutes(-id)
            });
            db.Obavijesti.Add(new Obavijest
            {
                Id = id + 20,
                KorisnikId = 2,
                Naslov = $"AI obavijest {id}",
                Poruka = $"Sadržaj obavijesti {id}",
                Tip = "Test",
                Link = $"/Voznja/Details/{id}",
                Kreirano = DateTime.UtcNow.AddMinutes(-id),
                Procitano = id % 2 == 0
            });
            db.Placanja.Add(new Placanje
            {
                Id = id,
                RezervacijaId = id,
                Iznos = 10 + id,
                VrijemePlacanja = DateTime.UtcNow.AddMinutes(-id),
                NacinPlacanja = NacinPlacanja.Kartica,
                Uspjesno = true
            });
        }
        await db.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<IAiContextService>();
        var principal = CreatePrincipal("2", "Passenger");

        var context = await service.BuildAsync(
            principal,
            "Detalji rezervacije",
            "/Rezervacija/Details/1",
            CancellationToken.None);

        Assert.Contains("Putnik Test", context);
        Assert.Contains("\"Saldo\": 50", context);
        Assert.Contains("/Rezervacija/Details/1", context);
        using var document = JsonDocument.Parse(context);
        Assert.Equal(13, document.RootElement.GetProperty("ridesAsPassenger").GetArrayLength());
        Assert.Equal(12, document.RootElement.GetProperty("balanceTransactions").GetArrayLength());
        Assert.Equal(12, document.RootElement.GetProperty("notifications").GetArrayLength());
        Assert.Equal(13, document.RootElement.GetProperty("payments").GetArrayLength());
        Assert.Equal(9, document.RootElement.GetProperty("otherVisibleRides").GetArrayLength());
        Assert.Equal(
            13,
            document.RootElement
                .GetProperty("rideSummary")
                .GetProperty("asPassenger")
                .GetProperty("total")
                .GetInt32());
        var routes = document.RootElement.GetProperty("routes")
            .EnumerateArray()
            .Select(route => route.GetProperty("path").GetString())
            .ToList();
        Assert.Contains("/Voznja?view=available&status=all", routes);
        var userProperties = document.RootElement.GetProperty("user")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToList();
        Assert.DoesNotContain("OIB", userProperties);
        Assert.DoesNotContain("JMBG", userProperties);
        Assert.DoesNotContain("LozinkaHash", userProperties);
        Assert.DoesNotContain("LozinkaHash", context);
        Assert.DoesNotContain("KycOib", context);
    }

    [Fact]
    public async Task DriverContext_ContainsOwnedRidesAndTheirReservations()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAiContextService>();
        var principal = CreatePrincipal("1", "Driver");

        var context = await service.BuildAsync(
            principal,
            "Moje vožnje",
            "/Voznja?view=driving&status=all",
            CancellationToken.None);

        using var document = JsonDocument.Parse(context);
        var rides = document.RootElement.GetProperty("ridesAsDriver");
        Assert.Equal(1, rides.GetArrayLength());
        Assert.Equal(1, rides[0].GetProperty("reservations").GetArrayLength());
        Assert.Equal("Putnik Test", rides[0].GetProperty("reservations")[0].GetProperty("passenger").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("balanceTransactions").GetArrayLength());
    }

    [Fact]
    public async Task AnonymousContext_ContainsOnlyPublicNavigation()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAiContextService>();

        var context = await service.BuildAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "Početna",
            "/",
            CancellationToken.None);

        Assert.Contains("\"authentication\": \"anonymous\"", context);
        Assert.Contains("/?auth=login", context);
        Assert.DoesNotContain("Putnik Test", context);
        Assert.DoesNotContain("/Korisnik/Saldo", context);
    }

    private static ClaimsPrincipal CreatePrincipal(string korisnikId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(SideSeatClaimTypes.KorisnikId, korisnikId),
            new(ClaimTypes.NameIdentifier, korisnikId),
            new(ClaimTypes.Name, "putnik@example.com")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
