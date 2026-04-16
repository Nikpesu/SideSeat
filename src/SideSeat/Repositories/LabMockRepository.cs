using SideSeat.Models;

namespace SideSeat.Repositories;

/// <summary>
/// Centralni mock repository za Lab 2 koji vraca staticke podatke iz Lab1 seed metode.
/// </summary>
public class LabMockRepository
{
    private readonly Lab1Podaci _data;

    public LabMockRepository()
    {
        _data = Lab1Demo.KreirajPodatke();
    }

    public List<Grad> GetGradovi() => _data.Gradovi;
    public Grad? GetGradById(int id) => _data.Gradovi.FirstOrDefault(x => x.Id == id);

    public List<Korisnik> GetKorisnici() => _data.Korisnici;
    public Korisnik? GetKorisnikById(int id) => _data.Korisnici.FirstOrDefault(x => x.Id == id);

    public List<Vozilo> GetVozila() => _data.Vozila;
    public Vozilo? GetVoziloById(int id) => _data.Vozila.FirstOrDefault(x => x.Id == id);

    public List<Voznja> GetVoznje() => _data.Voznje;
    public Voznja? GetVoznjaById(int id) => _data.Voznje.FirstOrDefault(x => x.Id == id);

    public List<Rezervacija> GetRezervacije() => _data.Rezervacije;
    public Rezervacija? GetRezervacijaById(int id) => _data.Rezervacije.FirstOrDefault(x => x.Id == id);

    public List<Placanje> GetPlacanja() => _data.Placanja;
    public Placanje? GetPlacanjeById(int id) => _data.Placanja.FirstOrDefault(x => x.Id == id);

    public List<OcjenaVoznje> GetOcjene() => _data.Ocjene;
    public OcjenaVoznje? GetOcjenaById(int id) => _data.Ocjene.FirstOrDefault(x => x.Id == id);
}
