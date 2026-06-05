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
    public DbSet<SaldoTransakcija> SaldoTransakcije => Set<SaldoTransakcija>();
    public DbSet<VoznjaAttachment> VoznjaAttachments => Set<VoznjaAttachment>();

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

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Korisnik)
            .WithOne()
            .HasForeignKey<AppUser>(u => u.KorisnikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.KorisnikId)
            .IsUnique()
            .HasFilter("[KorisnikId] IS NOT NULL");

        modelBuilder.Entity<VoznjaAttachment>()
            .HasOne(a => a.Voznja)
            .WithMany(v => v.Attachments)
            .HasForeignKey(a => a.VoznjaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
