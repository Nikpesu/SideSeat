using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SideSeat.Services;

namespace SideSeat.Controllers;

[Authorize]
public sealed class AiActionController(IPendingActionService pendingActions) : Controller
{
    [HttpGet("AiAction/Review/{token}")]
    public IActionResult Review(string token)
    {
        var descriptor = pendingActions.GetDescriptor(token, User);
        if (descriptor is null)
        {
            return NotFound();
        }

        return View(descriptor);
    }

    [HttpPost]
    [Route("AiAction/Confirm/{token?}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(string token, CancellationToken cancellationToken)
    {
        var result = await pendingActions.ConfirmAsync(token, User, "AI Review", cancellationToken);
        TempData["AiActionStatus"] = result.Message;
        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.Link))
        {
            return LocalRedirect(result.Link);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Route("AiAction/Cancel/{token?}")]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(string token)
    {
        TempData["AiActionStatus"] = pendingActions.Cancel(token, User)
            ? "Akcija je otkazana."
            : "Akcija ne postoji ili pripada drugom korisniku.";
        return RedirectToAction("Index", "Home");
    }
}
