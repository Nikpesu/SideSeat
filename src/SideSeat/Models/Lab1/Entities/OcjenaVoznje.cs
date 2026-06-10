using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class OcjenaVoznje
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Rezervacija))]
    public int RezervacijaId { get; set; }
    public virtual Rezervacija Rezervacija { get; set; } = null!;

    [ForeignKey(nameof(Autor))]
    public int AutorId { get; set; }
    public virtual Korisnik Autor { get; set; } = null!;
    public int BrojZvjezdica { get; set; }
    public string Komentar { get; set; } = string.Empty;
    public DateTime Kreirano { get; set; }
    public DateTime? Uredeno { get; set; }
    public bool Administratorska { get; set; }
    public virtual ICollection<OcjenaSlika> Slike { get; set; } = new List<OcjenaSlika>();
}
