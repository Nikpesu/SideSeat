using SideSeat.Models.Api;
using SideSeat.Models;

namespace SideSeat.Models.Commands;

public enum CommandErrorKind
{
    None,
    Validation,
    Forbidden,
    NotFound,
    Conflict,
    BusinessRule
}

public sealed record CommandResult(
    bool Succeeded,
    CommandErrorKind ErrorKind,
    string Message,
    string? EntityType = null,
    int? EntityId = null,
    string? Link = null)
{
    public static CommandResult Success(
        string message,
        string entityType,
        int entityId,
        string link) =>
        new(true, CommandErrorKind.None, message, entityType, entityId, link);

    public static CommandResult Fail(CommandErrorKind kind, string message) =>
        new(false, kind, message);
}

public sealed record PendingActionDescriptor(
    string Token,
    string ActionType,
    string Title,
    string Summary,
    DateTime ExpiresAtUtc,
    PendingFormDescriptor? Form = null,
    string? TargetUrl = null);

public sealed record PendingFormDescriptor(
    string FormType,
    string Title,
    string SubmitLabel,
    string ReviewUrl,
    IReadOnlyList<PendingFormSection> Sections,
    IReadOnlyList<string> Warnings);

public sealed record PendingFormSection(
    string Title,
    IReadOnlyList<PendingFormField> Fields);

public sealed record PendingFormField(
    string Name,
    string Label,
    string Value,
    string InputType,
    bool IsSensitive = false);

public sealed record PendingActionEnvelope(
    string ActionType,
    int KorisnikId,
    string Title,
    string Summary,
    string PayloadJson,
    DateTime ExpiresAtUtc,
    PendingFormDescriptor? Form = null);

public static class SideSeatActionTypes
{
    public const string CreateCity = "create_city";
    public const string UpdateCity = "update_city";
    public const string DeleteCity = "delete_city";
    public const string CreateVehicle = "create_vehicle";
    public const string UpdateVehicle = "update_vehicle";
    public const string DeleteVehicle = "delete_vehicle";
    public const string CreateRide = "create_ride";
    public const string CreateReservation = "create_reservation";
    public const string UpdateReservation = "update_reservation";
    public const string DeleteReservation = "delete_reservation";
    public const string CreateReview = "create_review";
    public const string UpdateReview = "update_review";
    public const string DeleteReview = "delete_review";
    public const string CreatePayment = "create_payment";
    public const string UpdatePayment = "update_payment";
    public const string DeletePayment = "delete_payment";
    public const string CreateUser = "create_user";
    public const string UpdateUser = "update_user";
    public const string DeleteUser = "delete_user";
    public const string CreateBalanceTransaction = "create_balance_transaction";
    public const string UpdateRide = "update_ride";
    public const string DeleteRide = "delete_ride";
    public const string StartRide = "start_ride";
    public const string FinishRide = "finish_ride";
    public const string CheckInReservation = "check_in_reservation";
}

public sealed record CreateCityCommand(
    string Naziv,
    string Drzava,
    string PostanskiBroj,
    decimal? Latitude = null,
    decimal? Longitude = null);

public sealed record UpdateCityCommand(
    int Id,
    string Naziv,
    string Drzava,
    string PostanskiBroj,
    decimal? Latitude = null,
    decimal? Longitude = null);

public sealed record DeleteCityCommand(int Id);

public sealed record CreateVehicleCommand(
    string Marka,
    string Model,
    string Registracija,
    int GodinaProizvodnje,
    int BrojSjedala,
    string Boja,
    decimal ProsjecnaPotrosnja,
    int? VlasnikId);

public sealed record UpdateVehicleCommand(
    int Id,
    string Marka,
    string Model,
    string Registracija,
    int GodinaProizvodnje,
    int BrojSjedala,
    string Boja,
    decimal ProsjecnaPotrosnja,
    int? VlasnikId);

public sealed record DeleteVehicleCommand(int Id);

public sealed record CreateRideCommand(
    int VozacId,
    int PolazniGradId,
    int OdredisniGradId,
    DateTime Polazak,
    DateTime OcekivaniDolazak,
    decimal CijenaPoMjestu,
    int UkupnoMjesta,
    int SlobodnaMjesta,
    string Opis);

public sealed record CreateReservationCommand(
    int VoznjaId,
    int BrojMjesta,
    string Napomena,
    NacinPlacanja NacinPlacanja = NacinPlacanja.SideSeatSaldo,
    decimal Napojnica = 0);

public sealed record UpdateReservationCommand(
    int Id,
    int VoznjaId,
    int PutnikId,
    int BrojMjesta,
    StatusRezervacije Status,
    NacinPlacanja NacinPlacanja,
    decimal Napojnica,
    string Napomena);

public sealed record DeleteReservationCommand(int Id);

public sealed record CreateReviewCommand(
    int RezervacijaId,
    int BrojZvjezdica,
    string Komentar);

public sealed record UpdateReviewCommand(
    int Id,
    int RezervacijaId,
    int AutorId,
    int BrojZvjezdica,
    string Komentar);

public sealed record DeleteReviewCommand(int Id);

public sealed record CreatePaymentCommand(
    int RezervacijaId,
    decimal Iznos,
    DateTime VrijemePlacanja,
    NacinPlacanja NacinPlacanja,
    bool Uspjesno);

public sealed record UpdatePaymentCommand(
    int Id,
    int RezervacijaId,
    decimal Iznos,
    DateTime VrijemePlacanja,
    NacinPlacanja NacinPlacanja,
    bool Uspjesno);

public sealed record DeletePaymentCommand(int Id);

public sealed record CreateUserCommand(
    string Ime,
    string Prezime,
    string Email,
    string Adresa,
    string BrojMobitela,
    TipKorisnika Tip,
    bool JeAktivan,
    bool KycPodnesen,
    string? KycOib,
    string? KycBrojOsobne,
    string? KycBrojVozacke,
    DateTime? KycDatumRodenja,
    int? VoziloId,
    string Password);

public sealed record UpdateUserCommand(
    int Id,
    string Ime,
    string Prezime,
    string Email,
    string Adresa,
    string BrojMobitela,
    TipKorisnika Tip,
    bool JeAktivan,
    bool KycPodnesen,
    string? KycOib,
    string? KycBrojOsobne,
    string? KycBrojVozacke,
    DateTime? KycDatumRodenja,
    int? VoziloId,
    string? Password);

public sealed record DeleteUserCommand(int Id);

public sealed record CreateBalanceTransactionCommand(
    int? KorisnikId,
    decimal Iznos,
    string Tip,
    string Komentar = "");

public sealed record UpdateRideCommand(
    int Id,
    int VozacId,
    int PolazniGradId,
    int OdredisniGradId,
    DateTime Polazak,
    DateTime OcekivaniDolazak,
    decimal CijenaPoMjestu,
    int UkupnoMjesta,
    int SlobodnaMjesta,
    string Opis,
    StatusVoznje Status);

public sealed record DeleteRideCommand(int Id);

public sealed record StartRideCommand(int Id);

public sealed record FinishRideCommand(int Id, bool CashCollected = true);

public sealed record CheckInReservationCommand(
    int RezervacijaId,
    decimal? Latitude = null,
    decimal? Longitude = null);
