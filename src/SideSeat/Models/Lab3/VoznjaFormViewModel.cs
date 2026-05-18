using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SideSeat.Models.Lab3;

public class VoznjaFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "Vozac")]
    public int VozacId { get; set; }

    [Required]
    [Display(Name = "Polazni grad")]
    public int PolazniGradId { get; set; }

    [Required]
    [Display(Name = "Odredisni grad")]
    public int OdredisniGradId { get; set; }

    [Required]
    [Display(Name = "Polazak")]
    public DateTime Polazak { get; set; }

    [Required]
    [Display(Name = "Ocekivani dolazak")]
    public DateTime OcekivaniDolazak { get; set; }

    [Range(0.01, 10000)]
    [Display(Name = "Cijena po mjestu")]
    public decimal CijenaPoMjestu { get; set; }

    [Range(1, 20)]
    [Display(Name = "Ukupno mjesta")]
    public int UkupnoMjesta { get; set; }

    [Range(0, 20)]
    [Display(Name = "Slobodna mjesta")]
    public int SlobodnaMjesta { get; set; }

    [Display(Name = "Opis")]
    public string Opis { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Status")]
    public StatusVoznje Status { get; set; }

    public List<SelectListItem> Vozaci { get; set; } = new();
    public List<SelectListItem> Gradovi { get; set; } = new();
    public bool CanSelectDriver { get; set; }
}
