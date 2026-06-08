using Microsoft.EntityFrameworkCore;

namespace SideSeat.Data;

public static class DummyDataCleaner
{
    public static async Task RemoveAsync(SideSeatDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        db.OcjenaSlike.RemoveRange(db.OcjenaSlike);
        db.Ocjene.RemoveRange(db.Ocjene);
        db.Placanja.RemoveRange(db.Placanja);
        db.SaldoTransakcije.RemoveRange(db.SaldoTransakcije);
        db.Obavijesti.RemoveRange(db.Obavijesti);
        db.Rezervacije.RemoveRange(db.Rezervacije);
        db.Voznje.RemoveRange(db.Voznje);
        db.Vozila.RemoveRange(db.Vozila);
        db.Korisnici.RemoveRange(db.Korisnici);
        db.Gradovi.RemoveRange(db.Gradovi);

        await db.SaveChangesAsync();
    }
}
