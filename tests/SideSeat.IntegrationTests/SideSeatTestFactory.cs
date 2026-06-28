using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SideSeat;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public class SideSeatTestFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _databaseName = $"SideSeatTests-{Guid.NewGuid():N}";
    public string WebRootPath { get; } = Path.Combine(Path.GetTempPath(), $"SideSeatWebRoot-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(WebRootPath);

        builder.UseEnvironment("Testing");
        builder.UseSetting(WebHostDefaults.WebRootKey, WebRootPath);
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SideSeatDbContext"] = "TestConnection",
                ["Authentication:Google:ClientId"] = "test-client-id",
                ["Authentication:Google:ClientSecret"] = "test-client-secret",
                ["Mcp:ApiKey"] = "test-mcp-key",
                ["Mcp:KorisnikId"] = "1",
                ["Mcp:Roles:0"] = "Admin",
                ["Mcp:Roles:1"] = "Driver",
                ["Mcp:Roles:2"] = "Passenger"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SideSeatDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<SideSeatDbContext>>();
            services.RemoveAll<IDatabaseProvider>();
            services.RemoveAll<ICityGeocodingService>();
            services.RemoveAll<IRouteGeometryService>();
            services.RemoveAll<IPublicWebSearchService>();
            services.AddDbContext<SideSeatDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<ICityGeocodingService, TestCityGeocodingService>();
            services.AddSingleton<IRouteGeometryService, TestRouteGeometryService>();
            services.AddSingleton<IPublicWebSearchService, TestPublicWebSearchService>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAdminClient(bool allowAutoRedirect = true)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect
        });
        client.DefaultRequestHeaders.Add("X-Test-Auth", "admin");
        return client;
    }

    public HttpClient CreatePassengerClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "passenger");
        return client;
    }

    public HttpClient CreateDriverClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "driver");
        return client;
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        db.Gradovi.AddRange(
            new Grad
            {
                Id = 1, Naziv = "Zagreb", Drzava = "Hrvatska", PostanskiBroj = "10000",
                Latitude = 45.815010m, Longitude = 15.981919m
            },
            new Grad
            {
                Id = 2, Naziv = "Split", Drzava = "Hrvatska", PostanskiBroj = "21000",
                Latitude = 43.508133m, Longitude = 16.440193m
            },
            new Grad
            {
                Id = 3, Naziv = "Rijeka", Drzava = "Hrvatska", PostanskiBroj = "51000",
                Latitude = 45.327063m, Longitude = 14.442176m
            });

        db.Korisnici.AddRange(
            new Korisnik
            {
                Id = 1, Ime = "Admin", Prezime = "User", Email = "admin@example.com",
                Adresa = "Admin adresa", BrojMobitela = "0910000001", DatumRegistracije = DateTime.UtcNow,
                Tip = TipKorisnika.Admin, JeAktivan = true, LozinkaHash = string.Empty, Saldo = 100
            },
            new Korisnik
            {
                Id = 2, Ime = "Putnik", Prezime = "Test", Email = "putnik@example.com",
                Adresa = "Putnicka adresa", BrojMobitela = "0910000002", DatumRegistracije = DateTime.UtcNow,
                Tip = TipKorisnika.Putnik, JeAktivan = true, LozinkaHash = string.Empty, Saldo = 50
            });

        db.Vozila.Add(new Vozilo
        {
            Id = 1, Marka = "Skoda", Model = "Octavia", Registracija = "ZG-TEST",
            GodinaProizvodnje = 2020, BrojSjedala = 5, Boja = "Siva", ProsjecnaPotrosnja = 5.4m
        });

        db.Voznje.Add(new Voznja
        {
            Id = 1, VozacId = 1, PolazniGradId = 1, OdredisniGradId = 2,
            Polazak = DateTime.UtcNow.AddDays(1), OcekivaniDolazak = DateTime.UtcNow.AddDays(1).AddHours(4),
            CijenaPoMjestu = 20, UkupnoMjesta = 4, SlobodnaMjesta = 4, Opis = "Seed voznja", Status = StatusVoznje.Planirana
        });

        db.Rezervacije.Add(new Rezervacija
        {
            Id = 1, VoznjaId = 1, PutnikId = 2, BrojMjesta = 1, CijenaUkupno = 20,
            VrijemeRezervacije = DateTime.UtcNow, Status = StatusRezervacije.UProcesuPotvrde, Napomena = "Seed rezervacija"
        });

        db.Placanja.Add(new Placanje
        {
            Id = 1, RezervacijaId = 1, Iznos = 20, VrijemePlacanja = DateTime.UtcNow,
            NacinPlacanja = NacinPlacanja.Kartica, Uspjesno = true
        });

        db.Ocjene.Add(new OcjenaVoznje
        {
            Id = 1, RezervacijaId = 1, AutorId = 2, BrojZvjezdica = 5,
            Komentar = "Seed ocjena", Kreirano = DateTime.UtcNow
        });

        db.SaldoTransakcije.Add(new SaldoTransakcija
        {
            Id = 1, KorisnikId = 1, Iznos = 10, Tip = "uplata",
            SaldoPrije = 90, SaldoPoslije = 100, Vrijeme = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    public new void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(WebRootPath))
        {
            Directory.Delete(WebRootPath, recursive: true);
        }
    }
}

internal sealed class TestRouteGeometryService : IRouteGeometryService
{
    public Task<RouteGeometryResult?> GetRouteAsync(
        decimal startLatitude,
        decimal startLongitude,
        decimal endLatitude,
        decimal endLongitude,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<RouteGeometryResult?>(new RouteGeometryResult(
            [
                new((double)startLatitude, (double)startLongitude),
                new(
                    (double)((startLatitude + endLatitude) / 2),
                    (double)((startLongitude + endLongitude) / 2)),
                new((double)endLatitude, (double)endLongitude)
            ],
            410000,
            14400));
}

internal sealed class TestCityGeocodingService : ICityGeocodingService
{
    public Task<CityCoordinateResult> ResolveAsync(
        string name,
        string country,
        string postalCode,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken = default)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            return Task.FromResult(CityCoordinateResult.Failure(
                "Potrebno je unijeti obje koordinate ili obje ostaviti praznima."));
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            return Task.FromResult(
                latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180
                    ? CityCoordinateResult.Success(latitude.Value, longitude.Value)
                    : CityCoordinateResult.Failure("Koordinate nisu u dopuštenom rasponu."));
        }

        return Task.FromResult(
            name.Contains("Bez Lokacije", StringComparison.OrdinalIgnoreCase)
                ? CityCoordinateResult.Failure("Lokacija grada nije pronađena. Unesite koordinate ručno.")
                : CityCoordinateResult.Success(45.123456m, 15.654321m));
    }
}

internal sealed class TestPublicWebSearchService : IPublicWebSearchService
{
    public Task<PublicWebSearchResponse> SearchAsync(
        PublicWebSearchRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PublicWebSearchResponse(
            request.Query,
            string.IsNullOrWhiteSpace(request.Source) ? "auto" : request.Source,
            string.IsNullOrWhiteSpace(request.Language) ? "hr" : request.Language,
            DateTimeOffset.UtcNow,
            [
                new PublicWebSearchResult(
                    "SideSeat test rezultat",
                    $"Sažetak javne pretrage za {request.Query}.",
                    "https://example.test/sideseat",
                    "Test")
            ]));
}
