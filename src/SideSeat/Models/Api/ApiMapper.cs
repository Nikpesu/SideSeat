namespace SideSeat.Models.Api;

public static class ApiMapper
{
    public static GradDto ToDto(this Grad grad) => new()
    {
        Id = grad.Id,
        Naziv = grad.Naziv,
        Drzava = grad.Drzava,
        PostanskiBroj = grad.PostanskiBroj
    };

    public static KorisnikDto ToDto(this Korisnik korisnik) => new()
    {
        Id = korisnik.Id,
        Ime = korisnik.Ime,
        Prezime = korisnik.Prezime,
        Email = korisnik.Email,
        Adresa = korisnik.Adresa,
        BrojMobitela = korisnik.BrojMobitela,
        DatumRegistracije = korisnik.DatumRegistracije,
        Tip = korisnik.Tip,
        JeAktivan = korisnik.JeAktivan,
        KycPodnesen = korisnik.KycPodnesen,
        Saldo = korisnik.Saldo
    };

    public static VoziloDto ToDto(this Vozilo vozilo) => new()
    {
        Id = vozilo.Id,
        Marka = vozilo.Marka,
        Model = vozilo.Model,
        Registracija = vozilo.Registracija,
        GodinaProizvodnje = vozilo.GodinaProizvodnje,
        BrojSjedala = vozilo.BrojSjedala,
        Boja = vozilo.Boja,
        ProsjecnaPotrosnja = vozilo.ProsjecnaPotrosnja,
        Vlasnik = vozilo.Vlasnik?.ToDto()
    };

    public static VoznjaDto ToDto(this Voznja voznja) => new()
    {
        Id = voznja.Id,
        Vozac = voznja.Vozac?.ToDto(),
        PolazniGrad = voznja.PolazniGrad?.ToDto(),
        OdredisniGrad = voznja.OdredisniGrad?.ToDto(),
        Polazak = voznja.Polazak,
        OcekivaniDolazak = voznja.OcekivaniDolazak,
        CijenaPoMjestu = voznja.CijenaPoMjestu,
        UkupnoMjesta = voznja.UkupnoMjesta,
        SlobodnaMjesta = voznja.SlobodnaMjesta,
        Opis = voznja.Opis,
        Status = voznja.Status
    };

    public static RezervacijaDto ToDto(this Rezervacija rezervacija) => new()
    {
        Id = rezervacija.Id,
        VoznjaId = rezervacija.VoznjaId,
        Putnik = rezervacija.Putnik?.ToDto(),
        BrojMjesta = rezervacija.BrojMjesta,
        CijenaUkupno = rezervacija.CijenaUkupno,
        VrijemeRezervacije = rezervacija.VrijemeRezervacije,
        Status = rezervacija.Status,
        Napomena = rezervacija.Napomena
    };

    public static PlacanjeDto ToDto(this Placanje placanje) => new()
    {
        Id = placanje.Id,
        RezervacijaId = placanje.RezervacijaId,
        Iznos = placanje.Iznos,
        VrijemePlacanja = placanje.VrijemePlacanja,
        NacinPlacanja = placanje.NacinPlacanja,
        Uspjesno = placanje.Uspjesno
    };

    public static OcjenaDto ToDto(this OcjenaVoznje ocjena) => new()
    {
        Id = ocjena.Id,
        RezervacijaId = ocjena.RezervacijaId,
        Autor = ocjena.Autor?.ToDto(),
        BrojZvjezdica = ocjena.BrojZvjezdica,
        Komentar = ocjena.Komentar,
        Kreirano = ocjena.Kreirano,
        Slike = ocjena.Slike.Select(s => s.ToDto()).ToList()
    };

    public static SaldoTransakcijaDto ToDto(this SaldoTransakcija transakcija) => new()
    {
        Id = transakcija.Id,
        Korisnik = transakcija.Korisnik?.ToDto(),
        Iznos = transakcija.Iznos,
        Tip = transakcija.Tip,
        SaldoPrije = transakcija.SaldoPrije,
        SaldoPoslije = transakcija.SaldoPoslije,
        Vrijeme = transakcija.Vrijeme
    };

    public static OcjenaSlikaDto ToDto(this OcjenaSlika slika) => new()
    {
        Id = slika.Id,
        OcjenaVoznjeId = slika.OcjenaVoznjeId,
        FileName = slika.FileName,
        FilePath = slika.FilePath,
        ContentType = slika.ContentType,
        FileSize = slika.FileSize,
        CreatedAt = slika.CreatedAt
    };
}
