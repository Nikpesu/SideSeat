using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models;

public class Grad
{
    [Key]
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string Drzava { get; set; } = string.Empty;
    public string PostanskiBroj { get; set; } = string.Empty;
}