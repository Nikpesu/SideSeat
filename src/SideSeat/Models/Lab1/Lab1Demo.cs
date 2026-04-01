namespace SideSeat.Models;

// Demonstracijska klasa za Lab 1:
// 1) puni model testnim podacima
// 2) izvodi nekoliko LINQ upita
// 3) pokazuje osnovni async/await primjer
public static class Lab1Demo
{
    public static async Task RunAsync()
    {
        // Kreiramo cijeli graf objekata (gradovi, korisnici, voznje, rezervacije...).
        var podaci = KreirajPodatke();

        // Pokrecemo sinkrone LINQ primjere nad kreiranim podacima.
        IspisiLinqPrimjere(podaci);

        // Pokrecemo async/await primjer sa paralelnim taskovima.
        await IspisiAsyncAwaitPrimjerAsync(podaci);
    }

    private static Lab1Podaci KreirajPodatke()
    {
        // Korijenski objekt koji drzi sve kolekcije za demo.
        var podaci = new Lab1Podaci();

        // Osnovni lookup podaci: gradovi.
        var zagreb = new Grad { Id = 1, Naziv = "Zagreb", Drzava = "Hrvatska", PostanskiBroj = "10000" };
        var split = new Grad { Id = 2, Naziv = "Split", Drzava = "Hrvatska", PostanskiBroj = "21000" };
        var rijeka = new Grad { Id = 3, Naziv = "Rijeka", Drzava = "Hrvatska", PostanskiBroj = "51000" };
        var osijek = new Grad { Id = 4, Naziv = "Osijek", Drzava = "Hrvatska", PostanskiBroj = "31000" };

        podaci.Gradovi.AddRange([zagreb, split, rijeka, osijek]);

        var vozilo1 = new Vozilo
        {
            Id = 1,
            Marka = "Skoda",
            Model = "Octavia",
            Registracija = "ZG-1234-SS",
            GodinaProizvodnje = 2020,
            BrojSjedala = 5,
            Boja = "Siva",
            ProsjecnaPotrosnja = 5.4m
        };

        var vozilo2 = new Vozilo
        {
            Id = 2,
            Marka = "Volkswagen",
            Model = "Passat",
            Registracija = "ST-9876-CP",
            GodinaProizvodnje = 2019,
            BrojSjedala = 5,
            Boja = "Crna",
            ProsjecnaPotrosnja = 5.9m
        };

        // Korisnici: 2 vozaca i 3 putnika.
        var vozac1 = new Korisnik
        {
            Id = 1,
            Ime = "Ivan",
            Prezime = "Horvat",
            Email = "ivan@sideseat.hr",
            BrojMobitela = "+385911111111",
            DatumRegistracije = DateTime.Now.AddMonths(-6),
            Tip = TipKorisnika.Vozac,
            JeAktivan = true,
            VoziloId = vozilo1.Id,
            Vozilo = vozilo1
        };

        var vozac2 = new Korisnik
        {
            Id = 2,
            Ime = "Ana",
            Prezime = "Kovac",
            Email = "ana@sideseat.hr",
            BrojMobitela = "+385922222222",
            DatumRegistracije = DateTime.Now.AddMonths(-4),
            Tip = TipKorisnika.Vozac,
            JeAktivan = true,
            VoziloId = vozilo2.Id,
            Vozilo = vozilo2
        };

        var putnik1 = new Korisnik
        {
            Id = 3,
            Ime = "Marko",
            Prezime = "Maric",
            Email = "marko@sideseat.hr",
            BrojMobitela = "+385933333333",
            DatumRegistracije = DateTime.Now.AddMonths(-2),
            Tip = TipKorisnika.Putnik,
            JeAktivan = true
        };

        var putnik2 = new Korisnik
        {
            Id = 4,
            Ime = "Petra",
            Prezime = "Juric",
            Email = "petra@sideseat.hr",
            BrojMobitela = "+385944444444",
            DatumRegistracije = DateTime.Now.AddMonths(-1),
            Tip = TipKorisnika.Putnik,
            JeAktivan = true
        };

        var putnik3 = new Korisnik
        {
            Id = 5,
            Ime = "Luka",
            Prezime = "Babic",
            Email = "luka@sideseat.hr",
            BrojMobitela = "+385955555555",
            DatumRegistracije = DateTime.Now.AddDays(-20),
            Tip = TipKorisnika.Putnik,
            JeAktivan = true
        };

        podaci.Korisnici.AddRange([vozac1, vozac2, putnik1, putnik2, putnik3]);
        podaci.Vozila.AddRange([vozilo1, vozilo2]);

        // Veza 1-1: vozilo i vlasnik (vozac).
        vozilo1.VlasnikId = vozac1.Id;
        vozilo1.Vlasnik = vozac1;
        vozilo2.VlasnikId = vozac2.Id;
        vozilo2.Vlasnik = vozac2;

        // Tri glavne voznje (glavni objekti koje trazak zadatak spominje).
        var voznja1 = new Voznja
        {
            Id = 1,
            VozacId = vozac1.Id,
            Vozac = vozac1,
            PolazniGradId = zagreb.Id,
            PolazniGrad = zagreb,
            OdredisniGradId = split.Id,
            OdredisniGrad = split,
            Polazak = DateTime.Now.AddDays(1).Date.AddHours(8),
            OcekivaniDolazak = DateTime.Now.AddDays(1).Date.AddHours(13),
            CijenaPoMjestu = 25m,
            UkupnoMjesta = 3,
            SlobodnaMjesta = 3,
            Opis = "Direktna voznja uz jednu kratku pauzu.",
            Status = StatusVoznje.Planirana
        };

        var voznja2 = new Voznja
        {
            Id = 2,
            VozacId = vozac2.Id,
            Vozac = vozac2,
            PolazniGradId = split.Id,
            PolazniGrad = split,
            OdredisniGradId = zagreb.Id,
            OdredisniGrad = zagreb,
            Polazak = DateTime.Now.AddDays(2).Date.AddHours(15),
            OcekivaniDolazak = DateTime.Now.AddDays(2).Date.AddHours(20),
            CijenaPoMjestu = 24m,
            UkupnoMjesta = 3,
            SlobodnaMjesta = 3,
            Opis = "Povratna voznja Split - Zagreb.",
            Status = StatusVoznje.Planirana
        };

        var voznja3 = new Voznja
        {
            Id = 3,
            VozacId = vozac1.Id,
            Vozac = vozac1,
            PolazniGradId = zagreb.Id,
            PolazniGrad = zagreb,
            OdredisniGradId = osijek.Id,
            OdredisniGrad = osijek,
            Polazak = DateTime.Now.AddDays(3).Date.AddHours(7),
            OcekivaniDolazak = DateTime.Now.AddDays(3).Date.AddHours(10),
            CijenaPoMjestu = 15m,
            UkupnoMjesta = 3,
            SlobodnaMjesta = 3,
            Opis = "Jutarnja voznja Zagreb - Osijek.",
            Status = StatusVoznje.Planirana
        };

        podaci.Voznje.AddRange([voznja1, voznja2, voznja3]);
        vozac1.KreiraneVoznje.AddRange([voznja1, voznja3]);
        vozac2.KreiraneVoznje.Add(voznja2);

        // Rezervacije povezuju putnika i voznju (N-N preko entiteta Rezervacija).
        var rezervacija1 = new Rezervacija
        {
            Id = 1,
            VoznjaId = voznja1.Id,
            Voznja = voznja1,
            PutnikId = putnik1.Id,
            Putnik = putnik1,
            BrojMjesta = 1,
            CijenaUkupno = voznja1.CijenaPoMjestu,
            VrijemeRezervacije = DateTime.Now.AddHours(-4),
            Status = StatusRezervacije.Potvrdena,
            Napomena = "Imam mali ruksak."
        };

        var rezervacija2 = new Rezervacija
        {
            Id = 2,
            VoznjaId = voznja1.Id,
            Voznja = voznja1,
            PutnikId = putnik2.Id,
            Putnik = putnik2,
            BrojMjesta = 1,
            CijenaUkupno = voznja1.CijenaPoMjestu,
            VrijemeRezervacije = DateTime.Now.AddHours(-3),
            Status = StatusRezervacije.Aktivna,
            Napomena = "Polazak mi odgovara tocno u 8:00."
        };

        var rezervacija3 = new Rezervacija
        {
            Id = 3,
            VoznjaId = voznja2.Id,
            Voznja = voznja2,
            PutnikId = putnik3.Id,
            Putnik = putnik3,
            BrojMjesta = 2,
            CijenaUkupno = voznja2.CijenaPoMjestu * 2,
            VrijemeRezervacije = DateTime.Now.AddHours(-2),
            Status = StatusRezervacije.Potvrdena,
            Napomena = "Putujem s kolegom."
        };

        var rezervacija4 = new Rezervacija
        {
            Id = 4,
            VoznjaId = voznja3.Id,
            Voznja = voznja3,
            PutnikId = putnik1.Id,
            Putnik = putnik1,
            BrojMjesta = 1,
            CijenaUkupno = voznja3.CijenaPoMjestu,
            VrijemeRezervacije = DateTime.Now.AddHours(-1),
            Status = StatusRezervacije.Aktivna,
            Napomena = "Treba mi preuzimanje kod Arene."
        };

        podaci.Rezervacije.AddRange([rezervacija1, rezervacija2, rezervacija3, rezervacija4]);

        // Sinkronizacija relacija i slobodnih mjesta po voznji.
        PoveziRezervaciju(voznja1, putnik1, rezervacija1);
        PoveziRezervaciju(voznja1, putnik2, rezervacija2);
        PoveziRezervaciju(voznja2, putnik3, rezervacija3);
        PoveziRezervaciju(voznja3, putnik1, rezervacija4);

        // Evidencija placanja za potvrdene rezervacije.
        podaci.Placanja.AddRange(
        [
            new Placanje
            {
                Id = 1,
                RezervacijaId = rezervacija1.Id,
                Rezervacija = rezervacija1,
                Iznos = rezervacija1.CijenaUkupno,
                VrijemePlacanja = DateTime.Now.AddHours(-3),
                NacinPlacanja = NacinPlacanja.Kartica,
                Uspjesno = true
            },
            new Placanje
            {
                Id = 2,
                RezervacijaId = rezervacija3.Id,
                Rezervacija = rezervacija3,
                Iznos = rezervacija3.CijenaUkupno,
                VrijemePlacanja = DateTime.Now.AddHours(-1),
                NacinPlacanja = NacinPlacanja.Online,
                Uspjesno = true
            }
        ]);

        // Primjer jedne ocjene zavrsene/odradene voznje.
        podaci.Ocjene.Add(
            new OcjenaVoznje
            {
                Id = 1,
                RezervacijaId = rezervacija1.Id,
                Rezervacija = rezervacija1,
                AutorId = putnik1.Id,
                Autor = putnik1,
                BrojZvjezdica = 5,
                Komentar = "Ugodna voznja i tocan polazak.",
                Kreirano = DateTime.Now
            });

        return podaci;
    }

