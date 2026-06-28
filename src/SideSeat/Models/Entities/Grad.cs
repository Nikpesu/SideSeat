using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models;

public class Grad
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Naziv { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Drzava { get; set; } = string.Empty;

    [Required]
    [StringLength(12)]
    public string PostanskiBroj { get; set; } = string.Empty;

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }
}
