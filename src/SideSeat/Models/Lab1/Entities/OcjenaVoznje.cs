namespace SideSeat.Models;

public class OcjenaVoznje
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public Rezervacija Rezervacija { get; set; } = null!;
    public int AutorId { get; set; }
    public Korisnik Autor { get; set; } = null!;
    public int BrojZvjezdica { get; set; }
    public string Komentar { get; set; } = string.Empty;
    public DateTime Kreirano { get; set; }
}