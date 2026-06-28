using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SideSeat.Models;

public class Vozilo
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Marka { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [StringLength(24)]
    public string Registracija { get; set; } = string.Empty;

    [Range(1980, 2035)]
    public int GodinaProizvodnje { get; set; }

    [Range(1, 8)]
    public int BrojSjedala { get; set; }

    [Required]
    [StringLength(40)]
    public string Boja { get; set; } = string.Empty;

    [Range(0.1, 30)]
    public decimal ProsjecnaPotrosnja { get; set; }

    [Display(Name = "Vlasnik")]
    public int? VlasnikId { get; set; }
    public virtual Korisnik? Vlasnik { get; set; }
}
