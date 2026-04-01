namespace SideSeat.Models;

public class Placanje
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public Rezervacija Rezervacija { get; set; } = null!;
    public decimal Iznos { get; set; }
    public DateTime VrijemePlacanja { get; set; }
    public NacinPlacanja NacinPlacanja { get; set; }
    public bool Uspjesno { get; set; }
}