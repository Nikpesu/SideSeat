using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;

namespace SideSeat.Repositories;

/// <summary>
/// EF repository za citanje podataka u Lab 3 zadacima.
/// </summary>
public class SideSeatEfRepository
{
    private readonly SideSeatDbContext _db;

    public SideSeatEfRepository(SideSeatDbContext db)
    {
        _db = db;
    }

    public List<Grad> GetGradovi() => _db.Gradovi
        .AsNoTracking()
        .OrderBy(g => g.Naziv)
        .ToList();

    public Grad? GetGradById(int id) => _db.Gradovi
        .AsNoTracking()
        .FirstOrDefault(g => g.Id == id);

    public List<Korisnik> GetKorisnici() => _db.Korisnici
        .AsNoTracking()
        .Include(k => k.Vozilo)
        .OrderBy(k => k.Prezime)
        .ThenBy(k => k.Ime)
        .ToList();

    public Korisnik? GetKorisnikById(int id) => _db.Korisnici
        .AsNoTracking()
        .Include(k => k.Vozilo)
        .FirstOrDefault(k => k.Id == id);

    public List<Vozilo> GetVozila() => _db.Vozila
        .AsNoTracking()
        .Include(v => v.Vlasnik)
        .OrderBy(v => v.Marka)
        .ThenBy(v => v.Model)
        .ToList();

    public Vozilo? GetVoziloById(int id) => _db.Vozila
        .AsNoTracking()
        .Include(v => v.Vlasnik)
        .FirstOrDefault(v => v.Id == id);

    public List<Voznja> GetVoznje() => _db.Voznje
        .AsNoTracking()
        .Include(v => v.Vozac)
        .Include(v => v.PolazniGrad)
        .Include(v => v.OdredisniGrad)
        .OrderBy(v => v.Polazak)
        .ToList();

    public Voznja? GetVoznjaById(int id) => _db.Voznje
        .AsNoTracking()
        .Include(v => v.Vozac)
        .Include(v => v.PolazniGrad)
        .Include(v => v.OdredisniGrad)
        .Include(v => v.Rezervacije)
        .FirstOrDefault(v => v.Id == id);

    public List<Rezervacija> GetRezervacije() => _db.Rezervacije
        .AsNoTracking()
        .Include(r => r.Putnik)
        .Include(r => r.Voznja)
        .ThenInclude(v => v.PolazniGrad)
        .Include(r => r.Voznja)
        .ThenInclude(v => v.OdredisniGrad)
        .OrderByDescending(r => r.VrijemeRezervacije)
        .ToList();

    public Rezervacija? GetRezervacijaById(int id) => _db.Rezervacije
        .AsNoTracking()
        .Include(r => r.Putnik)
        .Include(r => r.Voznja)
        .ThenInclude(v => v.PolazniGrad)
        .Include(r => r.Voznja)
        .ThenInclude(v => v.OdredisniGrad)
        .FirstOrDefault(r => r.Id == id);

    public List<Placanje> GetPlacanja() => _db.Placanja
        .AsNoTracking()
        .Include(p => p.Rezervacija)
        .ThenInclude(r => r.Putnik)
        .Include(p => p.Rezervacija)
        .ThenInclude(r => r.Voznja)
        .ThenInclude(v => v.PolazniGrad)
        .Include(p => p.Rezervacija)
        .ThenInclude(r => r.Voznja)
        .ThenInclude(v => v.OdredisniGrad)
        .OrderByDescending(p => p.VrijemePlacanja)
        .ToList();

    public Placanje? GetPlacanjeById(int id) => _db.Placanja
        .AsNoTracking()
        .Include(p => p.Rezervacija)
        .ThenInclude(r => r.Putnik)
        .Include(p => p.Rezervacija)
        .ThenInclude(r => r.Voznja)
        .ThenInclude(v => v.PolazniGrad)
        .Include(p => p.Rezervacija)
        .ThenInclude(r => r.Voznja)
        .ThenInclude(v => v.OdredisniGrad)
        .FirstOrDefault(p => p.Id == id);

    public List<OcjenaVoznje> GetOcjene() => _db.Ocjene
        .AsNoTracking()
        .Include(o => o.Autor)
        .OrderByDescending(o => o.Kreirano)
        .ToList();

    public OcjenaVoznje? GetOcjenaById(int id) => _db.Ocjene
        .AsNoTracking()
        .Include(o => o.Autor)
        .Include(o => o.Rezervacija)
        .FirstOrDefault(o => o.Id == id);
}
