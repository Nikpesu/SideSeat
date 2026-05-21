using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.ViewModels;

public class KorisnikFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(80)]
    [Display(Name = "Ime")]
    public string Ime { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    [Display(Name = "Prezime")]
    public string Prezime { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(160)]
    [Display(Name = "Adresa")]
    public string Adresa { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    [Display(Name = "Mobitel")]
    public string BrojMobitela { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Datum registracije")]
    public DateTime DatumRegistracije { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Tip korisnika")]
    public TipKorisnika Tip { get; set; }

    [Display(Name = "Aktivan")]
    public bool JeAktivan { get; set; }

    [Display(Name = "KYC podnesen")]
    public bool KycPodnesen { get; set; }

    [StringLength(11, MinimumLength = 11)]
    [Display(Name = "OIB")]
    public string? KycOib { get; set; }

    [StringLength(40)]
    [Display(Name = "Broj osobne")]
    public string? KycBrojOsobne { get; set; }

    [StringLength(40)]
    [Display(Name = "Broj vozacke")]
    public string? KycBrojVozacke { get; set; }

    [Display(Name = "Datum rodenja")]
    public DateTime? KycDatumRodenja { get; set; }

    [Display(Name = "Vozilo")]
    public int? VoziloId { get; set; }

    public string VoziloNaziv { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [MinLength(6)]
    [Display(Name = "Lozinka")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Lozinke se ne podudaraju.")]
    [Display(Name = "Potvrda lozinke")]
    public string? ConfirmPassword { get; set; }
}
