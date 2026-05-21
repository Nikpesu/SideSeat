using SideSeat.Models;

namespace SideSeat.Models.ViewModels;

public class RezervacijaListItemViewModel
{
    public Rezervacija Rezervacija { get; set; } = null!;
    public bool CanRate { get; set; }
    public bool HasRated { get; set; }
    public string RateTargetLabel { get; set; } = string.Empty;
}
