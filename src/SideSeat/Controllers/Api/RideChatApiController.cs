using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;

namespace SideSeat.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/rides")]
public sealed class RideChatApiController(SideSeatDbContext dbContext) : ControllerBase
{
    [HttpGet("{id:int}/chat")]
    public async Task<IActionResult> Chat(int id, CancellationToken cancellationToken)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");
        var ride = await dbContext.Voznje
            .AsNoTracking()
            .Include(v => v.Vozac)
            .Include(v => v.Rezervacije).ThenInclude(r => r.Putnik)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (ride is null)
        {
            return NotFound();
        }

        var isDriver = isAdmin || ride.VozacId == userId.Value;
        var isPassenger = ride.Rezervacije.Any(r =>
            r.PutnikId == userId.Value && r.Status == StatusRezervacije.Potvrdena);
        if (!isDriver && !isPassenger)
        {
            return Forbid();
        }

        var messages = await dbContext.RideChatMessages
            .AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Where(m => m.VoznjaId == id)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(200)
            .Select(m => new
            {
                senderName = (m.Sender.Ime + " " + m.Sender.Prezime).Trim(),
                recipientId = m.RecipientId,
                recipientName = m.Recipient != null
                    ? (m.Recipient.Ime + " " + m.Recipient.Prezime).Trim()
                    : null,
                message = m.Message,
                createdAtUtc = m.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        IEnumerable<object> participants = isDriver
            ? ride.Rezervacije
                .Where(r => r.Status == StatusRezervacije.Potvrdena)
                .Select(r => new { id = r.PutnikId, name = (r.Putnik.Ime + " " + r.Putnik.Prezime).Trim() })
            : new[]
            {
                new { id = ride.VozacId, name = (ride.Vozac.Ime + " " + ride.Vozac.Prezime).Trim() + " (vozač)" }
            };

        return Ok(new { isDriver, participants, messages });
    }
}
