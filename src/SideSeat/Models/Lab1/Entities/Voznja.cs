using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class Voznja
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Vozac))]
    public int VozacId { get; set; }
    public virtual Korisnik Vozac { get; set; } = null!;

    [ForeignKey(nameof(PolazniGrad))]
    public int PolazniGradId { get; set; }
    public virtual Grad PolazniGrad { get; set; } = null!;

    [ForeignKey(nameof(OdredisniGrad))]
    public int OdredisniGradId { get; set; }
    public virtual Grad OdredisniGrad { get; set; } = null!;
    public DateTime Polazak { get; set; }
    public DateTime OcekivaniDolazak { get; set; }
    public decimal CijenaPoMjestu { get; set; }
    public int UkupnoMjesta { get; set; }
    public int SlobodnaMjesta { get; set; }
    public string Opis { get; set; } = string.Empty;
    public StatusVoznje Status { get; set; }
    public virtual ICollection<Rezervacija> Rezervacije { get; set; } = new List<Rezervacija>();
    public virtual ICollection<VoznjaAttachment> Attachments { get; set; } = new List<VoznjaAttachment>();
}
