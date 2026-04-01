namespace SideSeat.Models;

public class Lab1Podaci
{
    public List<Korisnik> Korisnici { get; } = new();
    public List<Grad> Gradovi { get; } = new();
    public List<Vozilo> Vozila { get; } = new();
    public List<Voznja> Voznje { get; } = new();
    public List<Rezervacija> Rezervacije { get; } = new();
    public List<Placanje> Placanja { get; } = new();
    public List<OcjenaVoznje> Ocjene { get; } = new();
}