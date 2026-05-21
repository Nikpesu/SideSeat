namespace SideSeat.Models.ViewModels;

public class DateTimeInputViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public DateTime? Value { get; set; }
    public bool DateOnly { get; set; }
    public string? HelpText { get; set; }
    public bool Required { get; set; }
}
