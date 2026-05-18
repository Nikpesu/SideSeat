using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Auth;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Lozinka")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Zapamti me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
