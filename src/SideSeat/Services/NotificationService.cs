using SideSeat.Data;
using SideSeat.Models;

namespace SideSeat.Services;

public interface INotificationService
{
    void Add(int korisnikId, string naslov, string poruka, string tip, string? link = null);
}

public class NotificationService : INotificationService
{
    private readonly SideSeatDbContext _db;

    public NotificationService(SideSeatDbContext db)
    {
        _db = db;
    }

    public void Add(int korisnikId, string naslov, string poruka, string tip, string? link = null)
    {
        _db.Obavijesti.Add(new Obavijest
        {
            KorisnikId = korisnikId,
            Naslov = naslov,
            Poruka = poruka,
            Tip = tip,
            Link = link,
            Kreirano = DateTime.UtcNow,
            Procitano = false
        });
    }
}
