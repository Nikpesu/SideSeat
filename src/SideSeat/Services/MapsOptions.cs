namespace SideSeat.Services;

public sealed class MapsOptions
{
    public const string SectionName = "Maps";

    private const string DefaultTileUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    private const string DefaultAttribution =
        "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors";
    private const string DefaultNominatimBaseUrl = "https://nominatim.openstreetmap.org";
    private const string DefaultRoutingBaseUrl = "https://router.project-osrm.org";
    private const string DefaultUserAgent =
        "SideSeat/0.28 (+https://github.com/Nikpesu/SideSeat)";

    private string? _tileUrl = DefaultTileUrl;
    private string? _tileAttribution = DefaultAttribution;
    private string? _nominatimBaseUrl = DefaultNominatimBaseUrl;
    private string? _nominatimUserAgent = DefaultUserAgent;
    private string? _routingBaseUrl = DefaultRoutingBaseUrl;

    public string TileUrl
    {
        get => string.IsNullOrWhiteSpace(_tileUrl) ? DefaultTileUrl : _tileUrl;
        set => _tileUrl = value;
    }

    public string TileAttribution
    {
        get => string.IsNullOrWhiteSpace(_tileAttribution)
            ? DefaultAttribution
            : _tileAttribution;
        set => _tileAttribution = value;
    }

    public string NominatimBaseUrl
    {
        get => string.IsNullOrWhiteSpace(_nominatimBaseUrl)
            ? DefaultNominatimBaseUrl
            : _nominatimBaseUrl;
        set => _nominatimBaseUrl = value;
    }

    public string NominatimUserAgent
    {
        get => string.IsNullOrWhiteSpace(_nominatimUserAgent)
            ? DefaultUserAgent
            : _nominatimUserAgent;
        set => _nominatimUserAgent = value;
    }

    public string RoutingBaseUrl
    {
        get => string.IsNullOrWhiteSpace(_routingBaseUrl)
            ? DefaultRoutingBaseUrl
            : _routingBaseUrl;
        set => _routingBaseUrl = value;
    }

    public int RoutingTimeoutMilliseconds { get; set; } = 750;

    public string ContactEmail { get; set; } = string.Empty;
}
