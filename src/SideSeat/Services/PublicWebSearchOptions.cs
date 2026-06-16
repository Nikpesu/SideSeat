namespace SideSeat.Services;

public sealed class PublicWebSearchOptions
{
    public const string SectionName = "PublicWebSearch";

    public bool Enabled { get; set; } = true;
    public string WikipediaApiUrlTemplate { get; set; } = "https://{language}.wikipedia.org/w/api.php";
    public string DuckDuckGoApiUrl { get; set; } = "https://api.duckduckgo.com/";
    public string UserAgent { get; set; } = "SideSeat/0.30 (+https://github.com/Nikpesu/SideSeat)";
    public int TimeoutMilliseconds { get; set; } = 3000;
    public int CacheMinutes { get; set; } = 15;
    public int MaxResults { get; set; } = 5;
}
