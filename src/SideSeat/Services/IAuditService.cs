using System.Security.Claims;

namespace SideSeat.Services;

public interface IAuditService
{
    Task WriteAsync(
        ClaimsPrincipal principal,
        string source,
        string action,
        string entityType,
        string? entityId,
        bool succeeded,
        string summary,
        CancellationToken cancellationToken = default);
}
