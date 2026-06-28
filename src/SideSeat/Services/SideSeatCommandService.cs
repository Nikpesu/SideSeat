using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Models.Commands;
using SideSeat.Security;

namespace SideSeat.Services;

public sealed class SideSeatCommandService(
    SideSeatDbContext dbContext,
    IAuditService auditService,
    INotificationService notifications,
    ICityGeocodingService cityGeocoding,
    IPasswordHashingService passwordHashing) : ISideSeatCommandService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<CommandResult> ExecuteAsync<T>(
        string actionType,
        T command,
        ClaimsPrincipal principal,
        string source,
        CancellationToken cancellationToken = default)
    {
        var envelope = new PendingActionEnvelope(
            actionType,
            principal.GetKorisnikId() ?? 0,
            actionType,
            actionType,
            JsonSerializer.Serialize(command, JsonOptions),
            DateTime.UtcNow.AddMinutes(1),
            null);
        return ExecutePendingAsync(envelope, principal, source, cancellationToken);
    }

    public async Task<CommandResult> ExecutePendingAsync(
        PendingActionEnvelope action,
        ClaimsPrincipal principal,
        string source,
        CancellationToken cancellationToken)
    {
        IDbContextTransaction? transaction = null;
        CommandResult result;
        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            result = action.ActionType switch
            {
                SideSeatActionTypes.CreateCity => await CreateCityAsync(
                    Deserialize<CreateCityCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.UpdateCity => await UpdateCityAsync(
                    Deserialize<UpdateCityCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.DeleteCity => await DeleteCityAsync(
                    Deserialize<DeleteCityCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CreateVehicle => await CreateVehicleAsync(
                    Deserialize<CreateVehicleCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.UpdateVehicle => await UpdateVehicleAsync(
                    Deserialize<UpdateVehicleCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.DeleteVehicle => await DeleteVehicleAsync(
                    Deserialize<DeleteVehicleCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CreateRide => await CreateRideAsync(
                    Deserialize<CreateRideCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CreateReservation => await CreateReservationAsync(
                    Deserialize<CreateReservationCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.UpdateReservation => await UpdateReservationAsync(
                    Deserialize<UpdateReservationCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.DeleteReservation => await DeleteReservationAsync(
                    Deserialize<DeleteReservationCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CreateReview => await CreateReviewAsync(
                    Deserialize<CreateReviewCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.UpdateReview => await UpdateReviewAsync(
                    Deserialize<UpdateReviewCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.DeleteReview => await DeleteReviewAsync(
                    Deserialize<DeleteReviewCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CreatePayment => await CreatePaymentAsync(
                    Deserialize<CreatePaymentCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.UpdatePayment => await UpdatePaymentAsync(
                    Deserialize<UpdatePaymentCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.DeletePayment => await DeletePaymentAsync(
                    Deserialize<DeletePaymentCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CreateUser => await CreateUserAsync(
                    Deserialize<CreateUserCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.UpdateUser => await UpdateUserAsync(
                    Deserialize<UpdateUserCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.DeleteUser => await DeleteUserAsync(
                    Deserialize<DeleteUserCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CreateBalanceTransaction => await CreateBalanceTransactionAsync(
                    Deserialize<CreateBalanceTransactionCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.UpdateRide => await UpdateRideAsync(
                    Deserialize<UpdateRideCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.DeleteRide => await DeleteRideAsync(
                    Deserialize<DeleteRideCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.StartRide => await StartRideAsync(
                    Deserialize<StartRideCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.FinishRide => await FinishRideAsync(
                    Deserialize<FinishRideCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                SideSeatActionTypes.CheckInReservation => await CheckInReservationAsync(
                    Deserialize<CheckInReservationCommand>(action.PayloadJson),
                    principal,
                    cancellationToken),
                _ => CommandResult.Fail(CommandErrorKind.Validation, "Nepoznata vrsta akcije.")
            };

            if (transaction is not null)
            {
                if (result.Succeeded)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();
            result = CommandResult.Fail(
                CommandErrorKind.Conflict,
                "Podaci su se promijenili. Osvježi podatke i pokušaj ponovno.");
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();
            result = CommandResult.Fail(
                CommandErrorKind.Conflict,
                "Upis nije moguć zbog povezanih ili dupliciranih podataka.");
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        await auditService.WriteAsync(
            principal,
            source,
            action.ActionType,
            result.EntityType ?? action.ActionType,
            result.EntityId?.ToString(),
            result.Succeeded,
            result.Message,
            cancellationToken);
        return result;
    }

    private async Task<CommandResult> CreateCityAsync(
        CreateCityCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može kreirati grad.");
        }

        var name = command.Naziv.Trim();
        var country = command.Drzava.Trim();
        var postalCode = command.PostanskiBroj.Trim();
        if (name.Length is < 2 or > 120 || country.Length is < 2 or > 120 ||
            postalCode.Length is < 2 or > 12)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci grada nisu valjani.");
        }

        if (await dbContext.Gradovi.AnyAsync(
                city => city.Naziv == name && city.Drzava == country,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Grad već postoji.");
        }

        var coordinates = await cityGeocoding.ResolveAsync(
            name,
            country,
            postalCode,
            command.Latitude,
            command.Longitude,
            cancellationToken);
        if (!coordinates.Succeeded)
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                coordinates.Error ?? "Koordinate grada nisu dostupne.");
        }

        var city = new Grad
        {
            Naziv = name,
            Drzava = country,
            PostanskiBroj = postalCode,
            Latitude = coordinates.Latitude,
            Longitude = coordinates.Longitude
        };
        dbContext.Gradovi.Add(city);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Grad je kreiran.", "Grad", city.Id, $"/Grad/Details/{city.Id}");
    }

    private async Task<CommandResult> UpdateCityAsync(
        UpdateCityCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može uređivati grad.");
        }

        var city = await dbContext.Gradovi.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (city is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Grad ne postoji.");
        }

        var name = command.Naziv.Trim();
        var country = command.Drzava.Trim();
        var postalCode = command.PostanskiBroj.Trim();
        if (name.Length is < 2 or > 120 || country.Length is < 2 or > 120 ||
            postalCode.Length is < 2 or > 12)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci grada nisu valjani.");
        }

        if (await dbContext.Gradovi.AnyAsync(
                item => item.Id != city.Id && item.Naziv == name && item.Drzava == country,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Grad već postoji.");
        }

        var identityChanged =
            !string.Equals(city.Naziv, name, StringComparison.Ordinal) ||
            !string.Equals(city.Drzava, country, StringComparison.Ordinal) ||
            !string.Equals(city.PostanskiBroj, postalCode, StringComparison.Ordinal);
        var coordinatesChanged =
            city.Latitude != command.Latitude ||
            city.Longitude != command.Longitude;
        var bothCoordinatesOmitted = !command.Latitude.HasValue && !command.Longitude.HasValue;
        var latitude = !identityChanged && bothCoordinatesOmitted
            ? city.Latitude
            : identityChanged && !coordinatesChanged
                ? null
                : command.Latitude;
        var longitude = !identityChanged && bothCoordinatesOmitted
            ? city.Longitude
            : identityChanged && !coordinatesChanged
                ? null
                : command.Longitude;

        var coordinates = await cityGeocoding.ResolveAsync(
            name,
            country,
            postalCode,
            latitude,
            longitude,
            cancellationToken);
        if (!coordinates.Succeeded)
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                coordinates.Error ?? "Koordinate grada nisu dostupne.");
        }

        city.Naziv = name;
        city.Drzava = country;
        city.PostanskiBroj = postalCode;
        city.Latitude = coordinates.Latitude;
        city.Longitude = coordinates.Longitude;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Grad je ažuriran.", "Grad", city.Id, $"/Grad/Details/{city.Id}");
    }

    private async Task<CommandResult> DeleteCityAsync(
        DeleteCityCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može brisati grad.");
        }

        var city = await dbContext.Gradovi.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (city is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Grad ne postoji.");
        }

        var hasRelatedTrips = await dbContext.Voznje.AnyAsync(
            ride => ride.PolazniGradId == city.Id || ride.OdredisniGradId == city.Id,
            cancellationToken);
        if (hasRelatedTrips)
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Grad nije moguće obrisati dok postoje povezane vožnje.");
        }

        dbContext.Gradovi.Remove(city);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Grad je obrisan.", "Grad", command.Id, "/Grad");
    }

    private async Task<CommandResult> CreateVehicleAsync(
        CreateVehicleCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može kreirati vozilo.");
        }

        var registration = command.Registracija.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(command.Marka) ||
            string.IsNullOrWhiteSpace(command.Model) ||
            registration.Length > 24 ||
            command.GodinaProizvodnje is < 1980 or > 2035 ||
            command.BrojSjedala is < 1 or > 8 ||
            command.ProsjecnaPotrosnja is < 0.1m or > 30m)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci vozila nisu valjani.");
        }

        if (await dbContext.Vozila.AnyAsync(
                vehicle => vehicle.Registracija == registration,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Registracija već postoji.");
        }

        if (command.VlasnikId.HasValue &&
            !await dbContext.Korisnici.AnyAsync(
                user => user.Id == command.VlasnikId.Value,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vlasnik ne postoji.");
        }

        var vehicle = new Vozilo
        {
            Marka = command.Marka.Trim(),
            Model = command.Model.Trim(),
            Registracija = registration,
            GodinaProizvodnje = command.GodinaProizvodnje,
            BrojSjedala = command.BrojSjedala,
            Boja = command.Boja.Trim(),
            ProsjecnaPotrosnja = command.ProsjecnaPotrosnja,
            VlasnikId = command.VlasnikId
        };
        dbContext.Vozila.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vozilo je kreirano.", "Vozilo", vehicle.Id, $"/Vozilo/Details/{vehicle.Id}");
    }

    private async Task<CommandResult> UpdateVehicleAsync(
        UpdateVehicleCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može uređivati vozilo.");
        }

        var vehicle = await dbContext.Vozila.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (vehicle is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vozilo ne postoji.");
        }

        var registration = command.Registracija.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(command.Marka) ||
            string.IsNullOrWhiteSpace(command.Model) ||
            registration.Length is < 2 or > 24 ||
            command.GodinaProizvodnje is < 1980 or > 2035 ||
            command.BrojSjedala is < 1 or > 8 ||
            command.ProsjecnaPotrosnja is < 0.1m or > 30m)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci vozila nisu valjani.");
        }

        if (await dbContext.Vozila.AnyAsync(
                item => item.Id != vehicle.Id && item.Registracija == registration,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Registracija već postoji.");
        }

        if (command.VlasnikId.HasValue &&
            !await dbContext.Korisnici.AnyAsync(
                user => user.Id == command.VlasnikId.Value,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vlasnik ne postoji.");
        }

        vehicle.Marka = command.Marka.Trim();
        vehicle.Model = command.Model.Trim();
        vehicle.Registracija = registration;
        vehicle.GodinaProizvodnje = command.GodinaProizvodnje;
        vehicle.BrojSjedala = command.BrojSjedala;
        vehicle.Boja = command.Boja.Trim();
        vehicle.ProsjecnaPotrosnja = command.ProsjecnaPotrosnja;
        vehicle.VlasnikId = command.VlasnikId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vozilo je ažurirano.", "Vozilo", vehicle.Id, $"/Vozilo/Details/{vehicle.Id}");
    }

    private async Task<CommandResult> DeleteVehicleAsync(
        DeleteVehicleCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može brisati vozilo.");
        }

        var vehicle = await dbContext.Vozila.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (vehicle is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vozilo ne postoji.");
        }

        var linkedUsers = await dbContext.Korisnici
            .Where(user => user.VoziloId == vehicle.Id)
            .ToListAsync(cancellationToken);
        foreach (var user in linkedUsers)
        {
            user.VoziloId = null;
        }

        dbContext.Vozila.Remove(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vozilo je obrisano.", "Vozilo", command.Id, "/Vozilo");
    }

    private async Task<CommandResult> CreateRideAsync(
        CreateRideCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        if (currentUserId is null ||
            (!principal.IsInRole("Admin") &&
             (!principal.IsInRole("Driver") || currentUserId.Value != command.VozacId)))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo kreirati ovu vožnju.");
        }

        if (command.PolazniGradId == command.OdredisniGradId ||
            command.OcekivaniDolazak <= command.Polazak ||
            command.CijenaPoMjestu <= 0 ||
            command.UkupnoMjesta is < 1 or > 20 ||
            command.SlobodnaMjesta < 0 ||
            command.SlobodnaMjesta > command.UkupnoMjesta)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci vožnje nisu valjani.");
        }

        var refsExist =
            await dbContext.Korisnici.AnyAsync(user => user.Id == command.VozacId, cancellationToken) &&
            await dbContext.Gradovi.AnyAsync(city => city.Id == command.PolazniGradId, cancellationToken) &&
            await dbContext.Gradovi.AnyAsync(city => city.Id == command.OdredisniGradId, cancellationToken);
        if (!refsExist)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vozač ili grad ne postoji.");
        }

        if (await HasDriverSpacingConflictAsync(
                command.VozacId,
                command.Polazak,
                null,
                cancellationToken))
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Vozač već ima neotkazanu vožnju unutar 12 sati od ovog polaska.");
        }

        var ride = new Voznja
        {
            VozacId = command.VozacId,
            PolazniGradId = command.PolazniGradId,
            OdredisniGradId = command.OdredisniGradId,
            Polazak = command.Polazak,
            OcekivaniDolazak = command.OcekivaniDolazak,
            CijenaPoMjestu = command.CijenaPoMjestu,
            UkupnoMjesta = command.UkupnoMjesta,
            SlobodnaMjesta = command.SlobodnaMjesta,
            Opis = command.Opis.Trim(),
            Status = StatusVoznje.Planirana
        };
        dbContext.Voznje.Add(ride);
        await dbContext.SaveChangesAsync(cancellationToken);
        notifications.Add(
            ride.VozacId,
            "Vožnja kreirana",
            $"Vožnja #{ride.Id} je uspješno kreirana.",
            "Vožnja",
            $"/Voznja/Details/{ride.Id}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vožnja je kreirana.", "Voznja", ride.Id, $"/Voznja/Details/{ride.Id}");
    }

    private async Task<CommandResult> CreateReservationAsync(
        CreateReservationCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        if (currentUserId is null)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Korisnik nije prijavljen.");
        }

        if (command.BrojMjesta is < 1 or > 10 ||
            command.NacinPlacanja is not (NacinPlacanja.SideSeatSaldo or NacinPlacanja.Gotovina))
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci rezervacije nisu valjani.");
        }

        if (command.Napojnica != 0)
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Napojnica se dodaje pri ocjenjivanju vozača karticom.");
        }

        var ride = await dbContext.Voznje
            .FirstOrDefaultAsync(item => item.Id == command.VoznjaId, cancellationToken);
        var user = await dbContext.Korisnici
            .FirstOrDefaultAsync(item => item.Id == currentUserId.Value, cancellationToken);
        if (ride is null || user is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vožnja ili korisnik ne postoji.");
        }

        if (ride.Status != StatusVoznje.Planirana || ride.VozacId == currentUserId.Value)
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Ovu vožnju nije moguće rezervirati.");
        }

        if (await dbContext.Rezervacije.AnyAsync(
                reservation =>
                    reservation.VoznjaId == ride.Id &&
                    reservation.PutnikId == currentUserId.Value &&
                    reservation.Status != StatusRezervacije.Odbijena,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Već imaš aktivnu rezervaciju za ovu vožnju.");
        }

        var price = ride.CijenaPoMjestu * command.BrojMjesta;
        if (await HasPassengerReservationConflictAsync(
                currentUserId.Value,
                ride.Id,
                ride.Polazak,
                ride.OcekivaniDolazak,
                null,
                cancellationToken))
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Rezervacija se preklapa s postojećom vožnjom ili nema najmanje 1 sat razmaka.");
        }

        if (command.NacinPlacanja == NacinPlacanja.SideSeatSaldo)
        {
            var committed = await GetCommittedBalanceAsync(currentUserId.Value, cancellationToken);
            if (user.Saldo < committed + price)
            {
                return CommandResult.Fail(CommandErrorKind.BusinessRule, "Nema dovoljno raspoloživog salda.");
            }
        }

        if (command.BrojMjesta > ride.SlobodnaMjesta)
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Nema dovoljno slobodnih mjesta.");
        }

        var reservation = new Rezervacija
        {
            VoznjaId = ride.Id,
            PutnikId = currentUserId.Value,
            BrojMjesta = command.BrojMjesta,
            CijenaUkupno = price,
            VrijemeRezervacije = DateTime.UtcNow,
            Status = StatusRezervacije.UProcesuPotvrde,
            NacinPlacanja = command.NacinPlacanja,
            Napojnica = 0,
            Napomena = command.Napomena.Trim()
        };
        dbContext.Rezervacije.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        notifications.Add(
            ride.VozacId,
            "Nova rezervacija",
            $"Nova rezervacija #{reservation.Id} čeka tvoju potvrdu.",
            "Rezervacija",
            $"/Voznja/Details/{ride.Id}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success(
            "Rezervacija je kreirana.",
            "Rezervacija",
            reservation.Id,
            $"/Rezervacija/Details/{reservation.Id}");
    }

    private async Task<CommandResult> UpdateReservationAsync(
        UpdateReservationCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može uređivati rezervaciju.");
        }

        if (command.BrojMjesta is < 1 or > 10 ||
            command.Napojnica < 0 ||
            command.Napojnica > 10000 ||
            command.NacinPlacanja is not (NacinPlacanja.SideSeatSaldo or NacinPlacanja.Gotovina))
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci rezervacije nisu valjani.");
        }

        var reservation = await dbContext.Rezervacije
            .Include(item => item.Voznja)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (reservation is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Rezervacija ne postoji.");
        }

        if (command.Napojnica != reservation.Napojnica)
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Napojnica se dodaje pri ocjenjivanju vozača karticom i ne uređuje se na rezervaciji.");
        }

        var ride = reservation.VoznjaId == command.VoznjaId
            ? reservation.Voznja
            : await dbContext.Voznje.FirstOrDefaultAsync(item => item.Id == command.VoznjaId, cancellationToken);
        var passenger = await dbContext.Korisnici.FirstOrDefaultAsync(
            item => item.Id == command.PutnikId,
            cancellationToken);
        if (ride is null || passenger is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vožnja ili putnik ne postoji.");
        }

        if (ride.Status == StatusVoznje.Otkazana ||
            (ride.Status == StatusVoznje.Zavrsena && command.Status != StatusRezervacije.Zavrsena))
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Rezervaciju nije moguće premjestiti na ovu vožnju.");
        }

        if (ride.VozacId == command.PutnikId)
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Vozač ne može biti putnik vlastite vožnje.");
        }

        if (await HasPassengerReservationConflictAsync(
                command.PutnikId,
                ride.Id,
                ride.Polazak,
                ride.OcekivaniDolazak,
                reservation.Id,
                cancellationToken))
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Rezervacija se preklapa s postojećom vožnjom ili nema najmanje 1 sat razmaka.");
        }

        var oldReservedSeats = UsesRideCapacity(reservation.Status) ? reservation.BrojMjesta : 0;
        var newReservedSeats = UsesRideCapacity(command.Status) ? command.BrojMjesta : 0;
        if (reservation.VoznjaId == command.VoznjaId)
        {
            var delta = newReservedSeats - oldReservedSeats;
            if (delta > 0 && reservation.Voznja.SlobodnaMjesta < delta)
            {
                return CommandResult.Fail(CommandErrorKind.BusinessRule, "Nema dovoljno slobodnih mjesta.");
            }

            reservation.Voznja.SlobodnaMjesta -= delta;
        }
        else
        {
            if (ride.SlobodnaMjesta < newReservedSeats)
            {
                return CommandResult.Fail(CommandErrorKind.BusinessRule, "Nema dovoljno slobodnih mjesta na novoj vožnji.");
            }

            reservation.Voznja.SlobodnaMjesta += oldReservedSeats;
            ride.SlobodnaMjesta -= newReservedSeats;
            reservation.VoznjaId = ride.Id;
            reservation.Voznja = ride;
        }

        var total = ride.CijenaPoMjestu * command.BrojMjesta;
        if (command.NacinPlacanja == NacinPlacanja.SideSeatSaldo &&
            command.Status is (StatusRezervacije.UProcesuPotvrde or StatusRezervacije.Potvrdena))
        {
            var currentReservationCommit = reservation.NacinPlacanja == NacinPlacanja.SideSeatSaldo &&
                                           reservation.Status is (StatusRezervacije.UProcesuPotvrde or StatusRezervacije.Potvrdena)
                ? reservation.CijenaUkupno
                : 0;
            var committed = await GetCommittedBalanceAsync(command.PutnikId, cancellationToken) - currentReservationCommit;
            if (passenger.Saldo < committed + total)
            {
                return CommandResult.Fail(CommandErrorKind.BusinessRule, "Putnik nema dovoljno raspoloživog salda.");
            }
        }

        reservation.PutnikId = command.PutnikId;
        reservation.BrojMjesta = command.BrojMjesta;
        reservation.CijenaUkupno = total;
        reservation.Status = command.Status;
        reservation.NacinPlacanja = command.NacinPlacanja;
        reservation.Napojnica = command.Napojnica;
        reservation.Napomena = command.Napomena.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success(
            "Rezervacija je ažurirana.",
            "Rezervacija",
            reservation.Id,
            $"/Rezervacija/Details/{reservation.Id}");
    }

    private async Task<CommandResult> DeleteReservationAsync(
        DeleteReservationCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može brisati rezervaciju.");
        }

        var reservation = await dbContext.Rezervacije
            .Include(item => item.Voznja)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (reservation is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Rezervacija ne postoji.");
        }

        var reviews = await dbContext.Ocjene
            .Where(item => item.RezervacijaId == reservation.Id)
            .ToListAsync(cancellationToken);
        var payments = await dbContext.Placanja
            .Where(item => item.RezervacijaId == reservation.Id)
            .ToListAsync(cancellationToken);

        if (UsesRideCapacity(reservation.Status))
        {
            reservation.Voznja.SlobodnaMjesta += reservation.BrojMjesta;
        }

        dbContext.Ocjene.RemoveRange(reviews);
        dbContext.Placanja.RemoveRange(payments);
        dbContext.Rezervacije.Remove(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Rezervacija je obrisana.", "Rezervacija", command.Id, "/Rezervacija");
    }

    private async Task<CommandResult> CreateReviewAsync(
        CreateReviewCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        if (currentUserId is null)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Korisnik nije prijavljen.");
        }

        if (command.BrojZvjezdica is < 1 or > 5 ||
            string.IsNullOrWhiteSpace(command.Komentar) ||
            command.Komentar.Length > 500)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Ocjena ili komentar nisu valjani.");
        }

        var reservation = await dbContext.Rezervacije
            .Include(item => item.Voznja)
            .FirstOrDefaultAsync(item => item.Id == command.RezervacijaId, cancellationToken);
        if (reservation is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Rezervacija ne postoji.");
        }

        var canReview = reservation.Status == StatusRezervacije.Zavrsena &&
            (reservation.PutnikId == currentUserId.Value ||
             reservation.Voznja.VozacId == currentUserId.Value);
        if (!canReview)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo ocijeniti ovu rezervaciju.");
        }

        if (await dbContext.Ocjene.AnyAsync(
                review =>
                    review.RezervacijaId == reservation.Id &&
                    review.AutorId == currentUserId.Value,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Već si ocijenio ovu rezervaciju.");
        }

        var review = new OcjenaVoznje
        {
            RezervacijaId = reservation.Id,
            AutorId = currentUserId.Value,
            BrojZvjezdica = command.BrojZvjezdica,
            Komentar = command.Komentar.Trim(),
            Kreirano = DateTime.UtcNow
        };
        dbContext.Ocjene.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Ocjena je kreirana.", "Ocjena", review.Id, $"/Ocjena/Details/{review.Id}");
    }

    private async Task<CommandResult> UpdateReviewAsync(
        UpdateReviewCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        if (currentUserId is null)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Korisnik nije prijavljen.");
        }

        if (command.BrojZvjezdica is < 1 or > 5 ||
            string.IsNullOrWhiteSpace(command.Komentar) ||
            command.Komentar.Length > 500)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Ocjena ili komentar nisu valjani.");
        }

        var review = await dbContext.Ocjene
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (review is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Ocjena ne postoji.");
        }

        var isAdmin = principal.IsInRole("Admin");
        if (!isAdmin && review.AutorId != currentUserId.Value)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo uređivati ovu ocjenu.");
        }

        var reservationId = isAdmin ? command.RezervacijaId : review.RezervacijaId;
        var authorId = isAdmin ? command.AutorId : review.AutorId;
        var reservation = await dbContext.Rezervacije
            .Include(item => item.Voznja)
            .FirstOrDefaultAsync(item => item.Id == reservationId, cancellationToken);
        if (reservation is null ||
            !await dbContext.Korisnici.AnyAsync(item => item.Id == authorId, cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Rezervacija ili autor ne postoji.");
        }

        var canReview = reservation.Status == StatusRezervacije.Zavrsena &&
            reservation.Voznja.Status == StatusVoznje.Zavrsena &&
            (reservation.PutnikId == authorId || reservation.Voznja.VozacId == authorId);
        if (!isAdmin && !canReview)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo ocijeniti ovu rezervaciju.");
        }

        if (await dbContext.Ocjene.AnyAsync(
                item => item.Id != review.Id &&
                        item.RezervacijaId == reservationId &&
                        item.AutorId == authorId,
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Autor već ima ocjenu za ovu rezervaciju.");
        }

        review.RezervacijaId = reservationId;
        review.AutorId = authorId;
        review.BrojZvjezdica = command.BrojZvjezdica;
        review.Komentar = command.Komentar.Trim();
        review.Uredeno = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Ocjena je ažurirana.", "Ocjena", review.Id, $"/Ocjena/Details/{review.Id}");
    }

    private async Task<CommandResult> DeleteReviewAsync(
        DeleteReviewCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        if (currentUserId is null)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Korisnik nije prijavljen.");
        }

        var review = await dbContext.Ocjene
            .Include(item => item.Slike)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (review is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Ocjena ne postoji.");
        }

        if (!principal.IsInRole("Admin") && review.AutorId != currentUserId.Value)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo obrisati ovu ocjenu.");
        }

        dbContext.OcjenaSlike.RemoveRange(review.Slike);
        dbContext.Ocjene.Remove(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Ocjena je obrisana.", "Ocjena", command.Id, "/Ocjena");
    }

    private async Task<CommandResult> CreatePaymentAsync(
        CreatePaymentCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može evidentirati plaćanje.");
        }

        if (command.Iznos <= 0)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Iznos plaćanja mora biti veći od 0.");
        }

        var reservation = await dbContext.Rezervacije
            .FirstOrDefaultAsync(item => item.Id == command.RezervacijaId, cancellationToken);
        if (reservation is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Rezervacija ne postoji.");
        }

        var payment = new Placanje
        {
            RezervacijaId = reservation.Id,
            Iznos = command.Iznos,
            VrijemePlacanja = command.VrijemePlacanja == default ? DateTime.UtcNow : command.VrijemePlacanja,
            NacinPlacanja = command.NacinPlacanja,
            Uspjesno = command.Uspjesno
        };
        dbContext.Placanja.Add(payment);
        notifications.Add(
            reservation.PutnikId,
            command.Uspjesno ? "Plaćanje evidentirano" : "Plaćanje nije uspjelo",
            $"Plaćanje za rezervaciju #{reservation.Id}: {command.Iznos:0.00} EUR.",
            "Naplata",
            $"/Rezervacija/Details/{reservation.Id}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Plaćanje je kreirano.", "Placanje", payment.Id, $"/Placanje/Details/{payment.Id}");
    }

    private async Task<CommandResult> UpdatePaymentAsync(
        UpdatePaymentCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može uređivati plaćanje.");
        }

        if (command.Iznos <= 0)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Iznos plaćanja mora biti veći od 0.");
        }

        var payment = await dbContext.Placanja.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (payment is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Plaćanje ne postoji.");
        }

        if (!await dbContext.Rezervacije.AnyAsync(item => item.Id == command.RezervacijaId, cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Rezervacija ne postoji.");
        }

        payment.RezervacijaId = command.RezervacijaId;
        payment.Iznos = command.Iznos;
        payment.VrijemePlacanja = command.VrijemePlacanja == default ? payment.VrijemePlacanja : command.VrijemePlacanja;
        payment.NacinPlacanja = command.NacinPlacanja;
        payment.Uspjesno = command.Uspjesno;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Plaćanje je ažurirano.", "Placanje", payment.Id, $"/Placanje/Details/{payment.Id}");
    }

    private async Task<CommandResult> DeletePaymentAsync(
        DeletePaymentCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može brisati plaćanje.");
        }

        var payment = await dbContext.Placanja.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (payment is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Plaćanje ne postoji.");
        }

        dbContext.Placanja.Remove(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Plaćanje je obrisano.", "Placanje", command.Id, "/Placanje");
    }

    private async Task<CommandResult> CreateUserAsync(
        CreateUserCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može kreirati korisnika.");
        }

        var validation = await ValidateUserCommandAsync(
            null,
            command.Ime,
            command.Prezime,
            command.Email,
            command.Adresa,
            command.BrojMobitela,
            command.KycOib,
            command.VoziloId,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Trim().Length < 6)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Lozinka mora imati najmanje 6 znakova.");
        }

        var user = new Korisnik
        {
            Ime = command.Ime.Trim(),
            Prezime = command.Prezime.Trim(),
            Email = command.Email.Trim(),
            Adresa = command.Adresa.Trim(),
            BrojMobitela = command.BrojMobitela.Trim(),
            DatumRegistracije = DateTime.UtcNow,
            Tip = command.Tip,
            JeAktivan = command.JeAktivan,
            KycPodnesen = command.KycPodnesen,
            KycOib = string.IsNullOrWhiteSpace(command.KycOib) ? null : command.KycOib.Trim(),
            KycBrojOsobne = string.IsNullOrWhiteSpace(command.KycBrojOsobne) ? null : command.KycBrojOsobne.Trim(),
            KycBrojVozacke = string.IsNullOrWhiteSpace(command.KycBrojVozacke) ? null : command.KycBrojVozacke.Trim(),
            KycDatumRodenja = command.KycDatumRodenja,
            VoziloId = command.VoziloId,
            LozinkaHash = passwordHashing.Hash(command.Password.Trim())
        };
        dbContext.Korisnici.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Korisnik je kreiran.", "Korisnik", user.Id, $"/Korisnik/Details/{user.Id}");
    }

    private async Task<CommandResult> UpdateUserAsync(
        UpdateUserCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može uređivati korisnika.");
        }

        var user = await dbContext.Korisnici.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (user is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Korisnik ne postoji.");
        }

        var validation = await ValidateUserCommandAsync(
            user.Id,
            command.Ime,
            command.Prezime,
            command.Email,
            command.Adresa,
            command.BrojMobitela,
            command.KycOib,
            command.VoziloId,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        user.Ime = command.Ime.Trim();
        user.Prezime = command.Prezime.Trim();
        user.Email = command.Email.Trim();
        user.Adresa = command.Adresa.Trim();
        user.BrojMobitela = command.BrojMobitela.Trim();
        user.Tip = command.Tip;
        user.JeAktivan = command.JeAktivan;
        user.KycPodnesen = command.KycPodnesen;
        user.KycOib = string.IsNullOrWhiteSpace(command.KycOib) ? null : command.KycOib.Trim();
        user.KycBrojOsobne = string.IsNullOrWhiteSpace(command.KycBrojOsobne) ? null : command.KycBrojOsobne.Trim();
        user.KycBrojVozacke = string.IsNullOrWhiteSpace(command.KycBrojVozacke) ? null : command.KycBrojVozacke.Trim();
        user.KycDatumRodenja = command.KycDatumRodenja;
        user.VoziloId = command.VoziloId;
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            if (command.Password.Trim().Length < 6)
            {
                return CommandResult.Fail(CommandErrorKind.Validation, "Lozinka mora imati najmanje 6 znakova.");
            }

            user.LozinkaHash = passwordHashing.Hash(command.Password.Trim());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Korisnik je ažuriran.", "Korisnik", user.Id, $"/Korisnik/Details/{user.Id}");
    }

    private async Task<CommandResult> DeleteUserAsync(
        DeleteUserCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!principal.IsInRole("Admin"))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može brisati korisnika.");
        }

        var currentUserId = principal.GetKorisnikId();
        if (currentUserId == command.Id)
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Ne možeš obrisati vlastiti korisnički račun.");
        }

        var user = await dbContext.Korisnici.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (user is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Korisnik ne postoji.");
        }

        var hasTrips = await dbContext.Voznje.AnyAsync(item => item.VozacId == user.Id, cancellationToken);
        var hasReservations = await dbContext.Rezervacije.AnyAsync(item => item.PutnikId == user.Id, cancellationToken);
        if (hasTrips || hasReservations)
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Korisnik ima povezane vožnje ili rezervacije i ne može se obrisati.");
        }

        dbContext.Korisnici.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Korisnik je obrisan.", "Korisnik", command.Id, "/Korisnik");
    }

    private async Task<CommandResult> CreateBalanceTransactionAsync(
        CreateBalanceTransactionCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        if (currentUserId is null)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Korisnik nije prijavljen.");
        }

        var targetUserId = principal.IsInRole("Admin")
            ? command.KorisnikId ?? currentUserId.Value
            : currentUserId.Value;
        if (!principal.IsInRole("Admin") && command.KorisnikId.HasValue && command.KorisnikId != currentUserId.Value)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Ne možeš mijenjati tuđi saldo.");
        }

        if (command.Iznos <= 0 || command.Iznos > 1_000_000)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Iznos saldo transakcije nije valjan.");
        }

        var type = command.Tip.Trim().ToLowerInvariant();
        if (type is not ("uplata" or "isplata" or "korekcija"))
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Tip transakcije mora biti uplata, isplata ili korekcija.");
        }

        var user = await dbContext.Korisnici.FirstOrDefaultAsync(item => item.Id == targetUserId, cancellationToken);
        if (user is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Korisnik ne postoji.");
        }

        var before = user.Saldo;
        if (type == "isplata")
        {
            var committed = await GetCommittedBalanceAsync(user.Id, cancellationToken);
            if (user.Saldo - committed < command.Iznos)
            {
                return CommandResult.Fail(CommandErrorKind.BusinessRule, "Nema dovoljno raspoloživog salda za isplatu.");
            }

            user.Saldo -= command.Iznos;
        }
        else
        {
            user.Saldo += command.Iznos;
        }

        dbContext.SaldoTransakcije.Add(new SaldoTransakcija
        {
            KorisnikId = user.Id,
            Iznos = command.Iznos,
            Tip = type,
            Komentar = command.Komentar.Trim(),
            SaldoPrije = before,
            SaldoPoslije = user.Saldo,
            Vrijeme = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Saldo transakcija je kreirana.", "SaldoTransakcija", user.Id, $"/Korisnik/Details/{user.Id}");
    }

    private async Task<CommandResult> UpdateRideAsync(
        UpdateRideCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        var ride = await dbContext.Voznje
            .Include(item => item.Rezervacije)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (ride is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vožnja ne postoji.");
        }

        if (currentUserId is null ||
            (!principal.IsInRole("Admin") && ride.VozacId != currentUserId.Value))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo uređivati ovu vožnju.");
        }

        if (command.PolazniGradId == command.OdredisniGradId ||
            command.OcekivaniDolazak <= command.Polazak ||
            command.CijenaPoMjestu <= 0 ||
            command.UkupnoMjesta is < 1 or > 20 ||
            command.SlobodnaMjesta < 0 ||
            command.SlobodnaMjesta > command.UkupnoMjesta)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci vožnje nisu valjani.");
        }

        var driverId = principal.IsInRole("Admin") ? command.VozacId : ride.VozacId;
        var refsExist =
            await dbContext.Korisnici.AnyAsync(user => user.Id == driverId, cancellationToken) &&
            await dbContext.Gradovi.AnyAsync(city => city.Id == command.PolazniGradId, cancellationToken) &&
            await dbContext.Gradovi.AnyAsync(city => city.Id == command.OdredisniGradId, cancellationToken);
        if (!refsExist)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vozač ili grad ne postoji.");
        }

        if (await HasDriverSpacingConflictAsync(driverId, command.Polazak, ride.Id, cancellationToken))
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Vozač već ima neotkazanu vožnju unutar 12 sati od ovog polaska.");
        }

        ride.VozacId = driverId;
        ride.PolazniGradId = command.PolazniGradId;
        ride.OdredisniGradId = command.OdredisniGradId;
        ride.Polazak = command.Polazak;
        ride.OcekivaniDolazak = command.OcekivaniDolazak;
        ride.CijenaPoMjestu = command.CijenaPoMjestu;
        ride.UkupnoMjesta = command.UkupnoMjesta;
        ride.SlobodnaMjesta = command.SlobodnaMjesta;
        ride.Opis = command.Opis.Trim();
        if (principal.IsInRole("Admin"))
        {
            ride.Status = command.Status;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vožnja je ažurirana.", "Voznja", ride.Id, $"/Voznja/Details/{ride.Id}");
    }

    private async Task<CommandResult> DeleteRideAsync(
        DeleteRideCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        var ride = await dbContext.Voznje
            .Include(item => item.Rezervacije)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (ride is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vožnja ne postoji.");
        }

        if (currentUserId is null ||
            (!principal.IsInRole("Admin") && ride.VozacId != currentUserId.Value))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo obrisati ovu vožnju.");
        }

        if (!principal.IsInRole("Admin") && ride.Status == StatusVoznje.Zavrsena)
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Samo administrator može obrisati završenu vožnju.");
        }

        if (ride.Rezervacije.Any())
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Vožnja ima rezervacije i ne može se obrisati.");
        }

        dbContext.Voznje.Remove(ride);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vožnja je obrisana.", "Voznja", command.Id, "/Voznja");
    }

    private async Task<CommandResult> StartRideAsync(
        StartRideCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        var ride = await dbContext.Voznje
            .Include(item => item.Rezervacije)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (ride is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vožnja ne postoji.");
        }

        if (currentUserId is null ||
            (!principal.IsInRole("Admin") && ride.VozacId != currentUserId.Value))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo pokrenuti ovu vožnju.");
        }

        if (ride.Status != StatusVoznje.Planirana)
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Samo planirana vožnja može se pokrenuti.");
        }

        var nowStart = DateTime.Now;
        if (nowStart < ride.Polazak.AddMinutes(-30) || nowStart > ride.Polazak.AddHours(12))
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Vožnja se može pokrenuti od 30 min prije polaska do 12 h nakon zakazanog termina.");
        }

        var confirmed = ride.Rezervacije
            .Where(reservation => reservation.Status == StatusRezervacije.Potvrdena)
            .ToList();
        if (confirmed.Count == 0)
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Vožnja nema potvrđenih putnika.");
        }

        if (confirmed.Any(reservation => reservation.CheckInAtUtc is null))
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Svi potvrđeni putnici moraju označiti da su u autu.");
        }

        ride.Status = StatusVoznje.Aktivna;
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vožnja je pokrenuta.", "Voznja", ride.Id, $"/Voznja/Current");
    }

    private async Task<CommandResult> FinishRideAsync(
        FinishRideCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        var ride = await dbContext.Voznje
            .Include(item => item.Rezervacije)
            .ThenInclude(item => item.Putnik)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (ride is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vožnja ne postoji.");
        }

        if (currentUserId is null ||
            (!principal.IsInRole("Admin") && ride.VozacId != currentUserId.Value))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo završiti ovu vožnju.");
        }

        if (ride.Status is not (StatusVoznje.Planirana or StatusVoznje.Aktivna))
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Vožnju nije moguće završiti u trenutnom statusu.");
        }

        if (ride.Rezervacije.Any(reservation => reservation.Status == StatusRezervacije.UProcesuPotvrde))
        {
            return CommandResult.Fail(
                CommandErrorKind.BusinessRule,
                "Prije završetka potvrdi ili odbij sve rezervacije u procesu potvrde.");
        }

        if (!command.CashCollected &&
            ride.Rezervacije.Any(reservation =>
                reservation.Status == StatusRezervacije.Potvrdena &&
                reservation.NacinPlacanja == NacinPlacanja.Gotovina))
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Prvo potvrdi da je gotovina primljena.");
        }

        var driver = await dbContext.Korisnici.FirstOrDefaultAsync(item => item.Id == ride.VozacId, cancellationToken);
        if (driver is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vozač ne postoji.");
        }

        var nowUtc = DateTime.UtcNow;
        foreach (var reservation in ride.Rezervacije.Where(item => item.Status == StatusRezervacije.Potvrdena))
        {
            var total = reservation.CijenaUkupno;
            if (reservation.NacinPlacanja == NacinPlacanja.SideSeatSaldo)
            {
                var passengerSettlementTip = $"naplata-rezervacije:{reservation.Id}";
                var driverSettlementTip = $"priljev-voznja:{reservation.Id}";
                var passengerSettled = await dbContext.SaldoTransakcije
                    .AnyAsync(t => t.KorisnikId == reservation.PutnikId && t.Tip == passengerSettlementTip, cancellationToken);
                var driverSettled = await dbContext.SaldoTransakcije
                    .AnyAsync(t => t.KorisnikId == driver.Id && t.Tip == driverSettlementTip, cancellationToken);

                if (!passengerSettled)
                {
                    if (reservation.Putnik.Saldo < total)
                    {
                        return CommandResult.Fail(
                            CommandErrorKind.BusinessRule,
                            $"Putnik #{reservation.PutnikId} nema dovoljno salda za završnu naplatu.");
                    }

                    var saldoPrije = reservation.Putnik.Saldo;
                    reservation.Putnik.Saldo -= total;
                    dbContext.SaldoTransakcije.Add(new SaldoTransakcija
                    {
                        KorisnikId = reservation.PutnikId,
                        Iznos = total,
                        Tip = passengerSettlementTip,
                        SaldoPrije = saldoPrije,
                        SaldoPoslije = reservation.Putnik.Saldo,
                        Vrijeme = nowUtc
                    });
                }

                if (!driverSettled)
                {
                    var vozacSaldoPrije = driver.Saldo;
                    driver.Saldo += total;
                    dbContext.SaldoTransakcije.Add(new SaldoTransakcija
                    {
                        KorisnikId = driver.Id,
                        Iznos = total,
                        Tip = driverSettlementTip,
                        SaldoPrije = vozacSaldoPrije,
                        SaldoPoslije = driver.Saldo,
                        Vrijeme = nowUtc
                    });
                }

                await EnsurePaymentAsync(reservation.Id, total, NacinPlacanja.SideSeatSaldo, nowUtc, cancellationToken);
            }
            else
            {
                reservation.CashCollectedAtUtc ??= nowUtc;
                await EnsurePaymentAsync(reservation.Id, total, NacinPlacanja.Gotovina, nowUtc, cancellationToken);
            }

            reservation.Status = StatusRezervacije.Zavrsena;
            notifications.Add(
                reservation.PutnikId,
                "Vožnja završena",
                $"Vožnja #{ride.Id} je završena. Ukupno: {total:0.00} EUR.",
                "Vožnja",
                $"/Rezervacija/Details/{reservation.Id}");
        }

        ride.Status = StatusVoznje.Zavrsena;
        notifications.Add(
            ride.VozacId,
            "Vožnja završena",
            $"Vožnja #{ride.Id} je završena.",
            "Vožnja",
            $"/Voznja/Details/{ride.Id}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Vožnja je završena.", "Voznja", ride.Id, $"/Voznja/Details/{ride.Id}");
    }

    private async Task<CommandResult> CheckInReservationAsync(
        CheckInReservationCommand command,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var currentUserId = principal.GetKorisnikId();
        var reservation = await dbContext.Rezervacije
            .Include(item => item.Voznja)
            .FirstOrDefaultAsync(item => item.Id == command.RezervacijaId, cancellationToken);
        if (reservation is null)
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Rezervacija ne postoji.");
        }

        if (currentUserId is null ||
            (!principal.IsInRole("Admin") && reservation.PutnikId != currentUserId.Value))
        {
            return CommandResult.Fail(CommandErrorKind.Forbidden, "Nemaš pravo potvrditi ovu rezervaciju.");
        }

        if (reservation.Status != StatusRezervacije.Potvrdena ||
            reservation.Voznja.Status is not (StatusVoznje.Planirana or StatusVoznje.Aktivna))
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Samo potvrđena aktivna rezervacija može imati check-in.");
        }

        if (DateTime.Now < reservation.Voznja.Polazak.AddMinutes(-30))
        {
            return CommandResult.Fail(CommandErrorKind.BusinessRule, "Check-in je dostupan 30 minuta prije polaska.");
        }

        if (command.Latitude is < -90 or > 90 || command.Longitude is < -180 or > 180)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Lokacija nije valjana.");
        }

        reservation.CheckInAtUtc = DateTime.UtcNow;
        if (command.Latitude.HasValue && command.Longitude.HasValue)
        {
            reservation.LastLatitude = command.Latitude;
            reservation.LastLongitude = command.Longitude;
            reservation.LastLocationAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return CommandResult.Success("Check-in je potvrđen.", "Rezervacija", reservation.Id, $"/Rezervacija/Details/{reservation.Id}");
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException("Akcija nema valjane podatke.");

    private async Task<bool> HasDriverSpacingConflictAsync(
        int vozacId,
        DateTime polazak,
        int? excludeRideId,
        CancellationToken cancellationToken) =>
        await dbContext.Voznje.AnyAsync(ride =>
            ride.VozacId == vozacId &&
            ride.Status != StatusVoznje.Otkazana &&
            (!excludeRideId.HasValue || ride.Id != excludeRideId.Value) &&
            ride.Polazak >= polazak.AddHours(-12) &&
            ride.Polazak <= polazak.AddHours(12),
            cancellationToken);

    private async Task<bool> HasPassengerReservationConflictAsync(
        int putnikId,
        int voznjaId,
        DateTime polazak,
        DateTime ocekivaniDolazak,
        int? excludeReservationId,
        CancellationToken cancellationToken)
    {
        var startWithBuffer = polazak.AddHours(-1);
        var endWithBuffer = ocekivaniDolazak.AddHours(1);
        return await dbContext.Rezervacije.AnyAsync(reservation =>
            reservation.PutnikId == putnikId &&
            reservation.VoznjaId != voznjaId &&
            (!excludeReservationId.HasValue || reservation.Id != excludeReservationId.Value) &&
            reservation.Status != StatusRezervacije.Odbijena &&
            reservation.Voznja.Status != StatusVoznje.Otkazana &&
            reservation.Voznja.Polazak < endWithBuffer &&
            reservation.Voznja.OcekivaniDolazak > startWithBuffer,
            cancellationToken);
    }

    private async Task<decimal> GetCommittedBalanceAsync(int korisnikId, CancellationToken cancellationToken) =>
        await dbContext.Rezervacije
            .Where(reservation =>
                reservation.PutnikId == korisnikId &&
                reservation.NacinPlacanja == NacinPlacanja.SideSeatSaldo &&
                reservation.Voznja.Status == StatusVoznje.Planirana &&
                (reservation.Status == StatusRezervacije.UProcesuPotvrde ||
                 reservation.Status == StatusRezervacije.Potvrdena))
            .SumAsync(reservation => (decimal?)reservation.CijenaUkupno, cancellationToken) ?? 0;

    private async Task EnsurePaymentAsync(
        int reservationId,
        decimal total,
        NacinPlacanja paymentMethod,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var alreadyCharged = await dbContext.Placanja
            .AnyAsync(p => p.RezervacijaId == reservationId && p.Uspjesno, cancellationToken);
        if (alreadyCharged)
        {
            return;
        }

        dbContext.Placanja.Add(new Placanje
        {
            RezervacijaId = reservationId,
            Iznos = total,
            NacinPlacanja = paymentMethod,
            Uspjesno = true,
            VrijemePlacanja = nowUtc
        });
    }

    private async Task<CommandResult?> ValidateUserCommandAsync(
        int? userId,
        string firstName,
        string lastName,
        string email,
        string address,
        string phone,
        string? oib,
        int? vehicleId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();
        if (firstName.Trim().Length is < 1 or > 80 ||
            lastName.Trim().Length is < 1 or > 80 ||
            !LooksLikeEmail(normalizedEmail) ||
            address.Trim().Length is < 1 or > 160 ||
            phone.Trim().Length is < 3 or > 40)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "Podaci korisnika nisu valjani.");
        }

        if (!string.IsNullOrWhiteSpace(oib) && oib.Trim().Length != 11)
        {
            return CommandResult.Fail(CommandErrorKind.Validation, "OIB mora imati 11 znakova.");
        }

        if (await dbContext.Korisnici.AnyAsync(
                item => item.Email == normalizedEmail &&
                        (!userId.HasValue || item.Id != userId.Value),
                cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.Conflict, "Email već postoji.");
        }

        if (vehicleId.HasValue &&
            !await dbContext.Vozila.AnyAsync(item => item.Id == vehicleId.Value, cancellationToken))
        {
            return CommandResult.Fail(CommandErrorKind.NotFound, "Vozilo ne postoji.");
        }

        return null;
    }

    private static bool UsesRideCapacity(StatusRezervacije status) =>
        status is StatusRezervacije.Potvrdena or StatusRezervacije.Zavrsena;

    private static bool LooksLikeEmail(string value) =>
        value.Length is >= 3 and <= 254 &&
        value.Contains('@', StringComparison.Ordinal) &&
        value.Contains('.', StringComparison.Ordinal);
}
