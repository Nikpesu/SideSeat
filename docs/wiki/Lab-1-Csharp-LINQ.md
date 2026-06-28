# Lab 1 — Osnove C# / LINQ

[← Pregled labosa](Labs.md) · Predaja: **3.4.2026.** · Izvor: `docs/labs/lab-1/`

## Cilj i kriteriji

| Kriterij | Bodovi |
|---|---|
| Granularno izvođenje agenta po taskovima | 1 |
| Ispitivanje agenta za pojašnjenja | 1 |
| Usmeno ispitivanje LINQ/await ili C# koncepata | 2 |
| Objektni model projektnog zadatka | 2 |

Nužni uvjeti:
- Javni GitHub repozitorij, sav ocjenjivani kod na `main`, log korištenja AI agenta u `lab-1/`.
- Objektni model: **≥ 7 klasa**, od toga **≥ 4 kompleksne** (> 5 svojstava), s barem jednim
  vlastitim `enum`, barem jednim `DateTime` svojstvom i ispravnim 1-N i N-N vezama.
- U `Main` programu napuniti barem 3 razgranata glavna objekta i napisati smislene LINQ upite.
- Razumjeti `async`-`await`.

## Implementacija u SideSeatu

**Objektni model** (`src/SideSeat/Models/Entities/`) — domenske klase:
`Korisnik`, `Voznja`, `Rezervacija`, `Vozilo`, `Grad`, `Ocjena`, `OcjenaSlika`, `Placanje`,
`SaldoTransakcija`, `Obavijest`, `AuditLog`. Time je premašen uvjet od 7 klasa, a kompleksne
klase (`Korisnik`, `Voznja`, `Rezervacija`, `Placanje`) imaju više od 5 smislenih svojstava.

- **Vlastiti enumi** (`src/SideSeat/Models/Enums/`): npr. `TipKorisnika`, `StatusVoznje` i drugi
  statusi koji upravljaju poslovnim tijekom.
- **DateTime svojstva**: `Voznja.Polazak`, `Rezervacija.VrijemeRezervacije`,
  `Korisnik.DatumRegistracije`, `AuditLog.CreatedAtUtc` itd.
- **Veze**:
  - 1-N: `Grad` → `Voznja` (polazni/odredišni grad), `Voznja` → `Rezervacija`,
    `Korisnik` → `Vozilo`, `Voznja` → `Ocjena`.
  - N-N (preko spojnog entiteta): `Korisnik` ↔ `Voznja` kroz `Rezervacija` (putnici po vožnji).

**Punjenje podataka i LINQ** — demonstracijski seed i upiti su u `Models/Demo`
(`Lab1Demo.RunAsync()`, pozvan iz `Program.cs` kad je `DUMMY_DATA` uključen). Isti tip LINQ
logike kasnije živi u stvarnoj aplikaciji, npr. filtriranje i projekcije u
`Controllers/Api/SearchApiController.cs` (`Where`, `OrderBy`, `Select`, `Take`, podupiti `Any`).

**Async-await** — cijela aplikacija je async: repozitorij i kontroleri koriste
`async Task<...>` i `await` (npr. `SideSeatEfRepository`, `await dbContext.SaveChangesAsync()`),
a `Program.Main` je `async Task Main`.

**AI agent log** — `docs/labs/lab-1/agent_log.txt` / `.jsonl` i transcripti dokazuju
granularno, task-po-task vođenje agenta i ispitivanje za pojašnjenja.

## Za usmeno

- Razlika `First` / `FirstOrDefault` / `Single` / `SingleOrDefault`.
- Što je lambda izraz i kako `Where`/`Select` koriste `Func<>`/`Predicate<>`.
- Razlika 1-N i N-N veze i kako se N-N modelira spojnim entitetom (`Rezervacija`).
- Što radi `await` (oslobađa dretvu dok traje I/O) i zašto je bitno na serveru.
