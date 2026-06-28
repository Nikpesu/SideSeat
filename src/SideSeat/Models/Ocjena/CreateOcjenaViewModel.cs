using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SideSeat.Models.Ocjena;

public class CreateOcjenaViewModel
{
    public int RezervacijaId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public bool IsAdditional { get; set; }
    public bool CanTipDriver { get; set; }
    public decimal ExistingTip { get; set; }
    public string? SavedCardDisplay { get; set; }

    [Range(1, 5, ErrorMessage = "Ocjena mora biti izmedu 1 i 5.")]
    public int BrojZvjezdica { get; set; } = 5;

    [Required(ErrorMessage = "Komentar je obavezan.")]
    [StringLength(500, ErrorMessage = "Komentar moze imati najvise 500 znakova.")]
    public string Komentar { get; set; } = string.Empty;

    public List<IFormFile> Slike { get; set; } = new();

    [Range(0, 10000, ErrorMessage = "Napojnica mora biti izmedu 0 i 10000 EUR.")]
    [Display(Name = "Napojnica vozaču karticom")]
    public decimal Napojnica { get; set; }

    [Display(Name = "Ime na kartici")]
    public string? CardholderName { get; set; }

    [Display(Name = "Broj kartice")]
    public string? CardNumber { get; set; }

    [Display(Name = "Vrijedi do")]
    public string? CardExpiry { get; set; }

    [Display(Name = "CVV")]
    public string? CardCvv { get; set; }

    [Display(Name = "Spremi karticu za sljedeću uplatu")]
    public bool SaveCard { get; set; }
}
