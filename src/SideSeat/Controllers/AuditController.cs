using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;

namespace SideSeat.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AuditController(SideSeatDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(
        string? source,
        string? action,
        bool? succeeded,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(log => log.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(log => log.Action.Contains(action));
        }

        if (succeeded.HasValue)
        {
            query = query.Where(log => log.Succeeded == succeeded.Value);
        }

        ViewBag.Source = source;
        ViewBag.Action = action;
        ViewBag.Succeeded = succeeded;
        return View(await query
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken));
    }
}
