using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;

namespace SideSeat.Services;

public sealed class AiToolService(SideSeatDbContext dbContext) : IAiToolService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public IReadOnlyList<object> Definitions { get; } =
    [
        Tool(
            "get_current_user",
            "Dohvaća sigurne, aktualne podatke trenutno prijavljenog korisnika.",
            new
            {
                type = "object",
                properties = new { },
                additionalProperties = false
            }),
        Tool(
            "get_rides",
            "Dohvaća aktualne vožnje koje korisnik smije vidjeti.",
            new
            {
                type = "object",
                properties = new
                {
                    scope = new
                    {
                        type = "string",
                        @enum = new[] { "available", "passenger", "driver", "all" },
                        description = "available=dostupne, passenger=moja voženja, driver=moje objavljene, all=sve za admina"
                    },
                    status = new
                    {
                        type = "string",
                        @enum = new[] { "all", "planned", "completed", "cancelled" }
                    },
                    id = new { type = "integer" },
                    search = new { type = "string" }
                },
                required = new[] { "scope" },
                additionalProperties = false
            }),
        Tool(
            "get_reservations",
            "Dohvaća aktualne rezervacije koje korisnik smije vidjeti.",
            new
            {
                type = "object",
                properties = new
                {
                    scope = new
                    {
                        type = "string",
                        @enum = new[] { "mine", "my_rides", "all" }
                    },
                    status = new
                    {
                        type = "string",
                        @enum = new[] { "all", "pending", "confirmed", "rejected", "completed" }
                    },
                    id = new { type = "integer" }
                },
                required = new[] { "scope" },
                additionalProperties = false
            }),
        Tool(
            "get_balance",
            "Dohvaća aktualni saldo, rezervirana sredstva i transakcije prijavljenog korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    transactionLimit = new
                    {
                        type = "integer",
                        minimum = 1,
                        maximum = 100
                    }
                },
                additionalProperties = false
            })
    ];

    public async Task<string> ExecuteAsync(
        string toolName,
        string argumentsJson,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var korisnikId = principal.GetKorisnikId();
        if (korisnikId is null)
        {
            return SerializeError("Korisnik nije prijavljen.");
        }

        using var arguments = ParseArguments(argumentsJson);
        return toolName switch
        {
            "get_current_user" => await GetCurrentUserAsync(
                korisnikId.Value,
                principal,
                cancellationToken),
            "get_rides" => await GetRidesAsync(
                korisnikId.Value,
                principal,
                arguments.RootElement,
                cancellationToken),
            "get_reservations" => await GetReservationsAsync(
                korisnikId.Value,
                principal,
                arguments.RootElement,
                cancellationToken),
            "get_balance" => await GetBalanceAsync(
                korisnikId.Value,
                arguments.RootElement,
                cancellationToken),
            _ => SerializeError($"Nepoznat alat: {toolName}.")
        };
    }

    private async Task<string> GetCurrentUserAsync(
        int korisnikId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Korisnici
            .AsNoTracking()
            .Where(item => item.Id == korisnikId)
            .Select(item => new
            {
                item.Id,
                fullName = $"{item.Ime} {item.Prezime}".Trim(),
                item.Email,
                phone = item.BrojMobitela,
                address = item.Adresa,
                role = item.Tip.ToString(),
                identityRoles = principal.FindAll(ClaimTypes.Role)
                    .Select(claim => claim.Value)
                    .Distinct()
                    .ToArray(),
                item.JeAktivan,
                item.KycPodnesen,
                profileImage = item.ProfilnaSlikaPath,
                profileLink = $"/Korisnik/Details/{item.Id}",
                settingsLink = "/Korisnik/Settings",
                kycLink = "/Korisnik/Kyc"
            })
            .SingleOrDefaultAsync(cancellationToken);

        return user is null
            ? SerializeError("Profil korisnika nije pronađen.")
            : JsonSerializer.Serialize(new { user }, JsonOptions);
    }

    private async Task<string> GetRidesAsync(
        int korisnikId,
        ClaimsPrincipal principal,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var scope = ReadString(arguments, "scope") ?? "available";
        var status = ReadString(arguments, "status") ?? "all";
        var id = ReadInt(arguments, "id");
        var search = ReadString(arguments, "search");
        var isAdmin = principal.IsInRole("Admin");
        var isDriver = principal.IsInRole("Driver") || isAdmin;

        var query = dbContext.Voznje
            .AsNoTracking()
            .Include(ride => ride.Vozac)
            .Include(ride => ride.PolazniGrad)
            .Include(ride => ride.OdredisniGrad)
            .Include(ride => ride.Rezervacije)
            .AsQueryable();

        query = scope switch
        {
            "passenger" => query.Where(ride =>
                ride.Rezervacije.Any(reservation => reservation.PutnikId == korisnikId)),
            "driver" when isDriver => query.Where(ride => ride.VozacId == korisnikId),
            "all" when isAdmin => query,
            "available" => query.Where(ride =>
                ride.Status == StatusVoznje.Planirana &&
                ride.SlobodnaMjesta > 0 &&
                ride.VozacId != korisnikId),
            _ => query.Where(_ => false)
        };

        query = status switch
        {
            "planned" => query.Where(ride => ride.Status == StatusVoznje.Planirana),
            "completed" => query.Where(ride => ride.Status == StatusVoznje.Zavrsena),
            "cancelled" => query.Where(ride => ride.Status == StatusVoznje.Otkazana),
            _ => query
        };

        if (id.HasValue)
        {
            query = query.Where(ride => ride.Id == id.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(ride =>
                ride.Opis.Contains(term) ||
                ride.PolazniGrad.Naziv.Contains(term) ||
                ride.OdredisniGrad.Naziv.Contains(term) ||
                ride.Vozac.Ime.Contains(term) ||
                ride.Vozac.Prezime.Contains(term));
        }

        var rides = await query
            .OrderBy(ride => ride.Polazak)
            .Take(100)
            .Select(ride => new
            {
                ride.Id,
                status = ride.Status.ToString(),
                route = $"{ride.PolazniGrad.Naziv} → {ride.OdredisniGrad.Naziv}",
                departure = ride.Polazak,
                arrival = ride.OcekivaniDolazak,
                driver = $"{ride.Vozac.Ime} {ride.Vozac.Prezime}".Trim(),
                ride.CijenaPoMjestu,
                ride.UkupnoMjesta,
                ride.SlobodnaMjesta,
                reservationCount = ride.Rezervacije.Count,
                description = ride.Opis,
                link = $"/Voznja/Details/{ride.Id}",
                detailsMarkdown = $"[Detalji](/Voznja/Details/{ride.Id})"
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(new
        {
            scope,
            status,
            count = rides.Count,
            rides,
            listLink = BuildRideListLink(scope, status),
            listMarkdown = $"[Prikaži vožnje]({BuildRideListLink(scope, status)})"
        }, JsonOptions);
    }

    private async Task<string> GetReservationsAsync(
        int korisnikId,
        ClaimsPrincipal principal,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var scope = ReadString(arguments, "scope") ?? "mine";
        var status = ReadString(arguments, "status") ?? "all";
        var id = ReadInt(arguments, "id");
        var isAdmin = principal.IsInRole("Admin");
        var isDriver = principal.IsInRole("Driver") || isAdmin;

        var query = dbContext.Rezervacije
            .AsNoTracking()
            .Include(reservation => reservation.Putnik)
            .Include(reservation => reservation.Voznja)
                .ThenInclude(ride => ride.Vozac)
            .Include(reservation => reservation.Voznja)
                .ThenInclude(ride => ride.PolazniGrad)
            .Include(reservation => reservation.Voznja)
                .ThenInclude(ride => ride.OdredisniGrad)
            .AsQueryable();

        query = scope switch
        {
            "mine" => query.Where(reservation => reservation.PutnikId == korisnikId),
            "my_rides" when isDriver => query.Where(reservation =>
                reservation.Voznja.VozacId == korisnikId),
            "all" when isAdmin => query,
            _ => query.Where(_ => false)
        };

        query = status switch
        {
            "pending" => query.Where(reservation =>
                reservation.Status == StatusRezervacije.UProcesuPotvrde),
            "confirmed" => query.Where(reservation =>
                reservation.Status == StatusRezervacije.Potvrdena),
            "rejected" => query.Where(reservation =>
                reservation.Status == StatusRezervacije.Odbijena),
            "completed" => query.Where(reservation =>
                reservation.Status == StatusRezervacije.Zavrsena),
            _ => query
        };

        if (id.HasValue)
        {
            query = query.Where(reservation => reservation.Id == id.Value);
        }

        var reservations = await query
            .OrderByDescending(reservation => reservation.VrijemeRezervacije)
            .Take(100)
            .Select(reservation => new
            {
                reservation.Id,
                reservation.VoznjaId,
                passenger = $"{reservation.Putnik.Ime} {reservation.Putnik.Prezime}".Trim(),
                driver = $"{reservation.Voznja.Vozac.Ime} {reservation.Voznja.Vozac.Prezime}".Trim(),
                status = reservation.Status.ToDisplayName(),
                rideStatus = reservation.Voznja.Status.ToString(),
                route = $"{reservation.Voznja.PolazniGrad.Naziv} → {reservation.Voznja.OdredisniGrad.Naziv}",
                departure = reservation.Voznja.Polazak,
                reservation.BrojMjesta,
                reservation.CijenaUkupno,
                reservation.VrijemeRezervacije,
                note = reservation.Napomena,
                link = $"/Rezervacija/Details/{reservation.Id}",
                detailsMarkdown = $"[Detalji rezervacije](/Rezervacija/Details/{reservation.Id})",
                rideLink = $"/Voznja/Details/{reservation.VoznjaId}",
                rideMarkdown = $"[Detalji vožnje](/Voznja/Details/{reservation.VoznjaId})"
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(
            new { scope, status, count = reservations.Count, reservations },
            JsonOptions);
    }

    private async Task<string> GetBalanceAsync(
        int korisnikId,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(ReadInt(arguments, "transactionLimit") ?? 20, 1, 100);
        var user = await dbContext.Korisnici
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == korisnikId, cancellationToken);
        if (user is null)
        {
            return SerializeError("Profil korisnika nije pronađen.");
        }

        var reservedFunds = await dbContext.Rezervacije
            .AsNoTracking()
            .Where(reservation =>
                reservation.PutnikId == korisnikId &&
                reservation.Voznja.Status == StatusVoznje.Planirana &&
                (reservation.Status == StatusRezervacije.UProcesuPotvrde ||
                 reservation.Status == StatusRezervacije.Potvrdena))
            .SumAsync(reservation => (decimal?)reservation.CijenaUkupno, cancellationToken) ?? 0;

        var transactions = await dbContext.SaldoTransakcije
            .AsNoTracking()
            .Where(transaction => transaction.KorisnikId == korisnikId)
            .OrderByDescending(transaction => transaction.Vrijeme)
            .Take(limit)
            .Select(transaction => new
            {
                transaction.Id,
                transaction.Iznos,
                transaction.Tip,
                transaction.Komentar,
                transaction.SaldoPrije,
                transaction.SaldoPoslije,
                transaction.Vrijeme
            })
            .ToListAsync(cancellationToken);

        return JsonSerializer.Serialize(new
        {
            balance = user.Saldo,
            reservedFunds,
            availableBalance = user.Saldo - reservedFunds,
            transactionCount = transactions.Count,
            transactions,
            balanceLink = "/Korisnik/Saldo",
            topUpLink = "/Korisnik/Uplata"
        }, JsonOptions);
    }

    private static object Tool(string name, string description, object parameters) => new
    {
        type = "function",
        function = new
        {
            name,
            description,
            parameters
        }
    };

    private static string BuildRideListLink(string scope, string status)
    {
        var view = scope switch
        {
            "passenger" => "ridden",
            "driver" => "driving",
            "all" => "all",
            _ => "available"
        };

        return $"/Voznja?view={view}&status={status}";
    }

    private static JsonDocument ParseArguments(string json)
    {
        try
        {
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        }
        catch (JsonException)
        {
            return JsonDocument.Parse("{}");
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var result)
            ? result
            : null;

    private static string SerializeError(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOptions);
}
