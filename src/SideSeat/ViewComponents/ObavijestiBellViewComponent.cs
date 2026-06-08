using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models.Notifications;
using SideSeat.Security;

namespace SideSeat.ViewComponents;

public class ObavijestiBellViewComponent : ViewComponent
{
    private readonly SideSeatDbContext _db;

    public ObavijestiBellViewComponent(SideSeatDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var korisnikId = UserClaimsPrincipal.GetKorisnikId();
        if (!korisnikId.HasValue)
        {
            return Content(string.Empty);
        }

        var unreadCount = await _db.Obavijesti
            .AsNoTracking()
            .CountAsync(o => o.KorisnikId == korisnikId.Value && !o.Procitano);
        var items = await _db.Obavijesti
            .AsNoTracking()
            .Where(o => o.KorisnikId == korisnikId.Value)
            .OrderByDescending(o => o.Kreirano)
            .Take(8)
            .Select(o => new NotificationBellItemViewModel
            {
                Id = o.Id,
                Title = o.Naslov,
                Message = o.Poruka,
                Type = o.Tip,
                CreatedAt = o.Kreirano,
                IsRead = o.Procitano
            })
            .ToListAsync();

        return View(new NotificationBellViewModel
        {
            UnreadCount = unreadCount,
            Items = items
        });
    }
}
