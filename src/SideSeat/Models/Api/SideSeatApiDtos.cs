using System.ComponentModel.DataAnnotations;

namespace SideSeat.Models.Api;

public class GradDto
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string Drzava { get; set; } = string.Empty;
    public string PostanskiBroj { get; set; } = string.Empty;
}

public class GradRequest
{
    [Required, StringLength(120)]
    public string Naziv { get; set; } = string.Empty;
    [Required, StringLength(120)]
    public string Drzava { get; set; } = string.Empty;
    [Required, StringLength(12)]
    public string PostanskiBroj { get; set; } = string.Empty;
}

public class KorisnikDto
{
    public int Id { get; set; }
    public string Ime { get; set; } = string.Empty;
    public string Prezime { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Adresa { get; set; } = string.Empty;
    public string BrojMobitela { get; set; } = string.Empty;
    public DateTime DatumRegistracije { get; set; }
    public TipKorisnika Tip { get; set; }
    public bool JeAktivan { get; set; }
    public bool KycPodnesen { get; set; }
    public decimal Saldo { get; set; }
}

public class KorisnikRequest
{
    [Required, StringLength(80)]
    public string Ime { get; set; } = string.Empty;
    [Required, StringLength(80)]
    public string Prezime { get; set; } = string.Empty;
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(160)]
    public string Adresa { get; set; } = string.Empty;
    [Required, StringLength(40)]
    public string BrojMobitela { get; set; } = string.Empty;
    public TipKorisnika Tip { get; set; } = TipKorisnika.Putnik;
    public bool JeAktivan { get; set; } = true;
}

public class VoziloDto
{
    public int Id { get; set; }
    public string Marka { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Registracija { get; set; } = string.Empty;
    public int GodinaProizvodnje { get; set; }
    public int BrojSjedala { get; set; }
    public string Boja { get; set; } = string.Empty;
    public decimal ProsjecnaPotrosnja { get; set; }
    public KorisnikDto? Vlasnik { get; set; }
}

public class VoziloRequest
{
    [Required, StringLength(80)]
    public string Marka { get; set; } = string.Empty;
    [Required, StringLength(80)]
    public string Model { get; set; } = string.Empty;
    [Required, StringLength(24)]
    public string Registracija { get; set; } = string.Empty;
    [Range(1980, 2035)]
    public int GodinaProizvodnje { get; set; }
    [Range(1, 8)]
    public int BrojSjedala { get; set; }
    [Required, StringLength(40)]
    public string Boja { get; set; } = string.Empty;
    [Range(0.1, 30)]
    public decimal ProsjecnaPotrosnja { get; set; }
    public int? VlasnikId { get; set; }
}

public class VoznjaDto
{
    public int Id { get; set; }
    public KorisnikDto? Vozac { get; set; }
    public GradDto? PolazniGrad { get; set; }
    public GradDto? OdredisniGrad { get; set; }
    public DateTime Polazak { get; set; }
    public DateTime OcekivaniDolazak { get; set; }
    public decimal CijenaPoMjestu { get; set; }
    public int UkupnoMjesta { get; set; }
    public int SlobodnaMjesta { get; set; }
    public string Opis { get; set; } = string.Empty;
    public StatusVoznje Status { get; set; }
}

public class VoznjaRequest
{
    [Required]
    public int VozacId { get; set; }
    [Required]
    public int PolazniGradId { get; set; }
    [Required]
    public int OdredisniGradId { get; set; }
    [Required]
    public DateTime Polazak { get; set; }
    [Required]
    public DateTime OcekivaniDolazak { get; set; }
    [Range(0.01, 10000)]
    public decimal CijenaPoMjestu { get; set; }
    [Range(1, 20)]
    public int UkupnoMjesta { get; set; }
    [Range(0, 20)]
    public int SlobodnaMjesta { get; set; }
    [StringLength(1000)]
    public string Opis { get; set; } = string.Empty;
    public StatusVoznje Status { get; set; } = StatusVoznje.Planirana;
}

public class RezervacijaDto
{
    public int Id { get; set; }
    public int VoznjaId { get; set; }
    public KorisnikDto? Putnik { get; set; }
    public int BrojMjesta { get; set; }
    public decimal CijenaUkupno { get; set; }
    public DateTime VrijemeRezervacije { get; set; }
    public StatusRezervacije Status { get; set; }
    public string Napomena { get; set; } = string.Empty;
}

public class RezervacijaRequest
{
    [Required]
    public int VoznjaId { get; set; }
    [Required]
    public int PutnikId { get; set; }
    [Range(1, 10)]
    public int BrojMjesta { get; set; } = 1;
    public StatusRezervacije Status { get; set; } = StatusRezervacije.Aktivna;
    [StringLength(500)]
    public string Napomena { get; set; } = string.Empty;
}

public class PlacanjeDto
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public decimal Iznos { get; set; }
    public DateTime VrijemePlacanja { get; set; }
    public NacinPlacanja NacinPlacanja { get; set; }
    public bool Uspjesno { get; set; }
}

public class PlacanjeRequest
{
    [Required]
    public int RezervacijaId { get; set; }
    [Range(0.01, 100000)]
    public decimal Iznos { get; set; }
    public DateTime VrijemePlacanja { get; set; } = DateTime.UtcNow;
    public NacinPlacanja NacinPlacanja { get; set; }
    public bool Uspjesno { get; set; }
}

public class OcjenaDto
{
    public int Id { get; set; }
    public int RezervacijaId { get; set; }
    public KorisnikDto? Autor { get; set; }
    public int BrojZvjezdica { get; set; }
    public string Komentar { get; set; } = string.Empty;
    public DateTime Kreirano { get; set; }
}

public class OcjenaRequest
{
    [Required]
    public int RezervacijaId { get; set; }
    [Required]
    public int AutorId { get; set; }
    [Range(1, 5)]
    public int BrojZvjezdica { get; set; } = 5;
    [Required, StringLength(500)]
    public string Komentar { get; set; } = string.Empty;
}

public class SaldoTransakcijaDto
{
    public int Id { get; set; }
    public KorisnikDto? Korisnik { get; set; }
    public decimal Iznos { get; set; }
    public string Tip { get; set; } = string.Empty;
    public decimal SaldoPrije { get; set; }
    public decimal SaldoPoslije { get; set; }
    public DateTime Vrijeme { get; set; }
}

public class SaldoTransakcijaRequest
{
    [Required]
    public int KorisnikId { get; set; }
    [Range(0.01, 1000000)]
    public decimal Iznos { get; set; }
    [Required, StringLength(80)]
    public string Tip { get; set; } = string.Empty;
}

public class VoznjaAttachmentDto
{
    public int Id { get; set; }
    public int VoznjaId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}
