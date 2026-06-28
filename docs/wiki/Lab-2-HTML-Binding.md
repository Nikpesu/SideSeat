# Lab 2 — HTML / Binding

[← Pregled labosa](Labs.md) · Predaja: **17.4.2026.** · Izvor: `docs/labs/lab-2/`

## Cilj i kriteriji

| Kriterij | Bodovi |
|---|---|
| Prompt za sub-agenta za UI/UX | 1 |
| Log da je sub-agent pozivan za UI/UX | 1 |
| Unique (non-standard) UX koji radi s mock repozitorijima | 2 |
| Usmeno ispitivanje rada s custom agentima | 1 |

Nužni uvjeti:
- Custom UX sub-agent (instruction file commitan na Git) + log da ga je glavni agent spawnao.
- Mock repository sa statičkim podacima iz Lab 1.
- Index (lista) i Details stranice za svaki entitet (bez Create/Edit), custom home page,
  kompletna navigacija (izbornik, linkovi lista→detalji).
- UX mora biti unique/non-standard (ne default Bootstrap).

## Implementacija u SideSeatu

**UX sub-agent** — definiran kao skill/agent u repozitoriju:
`.github/skills/futuristic-ui-design/` (instrukcije za stil, komponente i layout principe), uz
agent profil `.github/agents/`. Logovi poziva su u `docs/labs/lab-2/` (agent_log + transcripti,
uključujući `SubagentStart`).

**Unique UX** — sučelje nije default Bootstrap: vlastiti dizajn u
`src/SideSeat/wwwroot/css/site.css` (aurora pozadina, „command palette" pretraga, custom
kartice, animirana karta ruta s autićima u pozadini — `wwwroot/js/route-background.js`).
Layout je u `Views/Shared/_Layout.cshtml`.

**Index i Details stranice** — za sve entitete postoje liste i detalji preko MVC kontrolera:
`GradController`, `VoziloController`, `VoznjaController`, `RezervacijaController`,
`KorisnikController`, `OcjenaController`, `PlacanjeController` i pripadajući `Views/`.

**Custom home page** — `HomeController` + `Views/Home/Index.cshtml` (pretraga vožnji s kartom).

**Navigacija** — primarna navigacija i sidebar u `_Layout.cshtml` (s `data-testid` hookovima
poput `nav-rides`, `nav-reservations`, `sidebar-admin`), linkovi s listi na Details stranice.

> Napomena: kasniji labovi (3+) zamijenili su mock repozitorije pravim EF repozitorijem, ali
> arhitektura kontroler → repozitorij → view, postavljena u Lab 2, ostala je ista.

## Za usmeno

- Tok MVC zahtjeva: Browser → Controller → Action → Model → View → HTML.
- Razlika Model vs ViewModel i zašto je strongly-typed `@model` bolji od `ViewData`.
- Kako se daje kontekst sub-agentu i kako se kritički provjerava generirani UI.
- Što čini UX „non-standard" u ovom projektu.
