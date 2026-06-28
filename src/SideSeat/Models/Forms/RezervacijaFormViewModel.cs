using System.ComponentModel.DataAnnotations;
using SideSeat.Models;

namespace SideSeat.Models.Forms;

public class RezervacijaFormViewModel
{
    [Required]
    public int VoznjaId { get; set; }

    [Range(1, 10)]
    [Display(Name = "Broj mjesta")]
    public int BrojMjesta { get; set; } = 1;

    [Display(Name = "Način plaćanja")]
    public NacinPlacanja NacinPlacanja { get; set; } = NacinPlacanja.SideSeatSaldo;

    [Range(0, 10000)]
    [Display(Name = "Napojnica")]
    public decimal Napojnica { get; set; }

    [Display(Name = "Napomena")]
    public string Napomena { get; set; } = string.Empty;
}
