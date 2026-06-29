# SideSeat Wiki

Dobrodošli u **SideSeat** pomoć — mreža za dijeljenje vožnji (ride-sharing) s AI asistentom,
kartama ruta u stvarnom vremenu, saldom i mock plaćanjima.

Ovaj wiki je čitljiv izravno na GitHubu. Krenite od vodiča za svoju ulogu.

## Sadržaj

### Početak
- [Početak rada (Getting Started)](Getting-Started.md) — instalacija, pokretanje, demo korisnici
- [Konfiguracija](Configuration.md) — varijable okruženja i postavke
- [Postavljanje (Deployment)](Deployment.md) — Docker i Docker Hub

### Vodiči po ulozi
- [Vodič za putnika](User-Guide.md) — pretraga, rezervacija, plaćanje, ocjene
- [Vodič za vozača](Driver-Guide.md) — KYC, objava vožnje, vođenje vožnje, zarada
- [Vodič za administratora](Admin-Guide.md) — korisnici, gradovi, vozila, plaćanja, audit

### Značajke
- [Saldo i plaćanja](Payments-and-Balance.md) — nadoplata, isplata, napojnica, settlement
- [AI asistent (Copilot)](AI-Assistant.md) — alati, ovlasti po ulozi, web pretraga
- [REST API i MCP](REST-API.md) — JSON API i Model Context Protocol
- [Hodogram vožnje](../vožnja.md) — korak-po-korak za vozače i putnike

### Za razvojne programere
- [Dubinski vodič kroz cijeli projekt](Project-Deep-Dive.md) — opširno objašnjenje svakog dijela uz dijagrame i tablice (za učenje i onboarding)
- [Arhitektura, folderi i klase](../ARCHITECTURE.md)
- [Changelog (povijest verzija)](../../changelogs/README.md)
- [Često postavljana pitanja (FAQ)](FAQ.md)

### Laboratorijske vježbe
- [Pregled svih labosa](Labs.md) — ciljevi, kriteriji i kako ih SideSeat ispunjava
- [Lab 1 — C# / LINQ](Lab-1-Csharp-LINQ.md)
- [Lab 2 — HTML / Binding](Lab-2-HTML-Binding.md)
- [Lab 3 — EF / Routing](Lab-3-EF-Routing.md)
- [Lab 4 — CRUD / JS](Lab-4-CRUD-JS.md)
- [Lab 5 — API / Auth / Testovi](Lab-5-API-Auth-Tests.md)

## Što je SideSeat?

SideSeat povezuje **vozače** koji objavljuju vožnje i **putnike** koji rezerviraju mjesta.
Plaćanje ide preko internog **SideSeat salda** ili gotovine kod vozača. Aplikacija nudi:

- pretragu i rezervaciju vožnji s kartom rute,
- saldo s mock nadoplatom (kartica/PayPal/Revolut) i isplatom,
- ocjene i napojnice (karticom, pri ocjenjivanju),
- obavijesti i live praćenje trenutne vožnje (SignalR),
- **AI asistenta** koji čita podatke i priprema akcije ovisno o ulozi te pretražuje javni web,
- REST API i MCP poslužitelj za vanjske klijente.

## Uloge

| Uloga | Što može |
|---|---|
| **Putnik** | pretraga i rezervacija vožnji, plaćanje, ocjene, napojnice, saldo |
| **Vozač** | sve kao putnik + KYC, objava i vođenje vožnji, naplata na saldo |
| **Admin** | potpuni CRUD nad korisnicima, gradovima, vozilima, plaćanjima + audit |
