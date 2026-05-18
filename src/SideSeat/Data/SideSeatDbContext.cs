using Microsoft.EntityFrameworkCore;
using SideSeat.Models;

namespace SideSeat.Data;

public class SideSeatDbContext : DbContext
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

        modelBuilder.Entity<Grad>().HasData(
            new Grad { Id = 1, Naziv = "Zagreb", Drzava = "Hrvatska", PostanskiBroj = "10000" },
            new Grad { Id = 2, Naziv = "Split", Drzava = "Hrvatska", PostanskiBroj = "21000" }
        );

        modelBuilder.Entity<Korisnik>().HasData(
            new Korisnik
            {
                Id = 1,
                Ime = "Marko",
                Prezime = "Maric",
                Email = "marko@example.com",
                LozinkaHash = "WjgcARlXlANvPrwzymGupw==.mAJGLSfwA1qBVtNB5RA7flpqKTF6m4GaYykrX7DvfRM=",
                Adresa = "Ilica 10, Zagreb",
                BrojMobitela = "0911111111",
                DatumRegistracije = new DateTime(2026, 5, 1, 8, 0, 0),
                Tip = TipKorisnika.Vozac,
                JeAktivan = true,
                KycPodnesen = true,
                KycOib = "12345678901",
                KycBrojOsobne = "12345678",
                KycBrojVozacke = "HR-VOZ-001",
                KycDatumRodenja = new DateTime(1990, 4, 12),
                VoziloId = null
            },
            new Korisnik
            {
                Id = 2,
                Ime = "Ivana",
                Prezime = "Ivic",
                Email = "ivana@example.com",
                LozinkaHash = "d+Ekke4YV8yR6E71CavL1w==.emo+8SBlglGcaUZ6zdYXv/sWOHz95xzNsKI3rkmB8os=",
                Adresa = "Marmontova 21, Split",
                BrojMobitela = "0922222222",
                DatumRegistracije = new DateTime(2026, 5, 1, 9, 0, 0),
                Tip = TipKorisnika.Putnik,
                JeAktivan = true,
                KycPodnesen = false,
                VoziloId = null
            },
            new Korisnik
            {
                Id = 3,
                Ime = "Ana",
                Prezime = "Admin",
                Email = "admin@example.com",
                LozinkaHash = "UJon3KrOrz+TJuEAH49PBA==.mtr30yYsJ5J4MS/edvNKLbZ/aOhiZ+W0gPp24rGz0JY=",
                Adresa = "Savska 100, Zagreb",
                BrojMobitela = "0933333333",
                DatumRegistracije = new DateTime(2026, 5, 1, 10, 0, 0),
                Tip = TipKorisnika.Admin,
                JeAktivan = true,
                KycPodnesen = true,
                VoziloId = null
            }
        );

        modelBuilder.Entity<Vozilo>().HasData(
            new Vozilo
            {
                Id = 1,
                Marka = "Skoda",
                Model = "Octavia",
                Registracija = "ZG-1234-AA",
                GodinaProizvodnje = 2021,
                BrojSjedala = 5,
                Boja = "Siva",
                ProsjecnaPotrosnja = 6.5m,
                VlasnikId = 1
            }
        );

        modelBuilder.Entity<Voznja>().HasData(
            new Voznja
            {
                Id = 1,
                VozacId = 1,
                PolazniGradId = 1,
                OdredisniGradId = 2,
                Polazak = new DateTime(2026, 5, 10, 8, 0, 0),
                OcekivaniDolazak = new DateTime(2026, 5, 10, 12, 0, 0),
                CijenaPoMjestu = 15.00m,
                UkupnoMjesta = 4,
                SlobodnaMjesta = 3,
                Opis = "Jutarnja voznja",
                Status = StatusVoznje.Planirana
            }
        );

        modelBuilder.Entity<Rezervacija>().HasData(
            new Rezervacija
            {
                Id = 1,
                VoznjaId = 1,
                PutnikId = 2,
                BrojMjesta = 1,
                CijenaUkupno = 15.00m,
                VrijemeRezervacije = new DateTime(2026, 5, 2, 9, 30, 0),
                Status = StatusRezervacije.Aktivna,
                Napomena = "Bez prtljage"
            }
        );

        modelBuilder.Entity<Placanje>().HasData(
            new Placanje
            {
                Id = 1,
                RezervacijaId = 1,
                Iznos = 15.00m,
                VrijemePlacanja = new DateTime(2026, 5, 2, 10, 0, 0),
                NacinPlacanja = NacinPlacanja.Kartica,
                Uspjesno = true
            }
        );

        modelBuilder.Entity<OcjenaVoznje>().HasData(
            new OcjenaVoznje
            {
                Id = 1,
                RezervacijaId = 1,
                AutorId = 2,
                BrojZvjezdica = 5,
                Komentar = "Odlicno",
                Kreirano = new DateTime(2026, 5, 12, 14, 0, 0)
            }
        );
    }
}
