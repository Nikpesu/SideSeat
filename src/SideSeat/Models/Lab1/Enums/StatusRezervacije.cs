using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models;

public enum StatusRezervacije
{
    [Display(Name = "U procesu potvrde")]
    UProcesuPotvrde = 0,

    [Display(Name = "Potvrđena")]
    Potvrdena,

    [Display(Name = "Odbijena")]
    Odbijena,

    [Display(Name = "Završena")]
    Zavrsena
}
