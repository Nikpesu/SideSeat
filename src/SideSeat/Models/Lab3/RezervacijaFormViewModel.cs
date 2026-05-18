using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Lab3;

public class RezervacijaFormViewModel
{
    [Required]
    public int VoznjaId { get; set; }

    [Range(1, 10)]
    [Display(Name = "Broj mjesta")]
    public int BrojMjesta { get; set; } = 1;

    [Display(Name = "Napomena")]
    public string Napomena { get; set; } = string.Empty;
}
