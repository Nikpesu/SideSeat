namespace SideSeat.Models.ViewModels;

public sealed class RouteMapViewModel
{
    public string StartName { get; set; } = string.Empty;
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public string EndName { get; set; } = string.Empty;
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public string Title { get; set; } = "Prikaz rute";
    public string CssClass { get; set; } = string.Empty;
}
