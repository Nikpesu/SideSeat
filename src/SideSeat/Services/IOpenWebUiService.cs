using System.Security.Claims;
using SideSeat.Models.Ai;

namespace SideSeat.Services;

public interface IOpenWebUiService
{
    bool IsConfigured { get; }
    Task<AiChatResponse> ChatAsync(
        AiChatRequest request,
        string applicationContext,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
