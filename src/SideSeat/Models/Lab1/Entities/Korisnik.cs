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
    public string BrojMobitela { get; set; } = string.Empty;
    public DateTime DatumRegistracije { get; set; }
    public TipKorisnika Tip { get; set; }
    public bool JeAktivan { get; set; }

    [ForeignKey(nameof(Vozilo))]
    public int? VoziloId { get; set; }
    public virtual Vozilo? Vozilo { get; set; }
    public virtual ICollection<Voznja> KreiraneVoznje { get; set; } = new List<Voznja>();
    public virtual ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
}