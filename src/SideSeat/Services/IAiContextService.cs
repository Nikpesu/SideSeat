using System.Security.Claims;

namespace SideSeat.Services;

public interface IAiContextService
{
    Task<string> BuildAsync(
        ClaimsPrincipal principal,
        string? pageTitle,
        string? pagePath,
        CancellationToken cancellationToken);
}
