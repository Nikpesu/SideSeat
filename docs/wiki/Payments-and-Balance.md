# Saldo i plaćanja

[← Wiki](Home.md)

SideSeat koristi interni **saldo** (u EUR) za rezervacije, zaradu vozača, napojnice i isplate.
Sve uplate su **mock** (demo) — stvarna naplata se ne izvršava, ali se sve bilježi u povijesti transakcija.

## Nadoplata (uplata na saldo)
**Račun → Uplati sredstva** (`/Korisnik/Uplata`):
1. unesi iznos,
2. odaberi način: **Kartica**, **PayPal** ili **Revolut Pay**,
   - kartica: ime, broj `4444 4444 4444 4444`, vrijedi do (MM/GG), CVV; karticu možeš spremiti,
   - PayPal/Revolut: ime računa + potvrda mock transakcije,
3. ispuni adresu naplate,
4. potvrdi — saldo se poveća.

## Isplata (sa salda)
**Račun → Saldo** (`/Korisnik/Saldo`), akcija **Isplata**:
- raspoloživo = `trenutni saldo − rezervirana sredstva`,
- rezervirana sredstva su iznosi potvrđenih/u procesu rezervacija plaćenih saldom; ne mogu se isplatiti
  dok se vožnja ne završi ili rezervacija ne otkaže.

## Napojnica
Napojnica se daje **isključivo karticom** i **isključivo u obrascu za ocjenu vozača**, nakon završene
vožnje i samo pri **prvoj** ocjeni vozača. Ide na saldo vozača i **ne** dira saldo putnika.
Ne može se unijeti pri kreiranju ili uređivanju rezervacije.

## Settlement na kraju vožnje
Kad vozač završi vožnju:

| Rezervacija | Učinak |
|---|---|
| plaćena **saldom** | iznos sjeda na saldo vozača |
| plaćena **gotovinom** | naplata gotovine se evidentira; saldo putnika se ne mijenja |
| **napojnica karticom** | dodatno sjeda na saldo vozača |

## Tijek novca — sažetak

| Događaj | Saldo |
|---|---|
| Uplata (Kartica/PayPal/Revolut) | **+** putnik/vozač |
| Rezervacija plaćena saldom | sredstva rezervirana (zaključana) |
| Završena vožnja (zarada) | **+** vozač |
| Napojnica karticom | **+** vozač |
| Isplata | **−** (do raspoloživog) |

Vidi i [Hodogram vožnje](../vožnja.md).
