using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SideSeat.Models.Auth;

public class UserSettingsViewModel
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(100)]
    [Display(Name = "Ime")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(100)]
    [Display(Name = "Prezime")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Adresa")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Broj mobitela")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Uloga putnika")]
    public bool IsRider { get; set; }

    [Display(Name = "Uloga vozaca")]
    public bool IsDriver { get; set; }

    public bool KycPodnesen { get; set; }

    public bool CanDisableRider { get; set; } = true;

    public bool CanDisableDriver { get; set; } = true;

    [Display(Name = "Profilna slika")]
    public IFormFile? ProfileImage { get; set; }

    public string? CurrentProfileImagePath { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Trenutna lozinka")]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Nova lozinka")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "Nova lozinka mora imati najmanje 6 znakova.")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Ponovi novu lozinku")]
    public string? ConfirmNewPassword { get; set; }
}
