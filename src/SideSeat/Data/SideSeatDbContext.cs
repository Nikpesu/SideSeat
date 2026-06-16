using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SideSeat.Models;

namespace SideSeat.Data;

public class SideSeatDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public SideSeatDbContext(DbContextOptions<SideSeatDbContext> options) : base(options)
    {
    }

    public DbSet<Grad> Gradovi => Set<Grad>();
    public DbSet<Korisnik> Korisnici => Set<Korisnik>();
    public DbSet<Vozilo> Vozila => Set<Vozilo>();
    public DbSet<Voznja> Voznje => Set<Voznja>();
    public DbSet<Rezervacija> Rezervacije => Set<Rezervacija>();
    public DbSet<Placanje> Placanja => Set<Placanje>();
    public DbSet<OcjenaVoznje> Ocjene => Set<OcjenaVoznje>();
    public DbSet<OcjenaSlika> OcjenaSlike => Set<OcjenaSlika>();
    public DbSet<SaldoTransakcija> SaldoTransakcije => Set<SaldoTransakcija>();
    public DbSet<Obavijest> Obavijesti => Set<Obavijest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RideChatMessage> RideChatMessages => Set<RideChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Voznja>()
            .HasOne(v => v.PolazniGrad)
            .WithMany()
            .HasForeignKey(v => v.PolazniGradId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Voznja>()
            .HasOne(v => v.OdredisniGrad)
            .WithMany()
            .HasForeignKey(v => v.OdredisniGradId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Rezervacija>()
            .HasOne(r => r.Voznja)
            .WithMany(v => v.Rezervacije)
            .HasForeignKey(r => r.VoznjaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OcjenaVoznje>()
            .HasOne(o => o.Rezervacija)
            .WithMany()
            .HasForeignKey(o => o.RezervacijaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vozilo>()
            .HasOne(v => v.Vlasnik)
            .WithMany()
            .HasForeignKey(v => v.VlasnikId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Korisnik>()
            .HasOne(k => k.Vozilo)
            .WithMany()
            .HasForeignKey(k => k.VoziloId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OcjenaVoznje>()
            .HasOne(o => o.AdminFeedbackAutor)
            .WithMany()
            .HasForeignKey(o => o.AdminFeedbackAutorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Placanje>()
            .HasOne(p => p.Rezervacija)
            .WithMany()
            .HasForeignKey(p => p.RezervacijaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SaldoTransakcija>()
            .HasOne(t => t.Korisnik)
            .WithMany(k => k.SaldoTransakcije)
            .HasForeignKey(t => t.KorisnikId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Obavijest>()
            .HasOne(o => o.Korisnik)
            .WithMany(k => k.Obavijesti)
            .HasForeignKey(o => o.KorisnikId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RideChatMessage>()
            .HasOne(message => message.Voznja)
            .WithMany()
            .HasForeignKey(message => message.VoznjaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RideChatMessage>()
            .HasOne(message => message.Sender)
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RideChatMessage>()
            .HasOne(message => message.Recipient)
            .WithMany()
            .HasForeignKey(message => message.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RideChatMessage>()
            .HasIndex(message => new { message.VoznjaId, message.CreatedAtUtc });

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Korisnik)
            .WithOne()
            .HasForeignKey<AppUser>(u => u.KorisnikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.KorisnikId)
            .IsUnique()
            .HasFilter("[KorisnikId] IS NOT NULL");

        modelBuilder.Entity<OcjenaSlika>()
            .HasOne(s => s.OcjenaVoznje)
            .WithMany(o => o.Slike)
            .HasForeignKey(s => s.OcjenaVoznjeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Grad>()
            .HasIndex(g => new { g.Naziv, g.Drzava })
            .IsUnique();

        modelBuilder.Entity<Vozilo>()
            .HasIndex(v => v.Registracija)
            .IsUnique();

        modelBuilder.Entity<AuditLog>()
            .HasIndex(log => log.CreatedAtUtc);

        modelBuilder.Entity<Korisnik>().Property(item => item.Saldo).HasPrecision(18, 2);
        modelBuilder.Entity<Voznja>().Property(item => item.CijenaPoMjestu).HasPrecision(18, 2);
        modelBuilder.Entity<Rezervacija>().Property(item => item.CijenaUkupno).HasPrecision(18, 2);
        modelBuilder.Entity<Rezervacija>().Property(item => item.NacinPlacanja).HasDefaultValue(NacinPlacanja.SideSeatSaldo);
        modelBuilder.Entity<Rezervacija>().Property(item => item.Napojnica).HasPrecision(18, 2);
        modelBuilder.Entity<Rezervacija>().Property(item => item.LastLatitude).HasPrecision(9, 6);
        modelBuilder.Entity<Rezervacija>().Property(item => item.LastLongitude).HasPrecision(9, 6);
        modelBuilder.Entity<Placanje>().Property(item => item.Iznos).HasPrecision(18, 2);
        modelBuilder.Entity<SaldoTransakcija>().Property(item => item.Iznos).HasPrecision(18, 2);
        modelBuilder.Entity<SaldoTransakcija>().Property(item => item.SaldoPrije).HasPrecision(18, 2);
        modelBuilder.Entity<SaldoTransakcija>().Property(item => item.SaldoPoslije).HasPrecision(18, 2);
        modelBuilder.Entity<Vozilo>().Property(item => item.ProsjecnaPotrosnja).HasPrecision(8, 2);
        modelBuilder.Entity<Grad>().Property(item => item.Latitude).HasPrecision(9, 6);
        modelBuilder.Entity<Grad>().Property(item => item.Longitude).HasPrecision(9, 6);
    }
}
