using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class Placanje
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Rezervacija))]
    public int RezervacijaId { get; set; }
    public virtual Rezervacija Rezervacija { get; set; } = null!;
    public decimal Iznos { get; set; }
    public DateTime VrijemePlacanja { get; set; }
    public NacinPlacanja NacinPlacanja { get; set; }
    public bool Uspjesno { get; set; }
}