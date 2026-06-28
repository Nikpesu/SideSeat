namespace SideSeat.Models;

public static class StatusRezervacijeExtensions
{
    public static string ToDisplayName(this StatusRezervacije status) =>
        status switch
        {
            StatusRezervacije.UProcesuPotvrde => "U procesu potvrde",
            StatusRezervacije.Potvrdena => "Potvrđena",
            StatusRezervacije.Odbijena => "Odbijena",
            StatusRezervacije.Zavrsena => "Završena",
            _ => status.ToString()
        };
}
