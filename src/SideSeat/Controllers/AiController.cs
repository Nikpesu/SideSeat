using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SideSeat.Models.Ai;
using SideSeat.Services;

namespace SideSeat.Controllers;

[ApiController]
[Route("api/ai")]
[EnableRateLimiting("ai")]
public sealed class AiController(
    IOpenWebUiService openWebUi,
    IAiContextService aiContext,
    ILogger<AiController> logger) : ControllerBase
{
    [HttpPost("chat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Chat(
        [FromBody] AiChatRequest request,
        CancellationToken cancellationToken)
    {
        if (!openWebUi.IsConfigured)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "SideSeat AI nije konfiguriran.",
                detail: "Postavi AI_API_TYPE, AI_BASE_URL i AI_API_KEY u Docker konfiguraciji.");
        }

        if (request.Messages.Count > 20 ||
            request.Messages.Any(message =>
                string.IsNullOrWhiteSpace(message.Content) ||
                message.Content.Length > 4000 ||
                message.Role is not ("user" or "assistant")))
        {
            return BadRequest(new { message = "Razgovor sadrži neispravnu ili predugu poruku." });
        }

        try
        {
            var applicationContext = await aiContext.BuildAsync(
                User,
                request.PageTitle,
                request.PagePath,
                cancellationToken);
            return Ok(await openWebUi.ChatAsync(
                request,
                applicationContext,
                User,
                cancellationToken));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "SideSeat AI trenutačno kasni.",
                detail: "Pokušaj ponovno za nekoliko trenutaka.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            logger.LogWarning(exception, "AI provider request failed.");
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "SideSeat AI trenutačno nije dostupan.",
                detail: "Veza s AI servisom nije uspjela. Pokušaj ponovno kasnije.");
        }
    }
}
