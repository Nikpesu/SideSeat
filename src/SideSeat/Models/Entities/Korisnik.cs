using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class Korisnik
{
    [Key]
    public int Id { get; set; }
    public string Ime { get; set; } = string.Empty;
    public string Prezime { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LozinkaHash { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
    public string Adresa { get; set; } = string.Empty;
    public string BrojMobitela { get; set; } = string.Empty;
    public DateTime DatumRegistracije { get; set; }
    public TipKorisnika Tip { get; set; }
    public bool JeAktivan { get; set; }
    public bool KycPodnesen { get; set; }
    public string? KycOib { get; set; }
    public string? KycBrojOsobne { get; set; }
    public string? KycBrojVozacke { get; set; }
    public DateTime? KycDatumRodenja { get; set; }
    public string? ProfilnaSlikaPath { get; set; }
    public string? SpremljenaKarticaIme { get; set; }
    public string? SpremljenaKarticaZadnjeCetiri { get; set; }
    public string? SpremljenaKarticaVrijediDo { get; set; }
    public string? SpremljenaAdresaPlacanja { get; set; }

    public int? VoziloId { get; set; }
    public virtual Vozilo? Vozilo { get; set; }
    public virtual ICollection<Voznja> KreiraneVoznje { get; set; } = new List<Voznja>();
    public virtual ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
    public virtual ICollection<SaldoTransakcija> SaldoTransakcije { get; set; } = new List<SaldoTransakcija>();
    public virtual ICollection<Obavijest> Obavijesti { get; set; } = new List<Obavijest>();
}
