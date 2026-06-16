using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Commands;
using SideSeat.Security;

namespace SideSeat.Services;

public sealed class AiToolService(
    SideSeatDbContext dbContext,
    IPendingActionService pendingActions,
    IPublicWebSearchService publicWebSearch) : IAiToolService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
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
            }),
        Tool(
            "search_public_web",
            "Pretražuje javne internetske izvore za vanjske informacije. Koristi Wikipedia za enciklopedijske upite i internet za opću web pretragu. Uvijek citiraj URL izvora iz rezultata.",
            new
            {
                type = "object",
                properties = new
                {
                    query = new
                    {
                        type = "string",
                        description = "Kratak upit za javnu web pretragu."
                    },
                    source = new
                    {
                        type = "string",
                        @enum = new[] { "auto", "wikipedia", "internet" },
                        description = "auto kombinira Wikipedia i opću web pretragu."
                    },
                    language = new
                    {
                        type = "string",
                        @enum = new[] { "hr", "en" },
                        description = "Jezik rezultata, zadano hr."
                    },
                    limit = new
                    {
                        type = "integer",
                        minimum = 1,
                        maximum = 5
                    }
                },
                required = new[] { "query" },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_city",
            "Priprema kreiranje grada za administratora. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    naziv = new { type = "string" },
                    drzava = new { type = "string" },
                    postanskiBroj = new { type = "string" },
                    latitude = new { type = "number", minimum = -90, maximum = 90 },
                    longitude = new { type = "number", minimum = -180, maximum = 180 }
                },
                required = new[] { "naziv", "drzava", "postanskiBroj" },
                additionalProperties = false
            }),
        Tool(
            "prepare_update_city",
            "Priprema uređivanje grada za administratora. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    naziv = new { type = "string" },
                    drzava = new { type = "string" },
                    postanskiBroj = new { type = "string" },
                    latitude = new { type = "number", minimum = -90, maximum = 90 },
                    longitude = new { type = "number", minimum = -180, maximum = 180 }
                },
                required = new[] { "id", "naziv", "drzava", "postanskiBroj" },
                additionalProperties = false
            }),
        Tool(
            "prepare_delete_city",
            "Priprema brisanje grada za administratora. Ne izvršava brisanje bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_vehicle",
            "Priprema kreiranje vozila za administratora. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    marka = new { type = "string" },
                    model = new { type = "string" },
                    registracija = new { type = "string" },
                    godinaProizvodnje = new { type = "integer" },
                    brojSjedala = new { type = "integer" },
                    boja = new { type = "string" },
                    prosjecnaPotrosnja = new { type = "number" },
                    vlasnikId = new { type = "integer" }
                },
                required = new[]
                {
                    "marka", "model", "registracija", "godinaProizvodnje",
                    "brojSjedala", "boja", "prosjecnaPotrosnja"
                },
                additionalProperties = false
            }),
        Tool(
            "prepare_update_vehicle",
            "Priprema uređivanje vozila za administratora. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    marka = new { type = "string" },
                    model = new { type = "string" },
                    registracija = new { type = "string" },
                    godinaProizvodnje = new { type = "integer" },
                    brojSjedala = new { type = "integer" },
                    boja = new { type = "string" },
                    prosjecnaPotrosnja = new { type = "number" },
                    vlasnikId = new { type = "integer" }
                },
                required = new[]
                {
                    "id", "marka", "model", "registracija", "godinaProizvodnje",
                    "brojSjedala", "boja", "prosjecnaPotrosnja"
                },
                additionalProperties = false
            }),
        Tool(
            "prepare_delete_vehicle",
            "Priprema brisanje vozila za administratora. Ne izvršava brisanje bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_user",
            "Priprema kreiranje korisnika za administratora. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    ime = new { type = "string" },
                    prezime = new { type = "string" },
                    email = new { type = "string" },
                    adresa = new { type = "string" },
                    brojMobitela = new { type = "string" },
                    tip = new { type = "string", @enum = new[] { "Vozac", "Putnik", "Admin", "VozacIPutnik" } },
                    jeAktivan = new { type = "boolean" },
                    kycPodnesen = new { type = "boolean" },
                    kycOib = new { type = "string" },
                    kycBrojOsobne = new { type = "string" },
                    kycBrojVozacke = new { type = "string" },
                    kycDatumRodenja = new { type = "string", format = "date-time" },
                    voziloId = new { type = "integer" },
                    password = new { type = "string" }
                },
                required = new[] { "ime", "prezime", "email", "adresa", "brojMobitela", "password" },
                additionalProperties = false
            }),
        Tool(
            "prepare_update_user",
            "Priprema uređivanje korisnika za administratora. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    ime = new { type = "string" },
                    prezime = new { type = "string" },
                    email = new { type = "string" },
                    adresa = new { type = "string" },
                    brojMobitela = new { type = "string" },
                    tip = new { type = "string", @enum = new[] { "Vozac", "Putnik", "Admin", "VozacIPutnik" } },
                    jeAktivan = new { type = "boolean" },
                    kycPodnesen = new { type = "boolean" },
                    kycOib = new { type = "string" },
                    kycBrojOsobne = new { type = "string" },
                    kycBrojVozacke = new { type = "string" },
                    kycDatumRodenja = new { type = "string", format = "date-time" },
                    voziloId = new { type = "integer" },
                    password = new { type = "string" }
                },
                required = new[] { "id", "ime", "prezime", "email", "adresa", "brojMobitela" },
                additionalProperties = false
            }),
        Tool(
            "prepare_delete_user",
            "Priprema brisanje korisnika za administratora. Ne izvršava brisanje bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_payment",
            "Priprema administratorsko evidentiranje plaćanja. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    rezervacijaId = new { type = "integer" },
                    iznos = new { type = "number" },
                    vrijemePlacanja = new { type = "string", format = "date-time" },
                    nacinPlacanja = new
                    {
                        type = "string",
                        @enum = new[] { "PayPal", "Kartica", "RevolutPay", "SideSeatSaldo", "Gotovina" }
                    },
                    uspjesno = new { type = "boolean" }
                },
                required = new[] { "rezervacijaId", "iznos", "nacinPlacanja" },
                additionalProperties = false
            }),
        Tool(
            "prepare_update_payment",
            "Priprema administratorsko uređivanje plaćanja. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    rezervacijaId = new { type = "integer" },
                    iznos = new { type = "number" },
                    vrijemePlacanja = new { type = "string", format = "date-time" },
                    nacinPlacanja = new
                    {
                        type = "string",
                        @enum = new[] { "PayPal", "Kartica", "RevolutPay", "SideSeatSaldo", "Gotovina" }
                    },
                    uspjesno = new { type = "boolean" }
                },
                required = new[] { "id", "rezervacijaId", "iznos", "nacinPlacanja" },
                additionalProperties = false
            }),
        Tool(
            "prepare_delete_payment",
            "Priprema brisanje plaćanja za administratora. Ne izvršava brisanje bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_balance_transaction",
            "Priprema saldo transakciju. Administrator može odabrati korisnika, korisnik samo sebe.",
            new
            {
                type = "object",
                properties = new
                {
                    korisnikId = new { type = "integer" },
                    iznos = new { type = "number" },
                    tip = new { type = "string", @enum = new[] { "uplata", "isplata", "korekcija" } },
                    komentar = new { type = "string" }
                },
                required = new[] { "iznos", "tip" },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_ride",
            "Priprema kreiranje vožnje. Vozač može kreirati samo vlastitu vožnju.",
            new
            {
                type = "object",
                properties = new
                {
                    vozacId = new { type = "integer" },
                    polazniGradId = new { type = "integer" },
                    odredisniGradId = new { type = "integer" },
                    polazak = new { type = "string", format = "date-time" },
                    ocekivaniDolazak = new { type = "string", format = "date-time" },
                    cijenaPoMjestu = new { type = "number" },
                    ukupnoMjesta = new { type = "integer" },
                    slobodnaMjesta = new { type = "integer" },
                    opis = new { type = "string" }
                },
                required = new[]
                {
                    "polazniGradId", "odredisniGradId", "polazak", "ocekivaniDolazak",
                    "cijenaPoMjestu", "ukupnoMjesta", "slobodnaMjesta"
                },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_reservation",
            "Priprema rezervaciju vožnje za prijavljenog korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    voznjaId = new { type = "integer" },
                    brojMjesta = new { type = "integer", minimum = 1, maximum = 10 },
                    nacinPlacanja = new
                    {
                        type = "string",
                        @enum = new[] { "SideSeatSaldo", "Gotovina" }
                    },
                    napojnica = new { type = "number", minimum = 0, maximum = 10000 },
                    napomena = new { type = "string" }
                },
                required = new[] { "voznjaId", "brojMjesta" },
                additionalProperties = false
            }),
        Tool(
            "prepare_update_reservation",
            "Priprema administratorsko uređivanje rezervacije. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    voznjaId = new { type = "integer" },
                    putnikId = new { type = "integer" },
                    brojMjesta = new { type = "integer", minimum = 1, maximum = 10 },
                    status = new
                    {
                        type = "string",
                        @enum = new[] { "UProcesuPotvrde", "Potvrdena", "Odbijena", "Zavrsena" }
                    },
                    nacinPlacanja = new
                    {
                        type = "string",
                        @enum = new[] { "SideSeatSaldo", "Gotovina" }
                    },
                    napojnica = new { type = "number", minimum = 0, maximum = 10000 },
                    napomena = new { type = "string" }
                },
                required = new[] { "id", "voznjaId", "putnikId", "brojMjesta", "status" },
                additionalProperties = false
            }),
        Tool(
            "prepare_delete_reservation",
            "Priprema brisanje rezervacije za administratora. Ne izvršava brisanje bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_create_review",
            "Priprema ocjenu završene rezervacije za prijavljenog korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    rezervacijaId = new { type = "integer" },
                    brojZvjezdica = new { type = "integer", minimum = 1, maximum = 5 },
                    komentar = new { type = "string" }
                },
                required = new[] { "rezervacijaId", "brojZvjezdica", "komentar" },
                additionalProperties = false
            }),
        Tool(
            "prepare_update_review",
            "Priprema uređivanje ocjene. Autor uređuje svoju ocjenu, administrator može urediti sve.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    rezervacijaId = new { type = "integer" },
                    autorId = new { type = "integer" },
                    brojZvjezdica = new { type = "integer", minimum = 1, maximum = 5 },
                    komentar = new { type = "string" }
                },
                required = new[] { "id", "rezervacijaId", "autorId", "brojZvjezdica", "komentar" },
                additionalProperties = false
            }),
        Tool(
            "prepare_delete_review",
            "Priprema brisanje ocjene. Autor može brisati svoju ocjenu, administrator sve.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_update_ride",
            "Priprema uređivanje postojeće vožnje. Ne izvršava upis bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    vozacId = new { type = "integer" },
                    polazniGradId = new { type = "integer" },
                    odredisniGradId = new { type = "integer" },
                    polazak = new { type = "string", format = "date-time" },
                    ocekivaniDolazak = new { type = "string", format = "date-time" },
                    cijenaPoMjestu = new { type = "number" },
                    ukupnoMjesta = new { type = "integer" },
                    slobodnaMjesta = new { type = "integer" },
                    opis = new { type = "string" },
                    status = new
                    {
                        type = "string",
                        @enum = new[] { "Planirana", "Aktivna", "Zavrsena", "Otkazana" }
                    }
                },
                required = new[]
                {
                    "id", "polazniGradId", "odredisniGradId", "polazak", "ocekivaniDolazak",
                    "cijenaPoMjestu", "ukupnoMjesta", "slobodnaMjesta"
                },
                additionalProperties = false
            }),
        Tool(
            "prepare_delete_ride",
            "Priprema brisanje vožnje. Ne briše bez potvrde korisnika.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_start_ride",
            "Priprema pokretanje vožnje kada su potvrđeni putnici spremni.",
            new
            {
                type = "object",
                properties = new { id = new { type = "integer" } },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_finish_ride",
            "Priprema završetak vožnje i settlement salda/gotovine.",
            new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer" },
                    cashCollected = new { type = "boolean" }
                },
                required = new[] { "id" },
                additionalProperties = false
            }),
        Tool(
            "prepare_check_in_reservation",
            "Priprema check-in putnika u auto, uz opcionalnu lokaciju.",
            new
            {
                type = "object",
                properties = new
                {
                    rezervacijaId = new { type = "integer" },
                    latitude = new { type = "number", minimum = -90, maximum = 90 },
                    longitude = new { type = "number", minimum = -180, maximum = 180 }
                },
                required = new[] { "rezervacijaId" },
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
            "search_public_web" => await SearchPublicWebAsync(
                arguments.RootElement,
                cancellationToken),
            "prepare_create_city" => PrepareCreateCity(principal, arguments.RootElement),
            "prepare_update_city" => PrepareUpdateCity(principal, arguments.RootElement),
            "prepare_delete_city" => PrepareDeleteCity(principal, arguments.RootElement),
            "prepare_create_vehicle" => PrepareCreateVehicle(principal, arguments.RootElement),
            "prepare_update_vehicle" => PrepareUpdateVehicle(principal, arguments.RootElement),
            "prepare_delete_vehicle" => PrepareDeleteVehicle(principal, arguments.RootElement),
            "prepare_create_user" => PrepareCreateUser(principal, arguments.RootElement),
            "prepare_update_user" => PrepareUpdateUser(principal, arguments.RootElement),
            "prepare_delete_user" => PrepareDeleteUser(principal, arguments.RootElement),
            "prepare_create_payment" => PrepareCreatePayment(principal, arguments.RootElement),
            "prepare_update_payment" => PrepareUpdatePayment(principal, arguments.RootElement),
            "prepare_delete_payment" => PrepareDeletePayment(principal, arguments.RootElement),
            "prepare_create_balance_transaction" => PrepareCreateBalanceTransaction(principal, arguments.RootElement),
            "prepare_create_ride" => PrepareCreateRide(principal, arguments.RootElement),
            "prepare_create_reservation" => PrepareCreateReservation(principal, arguments.RootElement),
            "prepare_update_reservation" => PrepareUpdateReservation(principal, arguments.RootElement),
            "prepare_delete_reservation" => PrepareDeleteReservation(principal, arguments.RootElement),
            "prepare_create_review" => PrepareCreateReview(principal, arguments.RootElement),
            "prepare_update_review" => PrepareUpdateReview(principal, arguments.RootElement),
            "prepare_delete_review" => PrepareDeleteReview(principal, arguments.RootElement),
            "prepare_update_ride" => PrepareUpdateRide(principal, arguments.RootElement),
            "prepare_delete_ride" => PrepareDeleteRide(principal, arguments.RootElement),
            "prepare_start_ride" => PrepareStartRide(principal, arguments.RootElement),
            "prepare_finish_ride" => PrepareFinishRide(principal, arguments.RootElement),
            "prepare_check_in_reservation" => PrepareCheckInReservation(principal, arguments.RootElement),
            _ => SerializeError($"Nepoznat alat: {toolName}.")
        };
    }

    private string PrepareCreateCity(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može kreirati grad.");
        }

        var command = new CreateCityCommand(
            ReadString(arguments, "naziv") ?? string.Empty,
            ReadString(arguments, "drzava") ?? string.Empty,
            ReadString(arguments, "postanskiBroj") ?? string.Empty,
            ReadDecimal(arguments, "latitude"),
            ReadDecimal(arguments, "longitude"));
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreateCity,
            "Kreiranje grada",
            $"{command.Naziv}, {command.Drzava} ({command.PostanskiBroj})",
            command));
    }

    private string PrepareUpdateCity(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može uređivati grad.");
        }

        var command = new UpdateCityCommand(
            ReadInt(arguments, "id") ?? 0,
            ReadString(arguments, "naziv") ?? string.Empty,
            ReadString(arguments, "drzava") ?? string.Empty,
            ReadString(arguments, "postanskiBroj") ?? string.Empty,
            ReadDecimal(arguments, "latitude"),
            ReadDecimal(arguments, "longitude"));
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.UpdateCity,
            "Uređivanje grada",
            $"Grad #{command.Id}: {command.Naziv}, {command.Drzava}",
            command));
    }

    private string PrepareDeleteCity(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može brisati grad.");
        }

        var command = new DeleteCityCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.DeleteCity,
            "Brisanje grada",
            $"Grad #{command.Id}. Brisanje će biti odbijeno ako postoje povezane vožnje.",
            command));
    }

    private string PrepareCreateVehicle(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može kreirati vozilo.");
        }

        var command = new CreateVehicleCommand(
            ReadString(arguments, "marka") ?? string.Empty,
            ReadString(arguments, "model") ?? string.Empty,
            ReadString(arguments, "registracija") ?? string.Empty,
            ReadInt(arguments, "godinaProizvodnje") ?? 0,
            ReadInt(arguments, "brojSjedala") ?? 0,
            ReadString(arguments, "boja") ?? string.Empty,
            ReadDecimal(arguments, "prosjecnaPotrosnja") ?? 0,
            ReadInt(arguments, "vlasnikId"));
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreateVehicle,
            "Kreiranje vozila",
            $"{command.Marka} {command.Model}, {command.Registracija}",
            command));
    }

    private string PrepareUpdateVehicle(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može uređivati vozilo.");
        }

        var command = new UpdateVehicleCommand(
            ReadInt(arguments, "id") ?? 0,
            ReadString(arguments, "marka") ?? string.Empty,
            ReadString(arguments, "model") ?? string.Empty,
            ReadString(arguments, "registracija") ?? string.Empty,
            ReadInt(arguments, "godinaProizvodnje") ?? 0,
            ReadInt(arguments, "brojSjedala") ?? 0,
            ReadString(arguments, "boja") ?? string.Empty,
            ReadDecimal(arguments, "prosjecnaPotrosnja") ?? 0,
            ReadInt(arguments, "vlasnikId"));
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.UpdateVehicle,
            "Uređivanje vozila",
            $"Vozilo #{command.Id}: {command.Marka} {command.Model}, {command.Registracija}",
            command));
    }

    private string PrepareDeleteVehicle(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može brisati vozilo.");
        }

        var command = new DeleteVehicleCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.DeleteVehicle,
            "Brisanje vozila",
            $"Vozilo #{command.Id}. Korisnici povezani s vozilom bit će odspojeni.",
            command));
    }

    private string PrepareCreateUser(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može kreirati korisnika.");
        }

        var command = new CreateUserCommand(
            ReadString(arguments, "ime") ?? string.Empty,
            ReadString(arguments, "prezime") ?? string.Empty,
            ReadString(arguments, "email") ?? string.Empty,
            ReadString(arguments, "adresa") ?? string.Empty,
            ReadString(arguments, "brojMobitela") ?? string.Empty,
            ReadUserType(arguments, "tip"),
            ReadBool(arguments, "jeAktivan") ?? true,
            ReadBool(arguments, "kycPodnesen") ?? false,
            ReadString(arguments, "kycOib"),
            ReadString(arguments, "kycBrojOsobne"),
            ReadString(arguments, "kycBrojVozacke"),
            ReadDateTime(arguments, "kycDatumRodenja"),
            ReadInt(arguments, "voziloId"),
            ReadString(arguments, "password") ?? string.Empty);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreateUser,
            "Kreiranje korisnika",
            $"{command.Ime} {command.Prezime} ({command.Email})",
            command));
    }

    private string PrepareUpdateUser(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može uređivati korisnika.");
        }

        var command = new UpdateUserCommand(
            ReadInt(arguments, "id") ?? 0,
            ReadString(arguments, "ime") ?? string.Empty,
            ReadString(arguments, "prezime") ?? string.Empty,
            ReadString(arguments, "email") ?? string.Empty,
            ReadString(arguments, "adresa") ?? string.Empty,
            ReadString(arguments, "brojMobitela") ?? string.Empty,
            ReadUserType(arguments, "tip"),
            ReadBool(arguments, "jeAktivan") ?? true,
            ReadBool(arguments, "kycPodnesen") ?? false,
            ReadString(arguments, "kycOib"),
            ReadString(arguments, "kycBrojOsobne"),
            ReadString(arguments, "kycBrojVozacke"),
            ReadDateTime(arguments, "kycDatumRodenja"),
            ReadInt(arguments, "voziloId"),
            ReadString(arguments, "password"));
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.UpdateUser,
            "Uređivanje korisnika",
            $"Korisnik #{command.Id}: {command.Ime} {command.Prezime} ({command.Email})",
            command));
    }

    private string PrepareDeleteUser(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može brisati korisnika.");
        }

        var command = new DeleteUserCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.DeleteUser,
            "Brisanje korisnika",
            $"Korisnik #{command.Id}. Brisanje će biti odbijeno ako postoje vožnje ili rezervacije.",
            command));
    }

    private string PrepareCreatePayment(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može evidentirati plaćanje.");
        }

        var command = new CreatePaymentCommand(
            ReadInt(arguments, "rezervacijaId") ?? 0,
            ReadDecimal(arguments, "iznos") ?? 0,
            ReadDateTime(arguments, "vrijemePlacanja") ?? DateTime.UtcNow,
            ReadAnyPaymentMethod(arguments, "nacinPlacanja"),
            ReadBool(arguments, "uspjesno") ?? true);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreatePayment,
            "Evidentiranje plaćanja",
            $"Rezervacija #{command.RezervacijaId}, {command.Iznos:0.00} EUR, {command.NacinPlacanja}",
            command));
    }

    private string PrepareUpdatePayment(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može uređivati plaćanje.");
        }

        var command = new UpdatePaymentCommand(
            ReadInt(arguments, "id") ?? 0,
            ReadInt(arguments, "rezervacijaId") ?? 0,
            ReadDecimal(arguments, "iznos") ?? 0,
            ReadDateTime(arguments, "vrijemePlacanja") ?? DateTime.UtcNow,
            ReadAnyPaymentMethod(arguments, "nacinPlacanja"),
            ReadBool(arguments, "uspjesno") ?? true);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.UpdatePayment,
            "Uređivanje plaćanja",
            $"Plaćanje #{command.Id}, rezervacija #{command.RezervacijaId}, {command.Iznos:0.00} EUR",
            command));
    }

    private string PrepareDeletePayment(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može brisati plaćanje.");
        }

        var command = new DeletePaymentCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.DeletePayment,
            "Brisanje plaćanja",
            $"Plaćanje #{command.Id}",
            command));
    }

    private string PrepareCreateBalanceTransaction(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new CreateBalanceTransactionCommand(
            ReadInt(arguments, "korisnikId"),
            ReadDecimal(arguments, "iznos") ?? 0,
            ReadString(arguments, "tip") ?? string.Empty,
            ReadString(arguments, "komentar") ?? string.Empty);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreateBalanceTransaction,
            "Saldo transakcija",
            $"{command.Tip}: {command.Iznos:0.00} EUR",
            command));
    }

    private string PrepareCreateRide(ClaimsPrincipal principal, JsonElement arguments)
    {
        var currentUserId = principal.GetKorisnikId()!.Value;
        var driverId = principal.IsInRole("Admin")
            ? ReadInt(arguments, "vozacId") ?? currentUserId
            : currentUserId;
        var command = new CreateRideCommand(
            driverId,
            ReadInt(arguments, "polazniGradId") ?? 0,
            ReadInt(arguments, "odredisniGradId") ?? 0,
            ReadDateTime(arguments, "polazak") ?? default,
            ReadDateTime(arguments, "ocekivaniDolazak") ?? default,
            ReadDecimal(arguments, "cijenaPoMjestu") ?? 0,
            ReadInt(arguments, "ukupnoMjesta") ?? 0,
            ReadInt(arguments, "slobodnaMjesta") ?? 0,
            ReadString(arguments, "opis") ?? string.Empty);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreateRide,
            "Kreiranje vožnje",
            $"Ruta grad #{command.PolazniGradId} → grad #{command.OdredisniGradId}, {command.Polazak:dd.MM.yyyy HH:mm}",
            command));
    }

    private string PrepareCreateReservation(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new CreateReservationCommand(
            ReadInt(arguments, "voznjaId") ?? 0,
            ReadInt(arguments, "brojMjesta") ?? 0,
            ReadString(arguments, "napomena") ?? string.Empty,
            ReadPaymentMethod(arguments, "nacinPlacanja"),
            ReadDecimal(arguments, "napojnica") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreateReservation,
            "Kreiranje rezervacije",
            $"Vožnja #{command.VoznjaId}, mjesta: {command.BrojMjesta}, plaćanje: {command.NacinPlacanja}",
            command));
    }

    private string PrepareUpdateReservation(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može uređivati rezervaciju.");
        }

        var command = new UpdateReservationCommand(
            ReadInt(arguments, "id") ?? 0,
            ReadInt(arguments, "voznjaId") ?? 0,
            ReadInt(arguments, "putnikId") ?? 0,
            ReadInt(arguments, "brojMjesta") ?? 0,
            ReadReservationStatus(arguments, "status"),
            ReadPaymentMethod(arguments, "nacinPlacanja"),
            ReadDecimal(arguments, "napojnica") ?? 0,
            ReadString(arguments, "napomena") ?? string.Empty);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.UpdateReservation,
            "Uređivanje rezervacije",
            $"Rezervacija #{command.Id}, vožnja #{command.VoznjaId}, putnik #{command.PutnikId}, status {command.Status}",
            command));
    }

    private string PrepareDeleteReservation(ClaimsPrincipal principal, JsonElement arguments)
    {
        if (!principal.IsInRole("Admin"))
        {
            return SerializeError("Samo administrator može brisati rezervaciju.");
        }

        var command = new DeleteReservationCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.DeleteReservation,
            "Brisanje rezervacije",
            $"Rezervacija #{command.Id}. Povezane ocjene i plaćanja bit će uklonjena.",
            command));
    }

    private string PrepareCreateReview(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new CreateReviewCommand(
            ReadInt(arguments, "rezervacijaId") ?? 0,
            ReadInt(arguments, "brojZvjezdica") ?? 0,
            ReadString(arguments, "komentar") ?? string.Empty);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CreateReview,
            "Kreiranje ocjene",
            $"Rezervacija #{command.RezervacijaId}, ocjena {command.BrojZvjezdica}/5",
            command));
    }

    private string PrepareUpdateReview(ClaimsPrincipal principal, JsonElement arguments)
    {
        var currentUserId = principal.GetKorisnikId()!.Value;
        var command = new UpdateReviewCommand(
            ReadInt(arguments, "id") ?? 0,
            ReadInt(arguments, "rezervacijaId") ?? 0,
            principal.IsInRole("Admin") ? ReadInt(arguments, "autorId") ?? currentUserId : currentUserId,
            ReadInt(arguments, "brojZvjezdica") ?? 0,
            ReadString(arguments, "komentar") ?? string.Empty);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.UpdateReview,
            "Uređivanje ocjene",
            $"Ocjena #{command.Id}, rezervacija #{command.RezervacijaId}, {command.BrojZvjezdica}/5",
            command));
    }

    private string PrepareDeleteReview(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new DeleteReviewCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.DeleteReview,
            "Brisanje ocjene",
            $"Ocjena #{command.Id}",
            command));
    }

    private string PrepareUpdateRide(ClaimsPrincipal principal, JsonElement arguments)
    {
        var currentUserId = principal.GetKorisnikId()!.Value;
        var driverId = principal.IsInRole("Admin")
            ? ReadInt(arguments, "vozacId") ?? currentUserId
            : currentUserId;
        var command = new UpdateRideCommand(
            ReadInt(arguments, "id") ?? 0,
            driverId,
            ReadInt(arguments, "polazniGradId") ?? 0,
            ReadInt(arguments, "odredisniGradId") ?? 0,
            ReadDateTime(arguments, "polazak") ?? default,
            ReadDateTime(arguments, "ocekivaniDolazak") ?? default,
            ReadDecimal(arguments, "cijenaPoMjestu") ?? 0,
            ReadInt(arguments, "ukupnoMjesta") ?? 0,
            ReadInt(arguments, "slobodnaMjesta") ?? 0,
            ReadString(arguments, "opis") ?? string.Empty,
            ReadRideStatus(arguments, "status"));
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.UpdateRide,
            "Uređivanje vožnje",
            $"Vožnja #{command.Id}: grad #{command.PolazniGradId} → grad #{command.OdredisniGradId}",
            command));
    }

    private string PrepareDeleteRide(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new DeleteRideCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.DeleteRide,
            "Brisanje vožnje",
            $"Vožnja #{command.Id}",
            command));
    }

    private string PrepareStartRide(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new StartRideCommand(ReadInt(arguments, "id") ?? 0);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.StartRide,
            "Pokretanje vožnje",
            $"Vožnja #{command.Id}",
            command));
    }

    private string PrepareFinishRide(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new FinishRideCommand(
            ReadInt(arguments, "id") ?? 0,
            ReadBool(arguments, "cashCollected") ?? true);
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.FinishRide,
            "Završetak vožnje",
            $"Vožnja #{command.Id}, gotovina primljena: {(command.CashCollected ? "da" : "ne")}",
            command));
    }

    private string PrepareCheckInReservation(ClaimsPrincipal principal, JsonElement arguments)
    {
        var command = new CheckInReservationCommand(
            ReadInt(arguments, "rezervacijaId") ?? 0,
            ReadDecimal(arguments, "latitude"),
            ReadDecimal(arguments, "longitude"));
        return SerializePending(pendingActions.Create(
            principal,
            SideSeatActionTypes.CheckInReservation,
            "Check-in putnika",
            $"Rezervacija #{command.RezervacijaId}",
            command));
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
                reservation.Napojnica,
                reservation.NacinPlacanja,
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
                reservation.NacinPlacanja == NacinPlacanja.SideSeatSaldo &&
                reservation.Voznja.Status == StatusVoznje.Planirana &&
                (reservation.Status == StatusRezervacije.UProcesuPotvrde ||
                 reservation.Status == StatusRezervacije.Potvrdena))
            .SumAsync(reservation => (decimal?)(reservation.CijenaUkupno + reservation.Napojnica), cancellationToken) ?? 0;

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

    private async Task<string> SearchPublicWebAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var query = ReadString(arguments, "query");
        if (string.IsNullOrWhiteSpace(query))
        {
            return SerializeError("Upit za javnu web pretragu ne smije biti prazan.");
        }

        var response = await publicWebSearch.SearchAsync(
            new PublicWebSearchRequest(
                query,
                ReadString(arguments, "source") ?? "auto",
                ReadString(arguments, "language") ?? "hr",
                ReadInt(arguments, "limit") ?? 5),
            cancellationToken);

        return JsonSerializer.Serialize(new
        {
            response.Query,
            response.Source,
            response.Language,
            response.RetrievedAt,
            count = response.Results.Count,
            response.Warning,
            results = response.Results.Select(result => new
            {
                result.Title,
                result.Snippet,
                result.Url,
                result.Source,
                markdown = $"[{result.Title}]({result.Url})"
            })
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
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var result)
            ? result
            : null;

    private static decimal? ReadDecimal(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetDecimal(out var result)
            ? result
            : null;

    private static bool? ReadBool(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static DateTime? ReadDateTime(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        value.TryGetDateTime(out var result)
            ? result
            : null;

    private static NacinPlacanja ReadPaymentMethod(JsonElement root, string name)
    {
        var value = ReadString(root, name);
        return Enum.TryParse<NacinPlacanja>(value, ignoreCase: true, out var parsed) &&
               parsed is NacinPlacanja.SideSeatSaldo or NacinPlacanja.Gotovina
            ? parsed
            : NacinPlacanja.SideSeatSaldo;
    }

    private static NacinPlacanja ReadAnyPaymentMethod(JsonElement root, string name)
    {
        var value = ReadString(root, name);
        return Enum.TryParse<NacinPlacanja>(value, ignoreCase: true, out var parsed)
            ? parsed
            : NacinPlacanja.SideSeatSaldo;
    }

    private static TipKorisnika ReadUserType(JsonElement root, string name)
    {
        var value = ReadString(root, name);
        return Enum.TryParse<TipKorisnika>(value, ignoreCase: true, out var parsed)
            ? parsed
            : TipKorisnika.Putnik;
    }

    private static StatusRezervacije ReadReservationStatus(JsonElement root, string name)
    {
        var value = ReadString(root, name);
        return Enum.TryParse<StatusRezervacije>(value, ignoreCase: true, out var parsed)
            ? parsed
            : StatusRezervacije.UProcesuPotvrde;
    }

    private static StatusVoznje ReadRideStatus(JsonElement root, string name)
    {
        var value = ReadString(root, name);
        return Enum.TryParse<StatusVoznje>(value, ignoreCase: true, out var parsed)
            ? parsed
            : StatusVoznje.Planirana;
    }

    private static string SerializePending(PendingActionDescriptor action) =>
        JsonSerializer.Serialize(new
        {
            requiresConfirmation = true,
            pendingAction = action
        }, JsonOptions);

    private static string SerializeError(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOptions);
}
