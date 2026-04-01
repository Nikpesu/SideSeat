namespace SideSeat.Models;

public class Voznja
{
    public int Id { get; set; }
    public int VozacId { get; set; }
    public Korisnik Vozac { get; set; } = null!;
    public int PolazniGradId { get; set; }
    public Grad PolazniGrad { get; set; } = null!;
    public int OdredisniGradId { get; set; }
    public Grad OdredisniGrad { get; set; } = null!;
    public DateTime Polazak { get; set; }
    public DateTime OcekivaniDolazak { get; set; }
    public decimal CijenaPoMjestu { get; set; }
    public int UkupnoMjesta { get; set; }
    public int SlobodnaMjesta { get; set; }
    public string Opis { get; set; } = string.Empty;
    public StatusVoznje Status { get; set; }
    public List<Rezervacija> Rezervacije { get; set; } = new();
}