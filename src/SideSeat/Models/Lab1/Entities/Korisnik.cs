namespace SideSeat.Models;

public class Korisnik
{
    public int Id { get; set; }
    public string Ime { get; set; } = string.Empty;
    public string Prezime { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BrojMobitela { get; set; } = string.Empty;
    public DateTime DatumRegistracije { get; set; }
    public TipKorisnika Tip { get; set; }
    public bool JeAktivan { get; set; }
    public int? VoziloId { get; set; }
    public Vozilo? Vozilo { get; set; }
    public List<Voznja> KreiraneVoznje { get; set; } = new();
    public List<Rezervacija> Rezervacije { get; set; } = new();
}