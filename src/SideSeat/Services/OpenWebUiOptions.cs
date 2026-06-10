namespace SideSeat.Services;

public sealed class OpenWebUiOptions
{
    public const string SectionName = "OpenWebUi";

    public string ApiType { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
