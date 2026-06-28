using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;

namespace SideSeat.Services;

/// <summary>
/// Pozadinski servis koji putniku stvori obavijest (zvonce) ~30 minuta prije polaska,
/// dok god rezervacija nije check-irana. Svaka rezervacija dobije obavijest najviše jednom.
/// </summary>
public sealed class CheckInReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<CheckInReminderService> logger) : BackgroundService
{
    private const string ReminderTip = "checkin-reminder";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kratka odgoda na startu da se aplikacija (i migracije) stignu podići.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Check-in podsjetnik nije uspio u ovom ciklusu.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideSeatDbContext>();

        var now = DateTime.Now;
        var windowEnd = now.AddMinutes(30);

        var due = await db.Rezervacije
            .Include(r => r.Voznja).ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja).ThenInclude(v => v.OdredisniGrad)
            .Where(r =>
                r.Status == StatusRezervacije.Potvrdena &&
                r.CheckInAtUtc == null &&
                (r.Voznja.Status == StatusVoznje.Planirana || r.Voznja.Status == StatusVoznje.Aktivna) &&
                r.Voznja.Polazak >= now &&
                r.Voznja.Polazak <= windowEnd)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return;
        }

        var added = false;
        foreach (var reservation in due)
        {
            var marker = $"(rez:{reservation.Id})";
            var alreadyNotified = await db.Obavijesti.AnyAsync(o =>
                o.KorisnikId == reservation.PutnikId &&
                o.Tip == ReminderTip &&
                o.Poruka.Contains(marker), cancellationToken);
            if (alreadyNotified)
            {
                continue;
            }

            var ruta = $"{reservation.Voznja.PolazniGrad?.Naziv} → {reservation.Voznja.OdredisniGrad?.Naziv}";
            db.Obavijesti.Add(new Obavijest
            {
                KorisnikId = reservation.PutnikId,
                Naslov = "Vrijeme za check-in",
                Poruka = $"Vožnja {ruta} kreće za manje od 30 min. Otvori Trenutnu vožnju i pošalji lokaciju. {marker}",
                Tip = ReminderTip,
                Link = "/Voznja/Current",
                Kreirano = DateTime.UtcNow,
                Procitano = false
            });
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
