using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using ModelContextProtocol.Server;
using SideSeat.Services;

namespace SideSeat.Mcp;

[McpServerToolType]
public sealed class SideSeatMcpTools(
    IAiToolService aiTools,
    IPendingActionService pendingActions,
    IHttpContextAccessor httpContextAccessor)
{
    [McpServerTool(Name = "get_current_user", ReadOnly = true)]
    [Description("Returns safe data for the configured SideSeat MCP service user.")]
    public Task<string> GetCurrentUser(CancellationToken cancellationToken) =>
        ExecuteReadAsync("get_current_user", "{}", cancellationToken);

    [McpServerTool(Name = "get_rides", ReadOnly = true)]
    [Description("Returns role-filtered SideSeat rides.")]
    public Task<string> GetRides(
        string scope = "available",
        string status = "all",
        int? id = null,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "get_rides",
            JsonSerializer.Serialize(new { scope, status, id, search }),
            cancellationToken);

    [McpServerTool(Name = "get_reservations", ReadOnly = true)]
    [Description("Returns role-filtered SideSeat reservations.")]
    public Task<string> GetReservations(
        string scope = "mine",
        string status = "all",
        int? id = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "get_reservations",
            JsonSerializer.Serialize(new { scope, status, id }),
            cancellationToken);

    [McpServerTool(Name = "get_balance", ReadOnly = true)]
    [Description("Returns balance and recent transactions for the configured service user.")]
    public Task<string> GetBalance(
        int transactionLimit = 20,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "get_balance",
            JsonSerializer.Serialize(new { transactionLimit }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_city", Destructive = false)]
    [Description("Prepares an admin city creation and returns a confirmation token.")]
    public Task<string> PrepareCreateCity(
        string naziv,
        string drzava,
        string postanskiBroj,
        decimal? latitude = null,
        decimal? longitude = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_city",
            JsonSerializer.Serialize(new { naziv, drzava, postanskiBroj, latitude, longitude }),
            cancellationToken);

    [McpServerTool(Name = "prepare_update_city", Destructive = false)]
    [Description("Prepares an admin city update and returns a confirmation token.")]
    public Task<string> PrepareUpdateCity(
        int id,
        string naziv,
        string drzava,
        string postanskiBroj,
        decimal? latitude = null,
        decimal? longitude = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_update_city",
            JsonSerializer.Serialize(new { id, naziv, drzava, postanskiBroj, latitude, longitude }),
            cancellationToken);

    [McpServerTool(Name = "prepare_delete_city", Destructive = false)]
    [Description("Prepares deleting a city and returns a confirmation token.")]
    public Task<string> PrepareDeleteCity(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_delete_city",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_vehicle", Destructive = false)]
    [Description("Prepares an admin vehicle creation and returns a confirmation token.")]
    public Task<string> PrepareCreateVehicle(
        string marka,
        string model,
        string registracija,
        int godinaProizvodnje,
        int brojSjedala,
        string boja,
        decimal prosjecnaPotrosnja,
        int? vlasnikId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_vehicle",
            JsonSerializer.Serialize(new
            {
                marka,
                model,
                registracija,
                godinaProizvodnje,
                brojSjedala,
                boja,
                prosjecnaPotrosnja,
                vlasnikId
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_update_vehicle", Destructive = false)]
    [Description("Prepares an admin vehicle update and returns a confirmation token.")]
    public Task<string> PrepareUpdateVehicle(
        int id,
        string marka,
        string model,
        string registracija,
        int godinaProizvodnje,
        int brojSjedala,
        string boja,
        decimal prosjecnaPotrosnja,
        int? vlasnikId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_update_vehicle",
            JsonSerializer.Serialize(new
            {
                id,
                marka,
                model,
                registracija,
                godinaProizvodnje,
                brojSjedala,
                boja,
                prosjecnaPotrosnja,
                vlasnikId
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_delete_vehicle", Destructive = false)]
    [Description("Prepares deleting a vehicle and returns a confirmation token.")]
    public Task<string> PrepareDeleteVehicle(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_delete_vehicle",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_user", Destructive = false)]
    [Description("Prepares an admin user creation and returns a confirmation token.")]
    public Task<string> PrepareCreateUser(
        string ime,
        string prezime,
        string email,
        string adresa,
        string brojMobitela,
        string password,
        string tip = "Putnik",
        bool jeAktivan = true,
        bool kycPodnesen = false,
        string? kycOib = null,
        string? kycBrojOsobne = null,
        string? kycBrojVozacke = null,
        DateTime? kycDatumRodenja = null,
        int? voziloId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_user",
            JsonSerializer.Serialize(new
            {
                ime,
                prezime,
                email,
                adresa,
                brojMobitela,
                tip,
                jeAktivan,
                kycPodnesen,
                kycOib,
                kycBrojOsobne,
                kycBrojVozacke,
                kycDatumRodenja,
                voziloId,
                password
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_update_user", Destructive = false)]
    [Description("Prepares an admin user update and returns a confirmation token.")]
    public Task<string> PrepareUpdateUser(
        int id,
        string ime,
        string prezime,
        string email,
        string adresa,
        string brojMobitela,
        string tip = "Putnik",
        bool jeAktivan = true,
        bool kycPodnesen = false,
        string? kycOib = null,
        string? kycBrojOsobne = null,
        string? kycBrojVozacke = null,
        DateTime? kycDatumRodenja = null,
        int? voziloId = null,
        string? password = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_update_user",
            JsonSerializer.Serialize(new
            {
                id,
                ime,
                prezime,
                email,
                adresa,
                brojMobitela,
                tip,
                jeAktivan,
                kycPodnesen,
                kycOib,
                kycBrojOsobne,
                kycBrojVozacke,
                kycDatumRodenja,
                voziloId,
                password
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_delete_user", Destructive = false)]
    [Description("Prepares deleting a user and returns a confirmation token.")]
    public Task<string> PrepareDeleteUser(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_delete_user",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_payment", Destructive = false)]
    [Description("Prepares an admin payment record creation and returns a confirmation token.")]
    public Task<string> PrepareCreatePayment(
        int rezervacijaId,
        decimal iznos,
        string nacinPlacanja,
        DateTime? vrijemePlacanja = null,
        bool uspjesno = true,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_payment",
            JsonSerializer.Serialize(new
            {
                rezervacijaId,
                iznos,
                vrijemePlacanja = vrijemePlacanja ?? DateTime.UtcNow,
                nacinPlacanja,
                uspjesno
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_update_payment", Destructive = false)]
    [Description("Prepares an admin payment record update and returns a confirmation token.")]
    public Task<string> PrepareUpdatePayment(
        int id,
        int rezervacijaId,
        decimal iznos,
        string nacinPlacanja,
        DateTime? vrijemePlacanja = null,
        bool uspjesno = true,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_update_payment",
            JsonSerializer.Serialize(new
            {
                id,
                rezervacijaId,
                iznos,
                vrijemePlacanja = vrijemePlacanja ?? DateTime.UtcNow,
                nacinPlacanja,
                uspjesno
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_delete_payment", Destructive = false)]
    [Description("Prepares deleting a payment record and returns a confirmation token.")]
    public Task<string> PrepareDeletePayment(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_delete_payment",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_balance_transaction", Destructive = false)]
    [Description("Prepares a balance transaction and returns a confirmation token.")]
    public Task<string> PrepareCreateBalanceTransaction(
        decimal iznos,
        string tip,
        int? korisnikId = null,
        string komentar = "",
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_balance_transaction",
            JsonSerializer.Serialize(new { korisnikId, iznos, tip, komentar }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_ride", Destructive = false)]
    [Description("Prepares a ride creation and returns a confirmation token.")]
    public Task<string> PrepareCreateRide(
        int polazniGradId,
        int odredisniGradId,
        DateTime polazak,
        DateTime ocekivaniDolazak,
        decimal cijenaPoMjestu,
        int ukupnoMjesta,
        int slobodnaMjesta,
        string opis = "",
        int? vozacId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_ride",
            JsonSerializer.Serialize(new
            {
                vozacId,
                polazniGradId,
                odredisniGradId,
                polazak,
                ocekivaniDolazak,
                cijenaPoMjestu,
                ukupnoMjesta,
                slobodnaMjesta,
                opis
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_reservation", Destructive = false)]
    [Description("Prepares a reservation and returns a confirmation token.")]
    public Task<string> PrepareCreateReservation(
        int voznjaId,
        int brojMjesta,
        string nacinPlacanja = "SideSeatSaldo",
        decimal napojnica = 0,
        string napomena = "",
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_reservation",
            JsonSerializer.Serialize(new { voznjaId, brojMjesta, nacinPlacanja, napojnica, napomena }),
            cancellationToken);

    [McpServerTool(Name = "prepare_update_reservation", Destructive = false)]
    [Description("Prepares an admin reservation update and returns a confirmation token.")]
    public Task<string> PrepareUpdateReservation(
        int id,
        int voznjaId,
        int putnikId,
        int brojMjesta,
        string status,
        string nacinPlacanja = "SideSeatSaldo",
        decimal napojnica = 0,
        string napomena = "",
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_update_reservation",
            JsonSerializer.Serialize(new
            {
                id,
                voznjaId,
                putnikId,
                brojMjesta,
                status,
                nacinPlacanja,
                napojnica,
                napomena
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_delete_reservation", Destructive = false)]
    [Description("Prepares deleting a reservation and returns a confirmation token.")]
    public Task<string> PrepareDeleteReservation(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_delete_reservation",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_create_review", Destructive = false)]
    [Description("Prepares a review and returns a confirmation token.")]
    public Task<string> PrepareCreateReview(
        int rezervacijaId,
        int brojZvjezdica,
        string komentar,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_create_review",
            JsonSerializer.Serialize(new { rezervacijaId, brojZvjezdica, komentar }),
            cancellationToken);

    [McpServerTool(Name = "prepare_update_review", Destructive = false)]
    [Description("Prepares a review update and returns a confirmation token.")]
    public Task<string> PrepareUpdateReview(
        int id,
        int rezervacijaId,
        int autorId,
        int brojZvjezdica,
        string komentar,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_update_review",
            JsonSerializer.Serialize(new { id, rezervacijaId, autorId, brojZvjezdica, komentar }),
            cancellationToken);

    [McpServerTool(Name = "prepare_delete_review", Destructive = false)]
    [Description("Prepares deleting a review and returns a confirmation token.")]
    public Task<string> PrepareDeleteReview(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_delete_review",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_update_ride", Destructive = false)]
    [Description("Prepares a ride update and returns a confirmation token.")]
    public Task<string> PrepareUpdateRide(
        int id,
        int polazniGradId,
        int odredisniGradId,
        DateTime polazak,
        DateTime ocekivaniDolazak,
        decimal cijenaPoMjestu,
        int ukupnoMjesta,
        int slobodnaMjesta,
        string opis = "",
        string status = "Planirana",
        int? vozacId = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_update_ride",
            JsonSerializer.Serialize(new
            {
                id,
                vozacId,
                polazniGradId,
                odredisniGradId,
                polazak,
                ocekivaniDolazak,
                cijenaPoMjestu,
                ukupnoMjesta,
                slobodnaMjesta,
                opis,
                status
            }),
            cancellationToken);

    [McpServerTool(Name = "prepare_delete_ride", Destructive = false)]
    [Description("Prepares deleting a ride and returns a confirmation token.")]
    public Task<string> PrepareDeleteRide(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_delete_ride",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_start_ride", Destructive = false)]
    [Description("Prepares starting a ride when confirmed passengers are checked in.")]
    public Task<string> PrepareStartRide(int id, CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_start_ride",
            JsonSerializer.Serialize(new { id }),
            cancellationToken);

    [McpServerTool(Name = "prepare_finish_ride", Destructive = false)]
    [Description("Prepares finishing a ride and settling balance/cash payments.")]
    public Task<string> PrepareFinishRide(
        int id,
        bool cashCollected = true,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_finish_ride",
            JsonSerializer.Serialize(new { id, cashCollected }),
            cancellationToken);

    [McpServerTool(Name = "prepare_check_in_reservation", Destructive = false)]
    [Description("Prepares reservation check-in with optional location.")]
    public Task<string> PrepareCheckInReservation(
        int rezervacijaId,
        decimal? latitude = null,
        decimal? longitude = null,
        CancellationToken cancellationToken = default) =>
        ExecuteReadAsync(
            "prepare_check_in_reservation",
            JsonSerializer.Serialize(new { rezervacijaId, latitude, longitude }),
            cancellationToken);

    [McpServerTool(Name = "confirm_action", Destructive = true)]
    [Description("Confirms and executes a previously prepared SideSeat write action.")]
    public async Task<string> ConfirmAction(
        string token,
        CancellationToken cancellationToken = default)
    {
        var result = await pendingActions.ConfirmAsync(
            token,
            Principal,
            "MCP",
            cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    [McpServerTool(Name = "cancel_action", Destructive = false, Idempotent = true)]
    [Description("Cancels a previously prepared SideSeat write action.")]
    public string CancelAction(string token) =>
        JsonSerializer.Serialize(new
        {
            cancelled = pendingActions.Cancel(token, Principal)
        });

    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? new ClaimsPrincipal(new ClaimsIdentity());

    private Task<string> ExecuteReadAsync(
        string name,
        string arguments,
        CancellationToken cancellationToken) =>
        aiTools.ExecuteAsync(name, arguments, Principal, cancellationToken);
}
