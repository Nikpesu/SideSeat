using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public class AiToolServiceTests : IClassFixture<SideSeatTestFactory>
{
    private readonly SideSeatTestFactory _factory;

    public AiToolServiceTests(SideSeatTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Tools_ReturnOnlyCurrentUsersAuthorizedLiveData()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        db.SaldoTransakcije.Add(new SaldoTransakcija
        {
            Id = 2,
            KorisnikId = 2,
            Iznos = 25,
            Tip = "uplata",
            Komentar = "AI tool test",
            SaldoPrije = 25,
            SaldoPoslije = 50,
            Vrijeme = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var tools = scope.ServiceProvider.GetRequiredService<IAiToolService>();
        var principal = CreatePrincipal("2", "Passenger");

        var userJson = await tools.ExecuteAsync(
            "get_current_user",
            "{}",
            principal,
            CancellationToken.None);
        var ridesJson = await tools.ExecuteAsync(
            "get_rides",
            """{"scope":"passenger","status":"all"}""",
            principal,
            CancellationToken.None);
        var reservationsJson = await tools.ExecuteAsync(
            "get_reservations",
            """{"scope":"mine","status":"all"}""",
            principal,
            CancellationToken.None);
        var balanceJson = await tools.ExecuteAsync(
            "get_balance",
            """{"transactionLimit":10}""",
            principal,
            CancellationToken.None);
        var unauthorizedAllRidesJson = await tools.ExecuteAsync(
            "get_rides",
            """{"scope":"all","status":"all"}""",
            principal,
            CancellationToken.None);

        Assert.Contains("Putnik Test", userJson);
        Assert.DoesNotContain("OIB", userJson);
        Assert.DoesNotContain("JMBG", userJson);
        Assert.DoesNotContain("LozinkaHash", userJson);

        using var rides = JsonDocument.Parse(ridesJson);
        Assert.Equal(1, rides.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(1, rides.RootElement.GetProperty("rides")[0].GetProperty("id").GetInt32());

        using var reservations = JsonDocument.Parse(reservationsJson);
        Assert.Equal(1, reservations.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(
            1,
            reservations.RootElement.GetProperty("reservations")[0].GetProperty("id").GetInt32());

        using var balance = JsonDocument.Parse(balanceJson);
        Assert.Equal(50, balance.RootElement.GetProperty("balance").GetDecimal());
        Assert.Equal(1, balance.RootElement.GetProperty("transactionCount").GetInt32());

        using var unauthorizedAllRides = JsonDocument.Parse(unauthorizedAllRidesJson);
        Assert.Equal(0, unauthorizedAllRides.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Tools_ExposeAndExecutePublicWebSearch()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var tools = scope.ServiceProvider.GetRequiredService<IAiToolService>();
        var principal = CreatePrincipal("2", "Passenger");

        var definitionsJson = JsonSerializer.Serialize(tools.Definitions);
        Assert.Contains("search_public_web", definitionsJson);

        var resultJson = await tools.ExecuteAsync(
            "search_public_web",
            """{"query":"OpenStreetMap","source":"wikipedia","language":"hr","limit":3}""",
            principal,
            CancellationToken.None);

        using var result = JsonDocument.Parse(resultJson);
        Assert.Equal("OpenStreetMap", result.RootElement.GetProperty("query").GetString());
        Assert.Equal(1, result.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(
            "https://example.test/sideseat",
            result.RootElement.GetProperty("results")[0].GetProperty("url").GetString());
        Assert.Contains(
            "[SideSeat test rezultat](https://example.test/sideseat)",
            resultJson);
    }

    [Fact]
    public async Task Tools_RespectRoleBasedAccessAndCityLookup()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var tools = scope.ServiceProvider.GetRequiredService<IAiToolService>();

        var passenger = CreatePrincipal("2", "Passenger");
        var admin = CreatePrincipal("1", "Admin");

        var passengerTools = JsonSerializer.Serialize(tools.GetDefinitions(passenger));
        var adminTools = JsonSerializer.Serialize(tools.GetDefinitions(admin));

        // Alat za sve je vidljiv putniku, ali admin/vozač alati nisu.
        Assert.Contains("get_cities", passengerTools);
        Assert.DoesNotContain("get_users", passengerTools);
        Assert.DoesNotContain("prepare_create_city", passengerTools);
        Assert.DoesNotContain("prepare_create_ride", passengerTools);

        // Administrator vidi sve te alate.
        Assert.Contains("get_users", adminTools);
        Assert.Contains("prepare_create_city", adminTools);
        Assert.Contains("prepare_create_ride", adminTools);

        // Izvršna provjera role: putnik ne može dohvatiti korisnike.
        var deniedJson = await tools.ExecuteAsync("get_users", "{}", passenger, CancellationToken.None);
        Assert.Contains("administrator", deniedJson);

        // Lookup gradova radi za putnika.
        var citiesJson = await tools.ExecuteAsync(
            "get_cities",
            """{"search":"Zagreb"}""",
            passenger,
            CancellationToken.None);
        using var cities = JsonDocument.Parse(citiesJson);
        Assert.True(cities.RootElement.GetProperty("count").GetInt32() >= 1);
        Assert.Contains("Zagreb", citiesJson);
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
