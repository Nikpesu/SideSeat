using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Auth;

public class UserSettingsViewModel
{
    [Required]
    [Display(Name = "Adresa")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Uloga putnika")]
    public bool IsRider { get; set; }

    [Display(Name = "Uloga vozaca")]
    public bool IsDriver { get; set; }

    public bool KycPodnesen { get; set; }
}
