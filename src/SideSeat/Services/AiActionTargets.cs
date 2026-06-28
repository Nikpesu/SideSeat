using System.Globalization;
using System.Text;
using SideSeat.Models.Commands;

namespace SideSeat.Services;

/// <summary>
/// Mapira pripremljenu AI akciju na stvarnu stranicu aplikacije s predpopunjenim poljima
/// (query string). AI tako ne izvršava akciju, nego korisnika odvede na formu i popuni ono
/// što je rečeno, a ostalo korisnik dovršava sam.
/// </summary>
public static class AiActionTargets
{
    public static string? Resolve(string actionType, object? payload) => payload switch
    {
        CreateCityCommand c => "/Grad/Create" + Query(
            ("Naziv", c.Naziv), ("Drzava", c.Drzava), ("PostanskiBroj", c.PostanskiBroj),
            ("Latitude", c.Latitude), ("Longitude", c.Longitude)),

        CreateVehicleCommand v => "/Vozilo/Create" + Query(
            ("Marka", v.Marka), ("Model", v.Model), ("Registracija", v.Registracija),
            ("GodinaProizvodnje", v.GodinaProizvodnje), ("BrojSjedala", v.BrojSjedala),
            ("Boja", v.Boja), ("ProsjecnaPotrosnja", v.ProsjecnaPotrosnja), ("VlasnikId", v.VlasnikId)),

        CreateRideCommand r => "/Voznja/Create" + Query(
            ("PolazniGradId", r.PolazniGradId), ("OdredisniGradId", r.OdredisniGradId),
            ("Polazak", r.Polazak), ("OcekivaniDolazak", r.OcekivaniDolazak),
            ("CijenaPoMjestu", r.CijenaPoMjestu), ("UkupnoMjesta", r.UkupnoMjesta), ("Opis", r.Opis)),

        CreateReservationCommand r => "/Rezervacija/Create" + Query(
            ("voznjaId", r.VoznjaId), ("BrojMjesta", r.BrojMjesta), ("Napomena", r.Napomena),
            ("NacinPlacanja", r.NacinPlacanja), ("Napojnica", r.Napojnica)),

        CreateReviewCommand r => "/Ocjena/Create" + Query(
            ("rezervacijaId", r.RezervacijaId), ("BrojZvjezdica", r.BrojZvjezdica), ("Komentar", r.Komentar)),

        // "Uplati 20 €" → stranica za nadoplatu salda (već predpopuni iznos).
        CreatePaymentCommand p => "/Korisnik/Uplata" + Query(("amount", p.Iznos)),
        CreateBalanceTransactionCommand b => "/Korisnik/Uplata" + Query(("amount", b.Iznos)),

        CreateUserCommand u => "/Korisnik/Create" + Query(
            ("Ime", u.Ime), ("Prezime", u.Prezime), ("Email", u.Email),
            ("Adresa", u.Adresa), ("BrojMobitela", u.BrojMobitela), ("Tip", u.Tip)),

        _ => null
    };

    private static string Query(params (string Key, object? Value)[] pairs)
    {
        var builder = new StringBuilder();
        foreach (var (key, value) in pairs)
        {
            var formatted = Format(value);
            if (string.IsNullOrEmpty(formatted))
            {
                continue;
            }

            builder.Append(builder.Length == 0 ? '?' : '&');
            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(formatted));
        }

        return builder.ToString();
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTime date => date.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
        decimal number => number.ToString("0.##", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        Enum enumValue => enumValue.ToString(),
        _ => value.ToString() ?? string.Empty
    };
}
