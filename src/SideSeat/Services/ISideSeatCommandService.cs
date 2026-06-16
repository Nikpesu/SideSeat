using System.Security.Claims;
using SideSeat.Models.Commands;

namespace SideSeat.Services;

public interface ISideSeatCommandService
{
    Task<CommandResult> ExecuteAsync<T>(
        string actionType,
        T command,
        ClaimsPrincipal principal,
        string source,
        CancellationToken cancellationToken = default);

    Task<CommandResult> ExecutePendingAsync(
        PendingActionEnvelope action,
        ClaimsPrincipal principal,
        string source,
        CancellationToken cancellationToken);
}
