using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.ViewModels;

public class RezervacijaAdminFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "Voznja")]
    public int VoznjaId { get; set; }

    [Required]
    [Display(Name = "Putnik")]
    public int PutnikId { get; set; }

    [Range(1, 10)]
    [Display(Name = "Broj mjesta")]
    public int BrojMjesta { get; set; } = 1;

    [Required]
    [Display(Name = "Status")]
    public StatusRezervacije Status { get; set; }

    [Display(Name = "Vrijeme rezervacije")]
    public DateTime VrijemeRezervacije { get; set; } = DateTime.UtcNow;

    [Display(Name = "Napomena")]
    [StringLength(500)]
    public string Napomena { get; set; } = string.Empty;

    public decimal CijenaUkupno { get; set; }

    public string PutnikNaziv { get; set; } = string.Empty;
    public string VoznjaNaziv { get; set; } = string.Empty;
}
