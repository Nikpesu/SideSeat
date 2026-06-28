# Lab 5 — API, Auth, Testovi

[← Pregled labosa](Labs.md) · Predaja: **12.6.2026.** · Izvor: `docs/labs/lab-5/`

## Cilj i kriteriji

| Kriterij | Bodovi |
|---|---|
| Kompletna API podrška za sve entitete (CRUD, DTO) | 2 |
| Autentikacija (local accounts) i autorizacija | 1 |
| Upload datoteka (dropzone ili sl.) | 1 |
| 3rd party autentikacija (Google/FB…) | 1 |
| Integracijski testovi za API endpointe (svi, CRUD) | 2 |

Nužni uvjeti:
- API kontroleri: `GET` (svi + pretraga), `GET` po ID-u, `POST`, `PUT`, `DELETE`; **DTO** klase
  (bez izlaganja internih polja), ugniježđeni DTO za povezane podatke.
- Upload asinkrono (Dropzone), datoteke na disk, metapodaci u bazu, AJAX popis + brisanje.
- ASP.NET Core Identity: lokalna registracija/prijava, prošireni `AppUser`, autorizacija po
  ulogama (`Admin` + barem još jedna).
- Google ili Facebook login.
- Integracijski testovi: uspješni scenariji, nepostojeći ID-evi, validacijske pogreške.

## Implementacija u SideSeatu

**REST API + DTO** — `src/SideSeat/Controllers/Api/` označeni `[ApiController]`:
`GradoviApiController`, `KorisniciApiController`, `OcjeneApiController`, `PlacanjaApiController`,
`RezervacijeApiController`, `SaldoTransakcijeApiController`, `VozilaApiController`,
`VoznjeApiController`, plus `MapsApiController` i `SearchApiController`. DTO klase su u
`Models/Api`. Pogledaj i [REST API i MCP](REST-API.md).

**Autentikacija i autorizacija** — ASP.NET Core Identity konfiguriran u `Program.cs`
(`AddIdentity<AppUser, IdentityRole<int>>` + EF stores). Prijava/registracija u `AuthController`;
prošireni `AppUser` (npr. `OIB`, `JMBG`, `KorisnikId`). Uloge: `Admin`, `Driver`, `Passenger`
(`IdentityDataSeeder`). Autorizacija preko `[Authorize]`/`[Authorize(Roles=...)]` i custom
preusmjeravanja (API → 401/403, web → login modal).

**3rd party login** — Google OAuth (`AuthController.ExternalLogin/ExternalLoginCallback`),
uključuje se kad su `Authentication:Google:ClientId/Secret` postavljeni.

**Upload datoteka** — slike ocjena spremaju se na disk (volume `sideseat-uploads`), metapodaci u
bazu (`OcjenaSlika`); upload i popis idu AJAX-om (`site.js`), uz brisanje.

**Integracijski testovi** — `tests/SideSeat.IntegrationTests/` (xUnit + `Mvc.Testing`):
`ApiCrudTests`, `MvcCrudTests`, `SqlServerConstraintTests`, `RideWorkflowFeatureTests`,
`AiToolServiceTests`, `AiContextTests`, `CityGeocodingServiceTests`, `RouteGeometryServiceTests`,
`OpenWebUiServiceTests`, `NavigationTests`, `SeminarFeatureTests`. Pokrivaju uspješne scenarije,
nepostojeće ID-eve i validacijske pogreške.

**Dodatno (iznad labosa)** — End-to-end **Playwright** scenarij od 10 koraka u
`tests/SideSeat.E2ETests/` (prijava → globalna pretraga → navigacija → admin → audit → odjava).

## Za usmeno

- Zašto DTO umjesto izravnog vraćanja entiteta (sigurnost, kružne reference, oblik odgovora).
- HTTP metode i statusni kodovi (200/201/204/400/401/403/404).
- Razlika autentikacije i autorizacije; kako Identity sprema lozinke (hash) i radi s ulogama.
- Kako integracijski testovi pokreću app u memoriji (`WebApplicationFactory`) i testiraju API.
