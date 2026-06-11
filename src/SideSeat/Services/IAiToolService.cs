using System.Security.Claims;

namespace SideSeat.Services;

public interface IAiToolService
{
    IReadOnlyList<object> Definitions { get; }

    Task<string> ExecuteAsync(
        string toolName,
        string argumentsJson,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
