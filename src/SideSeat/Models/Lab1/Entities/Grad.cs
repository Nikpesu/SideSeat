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
}