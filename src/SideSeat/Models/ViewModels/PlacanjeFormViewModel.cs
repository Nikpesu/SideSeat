using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.ViewModels;

public class PlacanjeFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "Rezervacija")]
    public int RezervacijaId { get; set; }

    [Range(0.01, 100000)]
    [Display(Name = "Iznos")]
    public decimal Iznos { get; set; }

    [Required]
    [Display(Name = "Vrijeme placanja")]
    public DateTime VrijemePlacanja { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Nacin placanja")]
    public NacinPlacanja NacinPlacanja { get; set; }

    [Display(Name = "Uspjesno")]
    public bool Uspjesno { get; set; }

    public string RezervacijaLabel { get; set; } = string.Empty;
}
