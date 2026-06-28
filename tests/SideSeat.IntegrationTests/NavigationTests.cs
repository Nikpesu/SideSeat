using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideSeat.Data;

namespace SideSeat.IntegrationTests;

public sealed class NavigationTests : IClassFixture<SideSeatTestFactory>
{
    private readonly SideSeatTestFactory _factory;

    public NavigationTests(SideSeatTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Guest_SeesSidebarAndAuthenticationWithoutPrivateNavigation()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("data-testid=\"sidebar-open\"", html);
        Assert.Contains("data-testid=\"sidebar-home\"", html);
        Assert.DoesNotContain("ss-launch-banner", html);
        Assert.DoesNotContain("AI-NATIVE MOBILITY", html);
        Assert.DoesNotContain("data-testid=\"desktop-primary-navigation\"", html);
        Assert.DoesNotContain("data-testid=\"global-search-trigger\"", html);
        Assert.DoesNotContain("data-testid=\"sidebar-create-ride\"", html);
        Assert.DoesNotContain("data-testid=\"sidebar-admin\"", html);
        Assert.DoesNotContain("data-testid=\"sidebar-logout\"", html);
    }

    [Fact]
    public async Task Passenger_SeesPrimaryNavigationBalanceAndAccountLinks()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreatePassengerClient();

        var html = await client.GetStringAsync("/Rezervacija");