    // Pomocna metoda koja odrzava obje strane relacije i broj slobodnih mjesta.
    private static void PoveziRezervaciju(Voznja voznja, Korisnik putnik, Rezervacija rezervacija)
    {
        voznja.Rezervacije.Add(rezervacija);
        putnik.Rezervacije.Add(rezervacija);
        voznja.SlobodnaMjesta -= rezervacija.BrojMjesta;
    }

    private static void IspisiLinqPrimjere(Lab1Podaci podaci)
    {
        Console.WriteLine("===== LAB 1: SideSeat LINQ =====");

        // 1) Filtriranje: planirane voznje koje jos imaju slobodnih mjesta.
        var dostupneVoznje = podaci.Voznje
            .Where(v => v.Status == StatusVoznje.Planirana && v.SlobodnaMjesta > 0)
            .OrderBy(v => v.Polazak)
            .ToList();

        Console.WriteLine("Dostupne planirane voznje:");
        foreach (var v in dostupneVoznje)
        {
            Console.WriteLine($"- {v.PolazniGrad.Naziv} -> {v.OdredisniGrad.Naziv}, polazak {v.Polazak:dd.MM.yyyy HH:mm}, slobodno mjesta: {v.SlobodnaMjesta}");
        }

        // 2) Projekcija + podupit: broj aktivnih rezervacija po vozacu.
        var vozaciPoRezervacijama = podaci.Korisnici
            .Where(k => k.Tip == TipKorisnika.Vozac)
            .Select(k => new
            {
                Vozac = k,
                BrojAktivnihRezervacija = k.KreiraneVoznje
                    .SelectMany(v => v.Rezervacije)
                    .Count(r => r.Status != StatusRezervacije.Otkazana)
            })
            .OrderByDescending(x => x.BrojAktivnihRezervacija)
            .ToList();

        Console.WriteLine("Vozaci poredani po broju rezervacija:");
        foreach (var x in vozaciPoRezervacijama)
        {
            Console.WriteLine($"- {x.Vozac.Ime} {x.Vozac.Prezime}: {x.BrojAktivnihRezervacija} rezervacija");
        }

        // 3) Any + vremenski filter: putnici koji putuju unutar 7 dana.
        var granica = DateTime.Now.AddDays(7);
        var putniciOVomTjednu = podaci.Korisnici
            .Where(k => k.Tip == TipKorisnika.Putnik)
            .Where(k => k.Rezervacije.Any(r => r.Voznja.Polazak <= granica && r.Status != StatusRezervacije.Otkazana))
            .ToList();

        Console.WriteLine("Putnici koji imaju voznju unutar 7 dana:");
        foreach (var p in putniciOVomTjednu)
        {
            Console.WriteLine($"- {p.Ime} {p.Prezime}");
        }

        // 4) Agregacija: prihod po voznji (sumiranje cijena rezervacija).
        var prihodPoVoznji = podaci.Voznje
            .Select(v => new
            {
                VoznjaId = v.Id,
                Prihod = v.Rezervacije
                    .Where(r => r.Status != StatusRezervacije.Otkazana)
                    .Sum(r => r.CijenaUkupno)
            })
            .OrderByDescending(x => x.Prihod)
            .ToList();

        Console.WriteLine("Prihod po voznji:");
        foreach (var p in prihodPoVoznji)
        {
            Console.WriteLine($"- Voznja #{p.VoznjaId}: {p.Prihod:0.00} EUR");
        }
    }

