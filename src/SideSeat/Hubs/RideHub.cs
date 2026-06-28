using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;

namespace SideSeat.Hubs;

[Authorize]
public sealed class RideHub(SideSeatDbContext dbContext) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Principal.GetKorisnikId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinRide(int voznjaId)
    {
        if (!await CanAccessRideAsync(voznjaId))
        {
            throw new HubException("Nemaš pristup ovoj vožnji.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RideGroup(voznjaId));
    }

    public async Task SendMessage(int voznjaId, string message, int? recipientId = null)
    {
        var userId = Principal.GetKorisnikId();
        if (userId is null || !await CanAccessRideAsync(voznjaId))
        {
            throw new HubException("Nemaš pristup ovoj vožnji.");
        }

        var trimmed = (message ?? string.Empty).Trim();
        if (trimmed.Length is < 1 or > 1000)
        {
            throw new HubException("Poruka nije valjana.");
        }

        if (recipientId.HasValue && !await IsRideParticipantAsync(voznjaId, recipientId.Value))
        {
            throw new HubException("Primatelj nije sudionik vožnje.");
        }

        var entity = new RideChatMessage
        {
            VoznjaId = voznjaId,
            SenderId = userId.Value,
            RecipientId = recipientId,
            Message = trimmed,
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.RideChatMessages.Add(entity);
        await dbContext.SaveChangesAsync();

        string? recipientName = null;
        if (recipientId.HasValue)
        {
            recipientName = await dbContext.Korisnici
                .Where(user => user.Id == recipientId.Value)
                .Select(user => (user.Ime + " " + user.Prezime).Trim())
                .FirstOrDefaultAsync();
        }

        var payload = new
        {
            entity.Id,
            entity.VoznjaId,
            entity.SenderId,
            senderName = Principal.Identity?.Name ?? "Korisnik",
            entity.RecipientId,
            recipientName,
            entity.Message,
            entity.CreatedAtUtc
        };

        if (recipientId.HasValue)
        {
            await Clients.Groups(UserGroup(userId.Value), UserGroup(recipientId.Value))
                .SendAsync("RideMessage", payload);
            return;
        }

        await Clients.Group(RideGroup(voznjaId)).SendAsync("RideMessage", payload);
    }

    public async Task CheckIn(int rezervacijaId, decimal? latitude = null, decimal? longitude = null)
    {
        var userId = Principal.GetKorisnikId();
        if (userId is null)
        {
            throw new HubException("Korisnik nije prijavljen.");
        }

        var reservation = await dbContext.Rezervacije
            .Include(item => item.Voznja)
            .Include(item => item.Putnik)
            .FirstOrDefaultAsync(item => item.Id == rezervacijaId);
        if (reservation is null)
        {
            throw new HubException("Rezervacija ne postoji.");
        }

        if (!Principal.IsInRole("Admin") && reservation.PutnikId != userId.Value)
        {
            throw new HubException("Nemaš pristup ovoj rezervaciji.");
        }

        if (reservation.Status != StatusRezervacije.Potvrdena ||
            reservation.Voznja.Status is not (StatusVoznje.Planirana or StatusVoznje.Aktivna))
        {
            throw new HubException("Check-in nije dostupan za ovu rezervaciju.");
        }

        if (DateTime.Now < reservation.Voznja.Polazak.AddMinutes(-30))
        {
            throw new HubException("Check-in je dostupan 30 minuta prije polaska.");
        }

        ApplyLocation(reservation, latitude, longitude);
        reservation.CheckInAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        await Clients.Group(RideGroup(reservation.VoznjaId)).SendAsync("PassengerReady", ToPassengerReadyPayload(reservation));
        if (reservation.LastLatitude.HasValue && reservation.LastLongitude.HasValue)
        {
            await Clients.Groups(await LocationRecipientGroupsAsync(reservation))
                .SendAsync("PassengerLocation", ToPassengerLocationPayload(reservation));
        }
    }

    public async Task UpdateLocation(int rezervacijaId, decimal latitude, decimal longitude)
    {
        var userId = Principal.GetKorisnikId();
        var reservation = await dbContext.Rezervacije
            .Include(item => item.Voznja)
            .Include(item => item.Putnik)
            .FirstOrDefaultAsync(item => item.Id == rezervacijaId);
        if (userId is null ||
            reservation is null ||
            (!Principal.IsInRole("Admin") && reservation.PutnikId != userId.Value))
        {
            throw new HubException("Nemaš pristup ovoj rezervaciji.");
        }

        if (reservation.CheckInAtUtc is null ||
            reservation.Voznja.Status is StatusVoznje.Zavrsena or StatusVoznje.Otkazana)
        {
            throw new HubException("Lokacija nije dostupna za ovu rezervaciju.");
        }

        ApplyLocation(reservation, latitude, longitude);
        await dbContext.SaveChangesAsync();
        await Clients.Groups(await LocationRecipientGroupsAsync(reservation))
            .SendAsync("PassengerLocation", ToPassengerLocationPayload(reservation));
    }

    private async Task<bool> CanAccessRideAsync(int voznjaId)
    {
        var userId = Principal.GetKorisnikId();
        if (userId is null)
        {
            return false;
        }

        if (Principal.IsInRole("Admin"))
        {
            return await dbContext.Voznje.AnyAsync(item => item.Id == voznjaId);
        }

        return await dbContext.Voznje.AnyAsync(item =>
            item.Id == voznjaId &&
            (item.VozacId == userId.Value ||
             item.Rezervacije.Any(reservation =>
                 reservation.PutnikId == userId.Value &&
                 reservation.Status == StatusRezervacije.Potvrdena)));
    }

    private async Task<bool> IsRideParticipantAsync(int voznjaId, int userId) =>
        await dbContext.Voznje.AnyAsync(item =>
            item.Id == voznjaId &&
            (item.VozacId == userId ||
             item.Rezervacije.Any(reservation =>
                 reservation.PutnikId == userId &&
                 reservation.Status == StatusRezervacije.Potvrdena)));

    private static void ApplyLocation(Rezervacija reservation, decimal? latitude, decimal? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return;
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new HubException("Lokacija nije valjana.");
        }

        reservation.LastLatitude = latitude;
        reservation.LastLongitude = longitude;
        reservation.LastLocationAtUtc = DateTime.UtcNow;
    }

    private async Task<IReadOnlyList<string>> LocationRecipientGroupsAsync(Rezervacija reservation)
    {
        var adminIds = await dbContext.Korisnici
            .AsNoTracking()
            .Where(user => user.Tip == TipKorisnika.Admin)
            .Select(user => user.Id)
            .ToListAsync();
        return adminIds
            .Append(reservation.Voznja.VozacId)
            .Append(reservation.PutnikId)
            .Distinct()
            .Select(UserGroup)
            .ToList();
    }

    private static object ToPassengerReadyPayload(Rezervacija reservation) => new
    {
        reservation.Id,
        reservation.VoznjaId,
        reservation.PutnikId,
        passengerName = $"{reservation.Putnik.Ime} {reservation.Putnik.Prezime}".Trim(),
        reservation.CheckInAtUtc
    };

    private static object ToPassengerLocationPayload(Rezervacija reservation) => new
    {
        reservation.Id,
        reservation.VoznjaId,
        reservation.PutnikId,
        passengerName = $"{reservation.Putnik.Ime} {reservation.Putnik.Prezime}".Trim(),
        reservation.CheckInAtUtc,
        reservation.LastLatitude,
        reservation.LastLongitude,
        reservation.LastLocationAtUtc
    };

    private static string RideGroup(int voznjaId) => $"ride:{voznjaId}";

    private static string UserGroup(int userId) => $"user:{userId}";

    private System.Security.Claims.ClaimsPrincipal Principal =>
        Context.User ?? new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
}
