using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SideSeat.Models;

public class AppUser : IdentityUser<int>
{
    [Required]
    [StringLength(11, MinimumLength = 11)]
    [RegularExpression("^[0-9]*$")]
    public string OIB { get; set; } = string.Empty;

    [Required]
    [StringLength(13, MinimumLength = 13)]
    [RegularExpression("^[0-9]*$")]
    public string JMBG { get; set; } = string.Empty;

    public int? KorisnikId { get; set; }
    public virtual Korisnik? Korisnik { get; set; }
}
