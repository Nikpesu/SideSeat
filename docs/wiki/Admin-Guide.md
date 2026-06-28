# Vodič za administratora

[← Wiki](Home.md)

Administrator ima potpuni pregled i CRUD nad svim entitetima te audit zapis.

## Administracija (lijevi meni → Administracija)

| Sekcija | Što radiš |
|---|---|
| **Korisnici** | pregled, kreiranje, uređivanje, brisanje korisnika; dodjela uloge i vozila |
| **Gradovi** | CRUD gradova (naziv, država, poštanski broj, koordinate) |
| **Vozila** | CRUD vozila (marka, model, registracija, sjedala, potrošnja) |
| **Rezervacije** | sve rezervacije + admin kreiranje/uređivanje/brisanje |
| **Plaćanja** | evidencija i CRUD plaćanja |
| **Ocjene** | sve ocjene, prilozi (slike) i admin feedback |
| **Audit zapis** | tko je što i kada radio (revizijski trag) |

## Pravila i ograničenja
- Brisanje **grada** odbija se ako postoje povezane vožnje.
- Brisanje **korisnika** odbija se ako ima vožnje ili rezervacije.
- Brisanje **rezervacije** uklanja povezane ocjene i plaćanja.
- Brisanje **vozila** odspaja povezane korisnike.

## Saldo korisnika
Admin može evidentirati **saldo transakcije** (uplata/isplata/korekcija) za bilo kojeg korisnika
(ručno ili preko AI alata uz potvrdu). Korisnik može samo za sebe.

## AI asistent
Admin u Copilotu vidi **sve alate**, uključujući lookup (`get_users`, `get_vehicles`, `get_payments`)
i `prepare_*` za sve entitete. Svaka promjena je pripremljena akcija koju admin potvrđuje.
Vidi [AI asistent](AI-Assistant.md).

## MCP poslužitelj
Vanjski AI klijenti mogu se spojiti preko MCP-a (Model Context Protocol) uz `MCP_API_KEY`.
Uloge i korisnik za MCP postavljaju se kroz konfiguraciju. Vidi [REST API i MCP](REST-API.md).
