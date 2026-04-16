# SideSeat

SideSeat je seminarski projekt za kolegij Razvoj web aplikacija u ASP.NET MVC tehnologiji.
Ovaj README prati stvarni tok vjezbi (Lab 1 i Lab 2), a ne dugorocni roadmap projekta.

## Trenutni status

- Projekt je aktivno implementiran kroz Labski tok.
- Podaci su trenutno mock/static (bez baze podataka).
- Fokus je na MVC obrascu, navigaciji, razor view-ovima i razumijevanju koda.

## Tok vjezbe

### Lab 1 - Osnove C#, LINQ i async/await

Cilj vjezbe:

- Kreirati objektni model domene.
- Popuniti model testnim podacima.
- Napisati smislene LINQ upite.
- Primijeniti async/await koncept.

Sto je napravljeno u projektu:

- Objektni model i enumi nalaze se u `src/SideSeat/Models/Lab1`.
- Demo podaci i LINQ/async primjer su u `src/SideSeat/Models/Lab1/Lab1Demo.cs`.
- Entiteti koji se koriste kroz vjezbe:
    - `Grad`
    - `Korisnik`
    - `Vozilo`
    - `Voznja`
    - `Rezervacija`
    - `Placanje`
    - `OcjenaVoznje`

### Lab 2 - HTML Binding i MVC

Cilj vjezbe:

- Koristiti mock repository kao izvor podataka.
- Napraviti Index i Details stranice za sve entitete.
- Napraviti custom home/dashboard stranicu.
- Osigurati kompletnu navigaciju (menu, linkovi, breadcrumbs).
- Primijeniti unique UX pristup uz sub-agent workflow.

Sto je napravljeno u projektu:

- Centralni mock repository: `src/SideSeat/Repositories/LabMockRepository.cs`.
- Kontroleri s `Index` i `Details` akcijama za:
    - Grad
    - Korisnik
    - Vozilo
    - Voznja
    - Rezervacija
    - Placanje
    - Ocjena
- Dashboard stranica: `src/SideSeat/Views/Home/Index.cshtml`.
- Glavna navigacija i layout: `src/SideSeat/Views/Shared/_Layout.cshtml`.
- Custom styling i responzivnost: `src/SideSeat/wwwroot/css/site.css`.
- UX sub-agent konfiguracija: `.github/agents/ux-lab.agent.md`.

## Brzi pregled napretka

- [x] Lab 1 model + seed podaci
- [x] Lab 1 LINQ upiti i async/await demo
- [x] Lab 2 mock repository
- [x] Lab 2 Index/Details stranice za glavne entitete
- [x] Lab 2 dashboard + navigacija
- [x] Lab 2 custom UX smjernice kroz sub-agent

## Pokretanje projekta

Preduvjet:

- Instaliran .NET SDK (projekt targetira `net10.0`).

Pokretanje:

```bash
cd src/SideSeat
dotnet restore
dotnet run
```

Build provjera:

```bash
cd src/SideSeat
dotnet build
```

## Struktura repozitorija (bitno za predaju)

- `lab-1/`
    - opis zadatka i logovi rada agenta za Lab 1
- `lab-2/`
    - opis zadatka i logovi rada agenta za Lab 2
- `src/SideSeat/`
    - ASP.NET Core MVC aplikacija

## Napomena

README je uskladen s trenutnim stanjem implementacije i tokom vjezbi.
Za svaku iducu vjezbu preporuka je azurirati ovaj dokument nakon zavrsenog taska.
