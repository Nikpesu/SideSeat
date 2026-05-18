using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Auth;

public class DriverKycViewModel
{
    [Required]
    [Display(Name = "OIB")]
    [StringLength(11, MinimumLength = 11)]
    public string Oib { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Broj osobne iskaznice")]
    public string BrojOsobne { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Broj vozacke dozvole")]
    public string BrojVozacke { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Datum rodenja")]
    public DateTime DatumRodenja { get; set; } = DateTime.UtcNow.AddYears(-20).Date;
}
