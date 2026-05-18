using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SideSeat.Data;
using SideSeat.Security;

namespace SideSeat.Controllers;

[Authorize]
public class ConfirmationController : Controller
{
    private readonly SideSeatDbContext _db;

    public ConfirmationController(SideSeatDbContext db)
    {
        _db = db;
    }

    public IActionResult Ride(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var voznja = _db.Voznje
            .AsNoTracking()
            .Include(v => v.PolazniGrad)
            .Include(v => v.OdredisniGrad)
            .Include(v => v.Vozac)
            .FirstOrDefault(v => v.Id == id);
        if (voznja is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && voznja.VozacId != userId.Value)
        {
            return Forbid();
        }

        return View(voznja);
    }

    public IActionResult Reservation(int id)
    {
        var userId = User.GetKorisnikId();
        if (userId is null)
        {
            return Challenge();
        }

        var rezervacija = _db.Rezervacije
            .AsNoTracking()
            .Include(r => r.Putnik)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.PolazniGrad)
            .Include(r => r.Voznja)
            .ThenInclude(v => v.OdredisniGrad)
            .FirstOrDefault(r => r.Id == id);
        if (rezervacija is null)
        {
            return NotFound();
        }

        var canAccess = User.IsInRole("Admin")
                        || rezervacija.PutnikId == userId.Value
                        || rezervacija.Voznja.VozacId == userId.Value;
        if (!canAccess)
        {
            return Forbid();
        }

        return View(rezervacija);
    }
}
