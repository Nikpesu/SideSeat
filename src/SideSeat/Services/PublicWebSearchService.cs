using System.Net;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SideSeat.Services;

public sealed partial class PublicWebSearchService(
    IHttpClientFactory httpClientFactory,
    IOptions<PublicWebSearchOptions> options,
    IMemoryCache cache,
    ILogger<PublicWebSearchService> logger) : IPublicWebSearchService
{
    private readonly PublicWebSearchOptions _options = options.Value;

    public async Task<PublicWebSearchResponse> SearchAsync(
        PublicWebSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = NormalizeQuery(request.Query);
        var source = NormalizeSource(request.Source);
        var language = NormalizeLanguage(request.Language);
        var maxResults = Math.Clamp(_options.MaxResults, 1, 10);
        var limit = Math.Clamp(request.Limit <= 0 ? maxResults : request.Limit, 1, maxResults);

        if (!_options.Enabled)
        {
            return Empty(query, source, language, "Javna web pretraga je isključena u konfiguraciji.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return Empty(query, source, language, "Upit za pretragu ne smije biti prazan.");
        }

        var cacheKey = $"public-web-search:{source}:{language}:{limit}:{query.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out PublicWebSearchResponse? cached) && cached is not null)
        {
            return cached;
        }

        var results = new List<PublicWebSearchResult>();
        var warnings = new List<string>();

        if (source is "auto" or "wikipedia")
        {
            await AddResultsAsync(
                results,
                warnings,
                () => SearchWikipediaAsync(query, language, limit, cancellationToken),
                cancellationToken);
        }

        if (source is "auto" or "internet")
        {
            await AddResultsAsync(
                results,
                warnings,
                () => SearchDuckDuckGoAsync(query, language, limit, cancellationToken),
                cancellationToken);
        }

        var deduplicated = results
            .Where(result => !string.IsNullOrWhiteSpace(result.Title) &&
                             !string.IsNullOrWhiteSpace(result.Url))
            .GroupBy(result => result.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(limit)
            .ToArray();

        var response = new PublicWebSearchResponse(
            query,
            source,
            language,
            DateTimeOffset.UtcNow,
            deduplicated,
            warnings.Count > 0 ? string.Join(" ", warnings.Distinct()) : null);

        cache.Set(
            cacheKey,
            response,
            TimeSpan.FromMinutes(Math.Clamp(_options.CacheMinutes, 1, 1440)));

        return response;
    }

    private async Task AddResultsAsync(
        List<PublicWebSearchResult> target,
        List<string> warnings,
        Func<Task<IReadOnlyList<PublicWebSearchResult>>> search,
        CancellationToken cancellationToken)
    {
        try
        {
            target.AddRange(await search());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            warnings.Add("Jedan vanjski izvor nije odgovorio prije isteka vremena.");
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Public web search provider request failed.");
            warnings.Add("Jedan vanjski izvor trenutačno nije dostupan.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Public web search provider returned invalid JSON.");
            warnings.Add("Jedan vanjski izvor vratio je neispravan odgovor.");
        }
    }

    private async Task<IReadOnlyList<PublicWebSearchResult>> SearchWikipediaAsync(
        string query,
        string language,
        int limit,
        CancellationToken cancellationToken)
    {
        var apiUrl = BuildWikipediaApiUrl(language);
        var uri = new UriBuilder(apiUrl)
        {
            Query = BuildQueryString(new Dictionary<string, string?>
            {
                ["action"] = "query",
                ["list"] = "search",
                ["srsearch"] = query,
                ["srlimit"] = limit.ToString(CultureInfo.InvariantCulture),
                ["utf8"] = "1",
                ["format"] = "json"
            })
        }.Uri;

        using var payload = await GetJsonAsync(uri, cancellationToken);
        if (!payload.RootElement.TryGetProperty("query", out var queryNode) ||
            !queryNode.TryGetProperty("search", out var searchNode) ||
            searchNode.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return searchNode.EnumerateArray()
            .Select(item =>
            {
                var title = item.TryGetProperty("title", out var titleNode)
                    ? titleNode.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(title))
                {
                    return null;
                }

                var snippet = item.TryGetProperty("snippet", out var snippetNode)
                    ? CleanSnippet(snippetNode.GetString())
                    : string.Empty;
                var wikiTitle = Uri.EscapeDataString(title.Replace(' ', '_'));
                var url = $"https://{language}.wikipedia.org/wiki/{wikiTitle}";

                return new PublicWebSearchResult(
                    title,
                    snippet,
                    url,
                    "Wikipedia");
            })
            .Where(result => result is not null)
            .Cast<PublicWebSearchResult>()
            .Take(limit)
            .ToArray();
    }

    private async Task<IReadOnlyList<PublicWebSearchResult>> SearchDuckDuckGoAsync(
        string query,
        string language,
        int limit,
        CancellationToken cancellationToken)
    {
        var uri = new UriBuilder(_options.DuckDuckGoApiUrl)
        {
            Query = BuildQueryString(new Dictionary<string, string?>
            {
                ["q"] = query,
                ["format"] = "json",
                ["no_html"] = "1",
                ["skip_disambig"] = "1",
                ["kl"] = language == "hr" ? "hr-hr" : "us-en"
            })
        }.Uri;

        using var payload = await GetJsonAsync(uri, cancellationToken);
        var results = new List<PublicWebSearchResult>();

        var heading = ReadString(payload.RootElement, "Heading");
        var abstractText = ReadString(payload.RootElement, "AbstractText");
        var abstractUrl = ReadString(payload.RootElement, "AbstractURL");
        if (!string.IsNullOrWhiteSpace(heading) &&
            !string.IsNullOrWhiteSpace(abstractUrl))
        {
            results.Add(new PublicWebSearchResult(
                heading,
                CleanSnippet(abstractText),
                abstractUrl,
                "DuckDuckGo"));
        }

        AddDuckDuckGoTopics(results, payload.RootElement, limit);
        return results.Take(limit).ToArray();
    }

    private async Task<JsonDocument> GetJsonAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(Math.Clamp(_options.TimeoutMilliseconds, 500, 10000)));

        var client = httpClientFactory.CreateClient("PublicWebSearch");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(_options.UserAgent)
            ? "SideSeat/1.0"
            : _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            timeout.Token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        return await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
    }

    private void AddDuckDuckGoTopics(
        List<PublicWebSearchResult> results,
        JsonElement root,
        int limit)
    {
        if (!root.TryGetProperty("RelatedTopics", out var topics) ||
            topics.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        AddDuckDuckGoTopicArray(results, topics, limit);
    }

    private void AddDuckDuckGoTopicArray(
        List<PublicWebSearchResult> results,
        JsonElement topics,
        int limit)
    {
        foreach (var topic in topics.EnumerateArray())
        {
            if (results.Count >= limit)
            {
                return;
            }

            if (topic.TryGetProperty("Topics", out var nested) &&
                nested.ValueKind == JsonValueKind.Array)
            {
                AddDuckDuckGoTopicArray(results, nested, limit);
                continue;
            }

            var text = ReadString(topic, "Text");
            var url = ReadString(topic, "FirstURL");
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var title = text.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
            results.Add(new PublicWebSearchResult(
                title,
                CleanSnippet(text),
                url,
                "DuckDuckGo"));
        }
    }

    private string BuildWikipediaApiUrl(string language)
    {
        var template = string.IsNullOrWhiteSpace(_options.WikipediaApiUrlTemplate)
            ? "https://{language}.wikipedia.org/w/api.php"
            : _options.WikipediaApiUrlTemplate;
        return template.Replace("{language}", language, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildQueryString(IReadOnlyDictionary<string, string?> values) =>
        string.Join(
            "&",
            values
                .Where(pair => pair.Value is not null)
                .Select(pair =>
                    $"{WebUtility.UrlEncode(pair.Key)}={WebUtility.UrlEncode(pair.Value)}"));

    private static string NormalizeQuery(string query)
    {
        var trimmed = (query ?? string.Empty).Trim();
        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }

    private static string NormalizeSource(string source) =>
        source?.Trim().ToLowerInvariant() switch
        {
            "wikipedia" => "wikipedia",
            "internet" => "internet",
            _ => "auto"
        };

    private static string NormalizeLanguage(string language) =>
        language?.Trim().ToLowerInvariant() switch
        {
            "en" => "en",
            _ => "hr"
        };

    private static string CleanSnippet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutHtml = HtmlTagRegex().Replace(value, string.Empty);
        var decoded = WebUtility.HtmlDecode(withoutHtml);
        var collapsed = WhitespaceRegex().Replace(decoded, " ").Trim();
        return collapsed.Length <= 500 ? collapsed : collapsed[..497] + "...";
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static PublicWebSearchResponse Empty(
        string query,
        string source,
        string language,
        string warning) =>
        new(
            query,
            source,
            language,
            DateTimeOffset.UtcNow,
            [],
            warning);

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
