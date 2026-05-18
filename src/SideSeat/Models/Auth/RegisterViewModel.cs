using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Auth;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8)]
    [Display(Name = "Lozinka")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [Display(Name = "Potvrda lozinke")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Adresa")]
    public string Address { get; set; } = string.Empty;
}
