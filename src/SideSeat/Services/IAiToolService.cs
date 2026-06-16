using System.Security.Claims;

namespace SideSeat.Services;

public interface IAiToolService
{
    IReadOnlyList<object> Definitions { get; }

    IReadOnlyList<object> GetDefinitions(ClaimsPrincipal principal);

    Task<string> ExecuteAsync(
        string toolName,
        string argumentsJson,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
