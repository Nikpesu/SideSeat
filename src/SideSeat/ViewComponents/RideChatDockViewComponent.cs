using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Models;
using SideSeat.Security;

namespace SideSeat.ViewComponents;

public sealed class RideChatDockViewComponent : ViewComponent
{
    private readonly SideSeatDbContext _db;

    public RideChatDockViewComponent(SideSeatDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var principal = HttpContext.User;
        var userId = principal.GetKorisnikId();
        if (userId is null)
        {
            return View(new List<RideChatDockItem>());
        }

        var isAdmin = principal.IsInRole("Admin");
        var rides = await _db.Voznje
            .AsNoTracking()
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Where(v => v.Status == StatusVoznje.Planirana || v.Status == StatusVoznje.Aktivna)
            .Where(v => isAdmin ||
                v.VozacId == userId.Value ||
                v.Rezervacije.Any(r => r.PutnikId == userId.Value && r.Status == StatusRezervacije.Potvrdena))
            .OrderBy(v => v.Polazak)
            .Take(25)
            .Select(v => new RideChatDockItem
            {
                Id = v.Id,
                Label = v.Polazak.ToString("dd.MM. HH:mm") + " — " +
                        v.PolazniGrad!.Naziv + " → " + v.OdredisniGrad!.Naziv
            })
            .ToListAsync();

        return View(rides);
    }
}

public sealed class RideChatDockItem
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
