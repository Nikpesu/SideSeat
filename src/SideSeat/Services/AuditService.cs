using System.Security.Claims;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;

namespace SideSeat.Services;

public sealed class AuditService(
    SideSeatDbContext dbContext,
    IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task WriteAsync(
        ClaimsPrincipal principal,
        string source,
        string action,
        string entityType,
        string? entityId,
        bool succeeded,
        string summary,
        CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        dbContext.AuditLogs.Add(new AuditLog
        {
            CreatedAtUtc = DateTime.UtcNow,
            KorisnikId = principal.GetKorisnikId(),
            Actor = Limit(principal.Identity?.Name ?? "anonymous", 120),
            Source = Limit(source, 40),
            Action = Limit(action, 80),
            EntityType = Limit(entityType, 80),
            EntityId = string.IsNullOrWhiteSpace(entityId) ? null : Limit(entityId, 80),
            Succeeded = succeeded,
            Summary = Limit(summary, 500),
            CorrelationId = Limit(
                context?.TraceIdentifier ?? System.Diagnostics.Activity.Current?.TraceId.ToString() ?? string.Empty,
                100)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string Limit(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum];
}