        Assert.Contains("data-testid=\"desktop-primary-navigation\"", html);
        Assert.Contains("data-testid=\"global-search-trigger\"", html);
        Assert.Contains("data-testid=\"nav-rides\"", html);
        Assert.Contains("data-testid=\"nav-reservations\"", html);
        Assert.Contains("data-testid=\"nav-reviews\"", html);
        Assert.Contains("data-testid=\"nav-balance\"", html);
        Assert.Matches(new Regex("50[,.]00 EUR", RegexOptions.IgnoreCase), html);
        Assert.Contains("data-testid=\"account-settings\"", html);
        Assert.Contains("data-testid=\"top-logout\"", html);
        Assert.Contains("data-testid=\"sidebar-logout\"", html);
        Assert.DoesNotContain("data-testid=\"sidebar-create-ride\"", html);
        Assert.DoesNotContain("data-testid=\"sidebar-admin\"", html);
        Assert.Matches(
            new Regex("class=\"ss-nav-link active\"[^>]*data-testid=\"nav-reservations\"", RegexOptions.IgnoreCase),
            html);
    }

    [Fact]
    public async Task Driver_SeesCreateRideButNotAdministration()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateDriverClient();

        var html = await client.GetStringAsync("/Voznja");

        Assert.Contains("data-testid=\"sidebar-create-ride\"", html);
        Assert.DoesNotContain("data-testid=\"sidebar-admin\"", html);
        Assert.Matches(
            new Regex("class=\"ss-nav-link active\"[^>]*data-testid=\"nav-rides\"", RegexOptions.IgnoreCase),
            html);
    }

    [Fact]
    public async Task Admin_SeesCreateRideAndAdministration()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateAdminClient();

        var html = await client.GetStringAsync("/Audit");

        Assert.Contains("data-testid=\"sidebar-create-ride\"", html);
        Assert.Contains("data-testid=\"sidebar-admin\"", html);
        Assert.Contains("data-testid=\"sidebar-admin-users\"", html);
        Assert.Contains("Audit zapis", html);
        Assert.Contains("Novi korisnik", html);
        Assert.Contains("Novi grad", html);
        Assert.Contains("Novo vozilo", html);
        Assert.Contains("Novo plaćanje", html);
    }

    [Fact]
    public async Task HomeSchedule_ShowsResponsiveRideResults()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("data-testid=\"home-schedule\"", html);
        Assert.Contains("data-testid=\"home-schedule-results\"", html);
        Assert.Contains("Zagreb", html);
        Assert.Contains("Split", html);
        Assert.Matches(new Regex("20[,.]00 EUR", RegexOptions.IgnoreCase), html);
        Assert.DoesNotContain("data-testid=\"home-schedule-empty\"", html);
        Assert.Contains("/lib/leaflet/leaflet.css", html);
        Assert.Contains("/js/route-maps.js", html);
        Assert.Contains("data-map-route-url=\"/api/maps/route\"", html);
        Assert.Contains("data-ss-home-route-source", html);
        Assert.Contains("data-route-start-lat=\"45.815010\"", html);
        Assert.Contains("data-ss-home-route-map", html);
        Assert.Contains("data-ss-route-carousel", html);
        Assert.Contains("data-ss-route-carousel-viewport", html);
        Assert.DoesNotContain("ss-holo-board", html);
        Assert.DoesNotContain("LIVE NETWORK", html);
        Assert.True(
            html.IndexOf("data-testid=\"home-schedule-results\"", StringComparison.Ordinal) <
            html.IndexOf("data-ss-home-route-map", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HomeSchedule_LimitsInfiniteCarouselToTwentyRoutes()
    {
        await _factory.SeedAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            for (var id = 2; id <= 25; id++)
            {
                db.Voznje.Add(new SideSeat.Models.Voznja
                {
                    Id = id,
                    VozacId = 1,
                    PolazniGradId = id % 2 == 0 ? 1 : 2,
                    OdredisniGradId = id % 2 == 0 ? 2 : 3,
                    Polazak = DateTime.UtcNow.AddDays(1).AddMinutes(id),
                    OcekivaniDolazak = DateTime.UtcNow.AddDays(1).AddHours(3).AddMinutes(id),
                    CijenaPoMjestu = 10 + id,
                    UkupnoMjesta = 4,
                    SlobodnaMjesta = 4,
                    Opis = $"Carousel ruta {id}",
                    Status = SideSeat.Models.StatusVoznje.Planirana
                });
            }
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        var html = await client.GetStringAsync("/");

        Assert.Equal(20, Regex.Matches(html, @"\bdata-ss-route-carousel-item\b").Count);
        Assert.Contains("20 prikazanih vožnji", html);
    }

    [Fact]
    public async Task HomeSchedule_ShowsEmptyStateForRouteWithoutRides()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/?from=3&to=2");

        Assert.Contains("data-testid=\"home-schedule\"", html);
        Assert.Contains("data-testid=\"home-schedule-empty\"", html);
        Assert.DoesNotContain("data-testid=\"home-schedule-results\"", html);
    }

    [Fact]
    public async Task RideAndReservationPages_RenderRouteMapsAndPreviewData()
    {
        await _factory.SeedAsync();
        using var admin = _factory.CreateAdminClient();

        var rideList = await admin.GetStringAsync("/Voznja?view=all");
        Assert.Contains("data-ss-route-preview-source", rideList);
        Assert.Contains("data-route-end-lat=\"43.508133\"", rideList);
        Assert.Contains("data-ss-keep-table", rideList);

        var reservationList = await admin.GetStringAsync("/Rezervacija?view=all");
        Assert.Contains("data-ss-route-preview-source", reservationList);
        Assert.Contains("data-ss-route-preview-button", reservationList);
        Assert.Contains("data-ss-keep-table", reservationList);

        var reviewList = await admin.GetStringAsync("/Ocjena");
        Assert.DoesNotContain("data-ss-keep-table", reviewList);

        var rideDetails = await admin.GetStringAsync("/Voznja/Details/1");
        Assert.Contains("data-ss-route-map", rideDetails);
        Assert.Contains("data-route-end-lat=\"43.508133\"", rideDetails);

        var reservationDetails = await admin.GetStringAsync("/Rezervacija/Details/1");
        Assert.Contains("data-route-start-lat=\"45.815010\"", reservationDetails);
        Assert.Contains("data-route-end-lng=\"16.440193\"", reservationDetails);
    }

    [Fact]
    public async Task RouteMap_RendersLegacyCoordinateFallback()
    {
        await _factory.SeedAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
            var city = await db.Gradovi.SingleAsync(item => item.Id == 1);
            city.Latitude = null;
            city.Longitude = null;
            await db.SaveChangesAsync();
        }

        using var admin = _factory.CreateAdminClient();
        var html = await admin.GetStringAsync("/Voznja/Details/1");

        Assert.Contains("data-route-start-lat=\"\"", html);
        Assert.Contains("Koordinate rute nisu dostupne.", html);
    }
}
