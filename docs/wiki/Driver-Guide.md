# Vodič za vozača

[← Wiki](Home.md)

Vozač objavljuje i vodi vožnje te naplaćuje zaradu na SideSeat saldo.

## 1. Aktiviraj vozačku ulogu (KYC)
Idi na **Račun → KYC vozača** i ispuni podatke (OIB, osobna, vozačka, datum rođenja).
Nakon predaje aktivira se vozačka uloga.

## 2. Poveži vozilo
Vozilo mora biti povezano s tvojim računom (admin ga dodaje/povezuje, vidi [Admin vodič](Admin-Guide.md)).

## 3. Objavi vožnju
**Akcije → Nova vožnja**: polazište, odredište, vrijeme polaska i dolaska, broj mjesta, cijena po mjestu, opis.
Ruta se prikazuje na karti (OSRM geometrija).

## 4. Upravljaj rezervacijama
Na detaljima vožnje vidiš dolazne rezervacije putnika i možeš ih **potvrditi** ili **odbiti**.
Putnik dobiva obavijest o promjeni statusa.

## 5. Vodi vožnju
- **Trenutna vožnja** — pratiš potvrđene putnike, njihov check-in i lokacije uživo (SignalR).
- **Pokreni** vožnju kad su putnici spremni; po dolasku označi **Završena**.

## 6. Naplata i isplata
Po završetku vožnje:
- za **gotovinske** rezervacije potvrđuješ primitak gotovine,
- za **saldo** rezervacije iznos sjeda na tvoj **SideSeat saldo**,
- **napojnice karticom** (koje putnik ostavi pri ocjeni) također sjedaju na tvoj saldo.

Sredstva sa salda isplaćuješ na stranici **Saldo** (akcija Isplata). Vidi [Saldo i plaćanja](Payments-and-Balance.md).

## AI asistent
Vozač u Copilotu dobiva i alate za vožnje: priprema kreiranja/uređivanja vožnje, start i završetak.
Svaka akcija traži tvoju potvrdu. Vidi [AI asistent](AI-Assistant.md).
