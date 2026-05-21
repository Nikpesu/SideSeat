namespace SideSeat.Models.ViewModels;

public class AutocompleteLookupViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string SelectedId { get; set; } = string.Empty;
    public string SelectedText { get; set; } = string.Empty;
    public string Placeholder { get; set; } = "Pretrazi...";
    public string? HelpText { get; set; }
    public int MinimumLength { get; set; } = 0;
    public bool Required { get; set; }
    public string? RouteMode { get; set; }
    public string? RoutePeerName { get; set; }
}
