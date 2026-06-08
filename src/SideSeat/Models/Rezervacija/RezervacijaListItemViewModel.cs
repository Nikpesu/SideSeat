using SideSeat.Models;

namespace SideSeat.Models.ViewModels;

public class RezervacijaListItemViewModel
{
    public Rezervacija Rezervacija { get; set; } = null!;
    public bool CanRate { get; set; }
    public bool HasRated { get; set; }
    public string RateTargetLabel { get; set; } = string.Empty;
}

public class RezervacijaListViewModel
{
    public IReadOnlyList<RezervacijaListItemViewModel> Rezervacije { get; set; } = Array.Empty<RezervacijaListItemViewModel>();
    public string SelectedView { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EmptyMessage { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool CanViewRideReservations { get; set; }
    public string SelectedStatus { get; set; } = "all";
    public int AllCount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int RejectedCount { get; set; }
    public int CompletedCount { get; set; }
}
