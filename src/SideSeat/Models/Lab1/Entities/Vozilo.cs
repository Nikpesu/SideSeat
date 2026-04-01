namespace SideSeat.Models;

public class Vozilo
{
    public int Id { get; set; }
    public string Marka { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Registracija { get; set; } = string.Empty;
    public int GodinaProizvodnje { get; set; }
    public int BrojSjedala { get; set; }
    public string Boja { get; set; } = string.Empty;
    public decimal ProsjecnaPotrosnja { get; set; }
    public int? VlasnikId { get; set; }
    public Korisnik? Vlasnik { get; set; }
}