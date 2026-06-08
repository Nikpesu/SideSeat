using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Security;

namespace SideSeat.Controllers;

[Authorize]
public class ObavijestController : Controller
{
    private readonly SideSeatDbContext _db;

    public ObavijestController(SideSeatDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var korisnikId = User.GetKorisnikId();
        if (!korisnikId.HasValue)
        {
            return Challenge();
        }

        var obavijest = await _db.Obavijesti
            .FirstOrDefaultAsync(o => o.Id == id && o.KorisnikId == korisnikId.Value);
        if (obavijest is null)
        {
            return NotFound();
        }

        obavijest.Procitano = true;
        await _db.SaveChangesAsync();

        return !string.IsNullOrWhiteSpace(obavijest.Link) && Url.IsLocalUrl(obavijest.Link)
            ? LocalRedirect(obavijest.Link)
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(string? returnUrl)
    {
        var korisnikId = User.GetKorisnikId();
        if (!korisnikId.HasValue)
        {
            return Challenge();
        }

        var unread = await _db.Obavijesti
            .Where(o => o.KorisnikId == korisnikId.Value && !o.Procitano)
            .ToListAsync();
        foreach (var obavijest in unread)
        {
            obavijest.Procitano = true;
        }

        await _db.SaveChangesAsync();
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
    }
}