    private static async Task IspisiAsyncAwaitPrimjerAsync(Lab1Podaci podaci)
    {
        Console.WriteLine();
        Console.WriteLine("===== LAB 1: Async/Await =====");

        // Pokrecemo dva neovisna async poziva bez cekanja odmah nakon pokretanja.
        var aktivneTask = DohvatiRezervacijePoStatusuAsync(podaci.Rezervacije, StatusRezervacije.Aktivna);
        var potvrdeneTask = DohvatiRezervacijePoStatusuAsync(podaci.Rezervacije, StatusRezervacije.Potvrdena);

        // Cekamo da oba zavrse (paralelno izvrsavanje taskova).
        await Task.WhenAll(aktivneTask, potvrdeneTask);

        // Nakon sto su gotovi, dohvatimo rezultate.
        var aktivne = await aktivneTask;
        var potvrdene = await potvrdeneTask;

        Console.WriteLine($"Aktivnih rezervacija: {aktivne.Count}");
        Console.WriteLine($"Potvrdenih rezervacija: {potvrdene.Count}");
    }

    private static async Task<List<Rezervacija>> DohvatiRezervacijePoStatusuAsync(
        IEnumerable<Rezervacija> rezervacije,
        StatusRezervacije status)
    {
        // Simulacija IO latencije (npr. baza ili vanjski API).
        await Task.Delay(150);

        // Filtriranje i materijalizacija rezultata u listu.
        return rezervacije.Where(r => r.Status == status).ToList();
    }
}