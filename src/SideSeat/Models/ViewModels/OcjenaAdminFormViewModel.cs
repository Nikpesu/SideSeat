using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.ViewModels;

public class OcjenaAdminFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "Rezervacija")]
    public int RezervacijaId { get; set; }

    [Required]
    [Display(Name = "Autor")]
    public int AutorId { get; set; }

    [Range(1, 5)]
    [Display(Name = "Broj zvjezdica")]
    public int BrojZvjezdica { get; set; } = 5;

    [Required]
    [StringLength(500)]
    [Display(Name = "Komentar")]
    public string Komentar { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Kreirano")]
    public DateTime Kreirano { get; set; } = DateTime.UtcNow;

    public string RezervacijaLabel { get; set; } = string.Empty;
    public string AutorNaziv { get; set; } = string.Empty;
}
