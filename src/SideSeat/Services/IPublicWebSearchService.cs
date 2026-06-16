namespace SideSeat.Services;

public interface IPublicWebSearchService
{
    Task<PublicWebSearchResponse> SearchAsync(
        PublicWebSearchRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PublicWebSearchRequest(
    string Query,
    string Source,
    string Language,
    int Limit);

public sealed record PublicWebSearchResponse(
    string Query,
    string Source,
    string Language,
    DateTimeOffset RetrievedAt,
    IReadOnlyList<PublicWebSearchResult> Results,
    string? Warning = null);

public sealed record PublicWebSearchResult(
    string Title,
    string Snippet,
    string Url,
    string Source);
