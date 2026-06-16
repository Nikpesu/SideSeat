# Vožnja — hodogram za vozače i putnike

Ovaj dokument opisuje cijeli tijek vožnje na SideSeat platformi: kako putnik rezervira i plati,
kako vozač odradi vožnju i naplati, te kako se novac **nadoplaćuje** i **isplaćuje** s aplikacije.

> Napomena o napojnici: napojnica se daje **isključivo karticom i isključivo u obrascu za ocjenu vozača**,
> nakon završene vožnje. Ne skida se sa SideSeat salda putnika i unosi se samo jednom (pri prvoj ocjeni vozača).

---

## 1. Hodogram za putnika

1. **Pronađi vožnju** — na početnoj stranici pretraži po polazištu, odredištu i datumu.
2. **Rezerviraj** — otvori vožnju i ispuni rezervaciju (broj mjesta, način plaćanja).
3. **Plati** — odaberi način plaćanja:
   - **SideSeat saldo** — iznos se rezervira s tvog salda (mora biti dovoljno sredstava).
   - **Gotovina / kartica kod vozača** — plaćaš izravno vozaču pri vožnji.
4. **Čekaj potvrdu** — vozač/sustav potvrđuje rezervaciju; status prelazi u *Potvrđena*.
5. **Vožnja** — prijaviš se (check-in) i odvezeš se do odredišta.
6. **Završetak** — kad vozač označi vožnju kao *Završena*, rezervacija postaje *Završena*.
7. **Ocijeni i (opcionalno) ostavi napojnicu** — u obrascu za ocjenu vozača daš ocjenu (1–5),
   komentar i, ako želiš, **napojnicu karticom**. Napojnica ide izravno na saldo vozača.

## 2. Hodogram za vozača

1. **Aktiviraj vozačku ulogu** — ispuni KYC obrazac (*Račun → KYC vozača*).
2. **Dodaj vozilo** — vozilo mora biti povezano s tvojim računom.
3. **Objavi vožnju** — *Akcije → Nova vožnja*: polazište, odredište, vrijeme, broj mjesta, cijena po mjestu.
4. **Upravljaj rezervacijama** — prati i potvrđuj dolazne rezervacije putnika.
5. **Trenutna vožnja** — na stranici *Trenutna vožnja* pratiš putnike, check-in i status.
6. **Završi vožnju** — označi vožnju kao *Završena*.
7. **Naplata** — zarada od vožnje (i napojnice karticom) sjeda na tvoj **SideSeat saldo**.
8. **Isplata** — sredstva sa salda isplaćuješ na stranici *Saldo*.

---

## 3. Kako nadoplatiti novce (uplata na saldo)

1. Idi na **Račun → Uplati sredstva** (*Korisnik/Uplata*).
2. Unesi **iznos** uplate.
3. Odaberi **način plaćanja**: Kartica, PayPal ili Revolut Pay.
   - Za karticu: ime na kartici, broj (`4444 4444 4444 4444`), vrijedi do (MM/GG), CVV.
     Karticu po želji možeš spremiti za sljedeći put.
   - Za PayPal/Revolut: ime računa i potvrda mock transakcije.
4. Ispuni **adresu naplate** (ulica, kućni broj, poštanski broj, država).
5. Potvrdi — saldo se **poveća** za uneseni iznos.

> Uplata je mock (demo) — stvarna naplata se ne izvršava. Svaka uplata se bilježi u povijesti transakcija.

## 4. Kako isplatiti novce (isplata sa salda)

1. Idi na **Račun → Saldo** (*Korisnik/Saldo*).
2. Unesi **iznos isplate** i pokreni akciju *Isplata*.
3. Sustav provjerava **raspoloživo za isplatu**:
   `raspoloživo = trenutni saldo − sredstva rezervirana za zakazane rezervacije`.
4. Ako je iznos u granicama raspoloživog, saldo se **smanji** i isplata se zabilježi.

> Sredstva koja su rezervirana za potvrđene/u procesu rezervacije (plaćene saldom) ne mogu se isplatiti
> dok se vožnja ne završi ili rezervacija ne otkaže.

---

## 5. Tijek novca — sažetak

| Događaj | Učinak na saldo |
|---|---|
| Uplata (Kartica / PayPal / Revolut) | **+** povećava saldo putnika/vozača |
| Rezervacija plaćena saldom | sredstva **rezervirana** (zaključana do završetka) |
| Završena vožnja (zarada vozača) | **+** sjeda na saldo vozača |
| Napojnica karticom pri ocjeni | **+** sjeda na saldo vozača (ne dira saldo putnika) |
| Isplata sa salda | **−** smanjuje saldo (do iznosa raspoloživog za isplatu) |

Sve promjene salda vidljive su u povijesti transakcija na stranici **Saldo**.
