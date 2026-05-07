# SideSeat semantic model

## Entiteti (tablice)

### Grad
- Svojstva: Id (PK), Naziv, Drzava, PostanskiBroj
- Veze: 1-N s Voznja (PolazniGrad, OdredisniGrad)

### Korisnik
- Svojstva: Id (PK), Ime, Prezime, Email, BrojMobitela, DatumRegistracije, Tip, JeAktivan, VoziloId (FK)
- Veze: 0-1 s Vozilo (Vozilo), 1-N s Voznja (KreiraneVoznje), 1-N s Rezervacija (Rezervacije), 1-N s OcjenaVoznje (Autor)

### Vozilo
- Svojstva: Id (PK), Marka, Model, Registracija, GodinaProizvodnje, BrojSjedala, Boja, ProsjecnaPotrosnja, VlasnikId (FK)
- Veze: 0-1 s Korisnik (Vlasnik)

### Voznja
- Svojstva: Id (PK), VozacId (FK), PolazniGradId (FK), OdredisniGradId (FK), Polazak, OcekivaniDolazak, CijenaPoMjestu, UkupnoMjesta, SlobodnaMjesta, Opis, Status
- Veze: N-1 s Korisnik (Vozac), N-1 s Grad (PolazniGrad, OdredisniGrad), 1-N s Rezervacija

### Rezervacija
- Svojstva: Id (PK), VoznjaId (FK), PutnikId (FK), BrojMjesta, CijenaUkupno, VrijemeRezervacije, Status, Napomena
- Veze: N-1 s Voznja, N-1 s Korisnik (Putnik), 1-1 ili 1-0..1 s Placanje

### Placanje
- Svojstva: Id (PK), RezervacijaId (FK), Iznos, VrijemePlacanja, NacinPlacanja, Uspjesno
- Veze: 1-1 s Rezervacija

### OcjenaVoznje
- Svojstva: Id (PK), RezervacijaId (FK), AutorId (FK), BrojZvjezdica, Komentar, Kreirano
- Veze: N-1 s Rezervacija, N-1 s Korisnik (Autor)

## Relacijski sazetak
- Grad 1-N Voznja (polazni i odredisni grad)
- Korisnik 1-N Voznja (vozac)
- Korisnik 1-N Rezervacija (putnik)
- Voznja 1-N Rezervacija
- Rezervacija 1-1 Placanje
- Rezervacija 1-N OcjenaVoznje (ili 1-0..1 ovisno o poslovnom pravilu)
- Korisnik 0-1 Vozilo (vlasnik)
