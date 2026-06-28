using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Commands;
using SideSeat.Security;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public sealed class RideWorkflowFeatureTests : IClassFixture<SideSeatTestFactory>
{
    private readonly SideSeatTestFactory _factory;

    public RideWorkflowFeatureTests(SideSeatTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PendingActionDescriptor_IncludesFilledFormAndReviewUrl()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var pending = scope.ServiceProvider.GetRequiredService<IPendingActionService>();
        var principal = CreatePrincipal(1, "Admin", "Driver", "Passenger");

        var action = pending.Create(
            principal,
            SideSeatActionTypes.CreateRide,
            "Kreiranje vožnje",
            "Test forma",
            new CreateRideCommand(
                1,
                1,
                2,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(3).AddHours(3),
                20,
                4,
                4,
                "AI forma"));

        Assert.NotNull(action.Form);
        Assert.Contains(action.Token, action.Form!.ReviewUrl);
        Assert.Contains(
            action.Form.Sections.SelectMany(section => section.Fields),
            field => field.Name == nameof(CreateRideCommand.PolazniGradId) && field.Value == "1");
    }

    [Fact]
    public async Task DriverSpacingRule_RejectsRideWithinTwelveHours()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var commands = scope.ServiceProvider.GetRequiredService<ISideSeatCommandService>();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var existingStart = await db.Voznje
            .Where(ride => ride.Id == 1)
            .Select(ride => ride.Polazak)
            .SingleAsync();

        var result = await commands.ExecuteAsync(
            SideSeatActionTypes.CreateRide,
            new CreateRideCommand(
                1,
                1,
                3,
                existingStart.AddHours(6),
                existingStart.AddHours(9),
                12,
                4,
                4,
                "Preblizu postojeće vožnje"),
            CreatePrincipal(1, "Admin", "Driver", "Passenger"),
            "Test",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(CommandErrorKind.BusinessRule, result.ErrorKind);
        Assert.Contains("12 sati", result.Message);
    }

    [Fact]
    public async Task CashReservationFinish_DoesNotMoveSaldo()
    {
        await _factory.SeedAsync();
        using var scope = _factory.Services.CreateScope();
        var commands = scope.ServiceProvider.GetRequiredService<ISideSeatCommandService>();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();
        var passenger = await db.Korisnici.SingleAsync(user => user.Id == 2);
        var driver = await db.Korisnici.SingleAsync(user => user.Id == 1);
        passenger.Saldo = 0;
        driver.Saldo = 100;
        db.Voznje.Add(new Voznja
        {
            Id = 20,
            VozacId = 1,
            PolazniGradId = 1,
            OdredisniGradId = 3,
            Polazak = DateTime.UtcNow.AddDays(4),
            OcekivaniDolazak = DateTime.UtcNow.AddDays(4).AddHours(3),
            CijenaPoMjestu = 20,
            UkupnoMjesta = 4,
            SlobodnaMjesta = 4,
            Opis = "Cash test",
            Status = StatusVoznje.Planirana
        });
        await db.SaveChangesAsync();

        var reservationResult = await commands.ExecuteAsync(
            SideSeatActionTypes.CreateReservation,
            new CreateReservationCommand(20, 1, "Gotovina", NacinPlacanja.Gotovina),
            CreatePrincipal(2, "Passenger"),
            "Test",
            CancellationToken.None);
        Assert.True(reservationResult.Succeeded, reservationResult.Message);

        var reservation = await db.Rezervacije.SingleAsync(item => item.Id == reservationResult.EntityId);
        reservation.Status = StatusRezervacije.Potvrdena;
        reservation.Voznja.SlobodnaMjesta -= reservation.BrojMjesta;
        await db.SaveChangesAsync();

        var finishResult = await commands.ExecuteAsync(
            SideSeatActionTypes.FinishRide,
            new FinishRideCommand(20, CashCollected: true),
            CreatePrincipal(1, "Admin", "Driver", "Passenger"),
            "Test",
            CancellationToken.None);

        Assert.True(finishResult.Succeeded, finishResult.Message);
        Assert.Equal(0, passenger.Saldo);
        Assert.Equal(100, driver.Saldo);
        Assert.Equal(StatusRezervacije.Zavrsena, reservation.Status);
        Assert.NotNull(reservation.CashCollectedAtUtc);
        Assert.True(await db.Placanja.AnyAsync(payment =>
            payment.RezervacijaId == reservation.Id &&
            payment.NacinPlacanja == NacinPlacanja.Gotovina &&
            payment.Iznos == 20 &&
            payment.Uspjesno));
    }

    private static ClaimsPrincipal CreatePrincipal(int korisnikId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(SideSeatClaimTypes.KorisnikId, korisnikId.ToString()),
            new(ClaimTypes.NameIdentifier, korisnikId.ToString()),
            new(ClaimTypes.Name, $"user{korisnikId}@example.com")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
