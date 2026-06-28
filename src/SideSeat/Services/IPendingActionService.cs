using System.Security.Claims;
using SideSeat.Models.Commands;

namespace SideSeat.Services;

public interface IPendingActionService
{
    PendingActionDescriptor Create<T>(
        ClaimsPrincipal principal,
        string actionType,
        string title,
        string summary,
        T payload);

    PendingActionDescriptor? GetDescriptor(
        string token,
        ClaimsPrincipal principal);

    Task<CommandResult> ConfirmAsync(
        string token,
        ClaimsPrincipal principal,
        string source,
        CancellationToken cancellationToken);

    bool Cancel(string token, ClaimsPrincipal principal);
}
