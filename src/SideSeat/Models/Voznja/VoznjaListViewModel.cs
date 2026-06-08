namespace SideSeat.Models.Rides;

public class VoznjaListViewModel
{
    public IReadOnlyList<Voznja> Voznje { get; set; } = Array.Empty<Voznja>();
    public string SelectedView { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EmptyMessage { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool CanViewDriving { get; set; }
    public string SelectedStatus { get; set; } = "all";
    public int AllCount { get; set; }
    public int PlannedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
}
