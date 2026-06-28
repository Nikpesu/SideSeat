# SideSeat — arhitektura, folderi i klase

Ovaj dokument ukratko objašnjava **svaki folder** u projektu i **svaku klasu** u njemu.
Aplikacija je ASP.NET Core MVC (.NET 10) za dijeljenje vožnji (ride-sharing) s integriranim
AI asistentom, REST API-jem, MCP poslužiteljem i kartama ruta.

> Za korisnički vodič vidi [docs/wiki/Home.md](wiki/Home.md). Za tijek vožnje i novca
> vidi [docs/vožnja.md](vožnja.md).

## Pregled rješenja

```
SideSeat/
├── src/SideSeat/                 # MVC web aplikacija (glavni projekt)
├── tests/SideSeat.IntegrationTests/  # integracijski testovi (xUnit)
├── docs/                         # dokumentacija (ovaj folder)
│   ├── wiki/                     # wikipedia-style pomoć
│   └── labs/                     # laboratorijske vježbe + logovi/transkripti
├── changelogs/                   # changelog po verziji (v0.x.md)
├── scripts/                      # pomoćne skripte (docker, sql backup)
└── .github/                      # workflowovi, skills, hooks, agenti
```

---

## `src/SideSeat` — glavni projekt

### Korijenske datoteke
- **Program.cs** — ulazna točka; konfigurira DI, autentifikaciju (Cookies + Identity + Google), EF Core, rate limiting, AI/MCP servise, middleware i rute.
- **SideSeat.csproj** — projektni fajl (.NET 10, NuGet ovisnosti).
- **Dockerfile** — multi-stage build (SDK → publish → aspnet runtime).
- **appsettings*.json** — konfiguracija (baza, AI, karte, web pretraga).

### `Controllers/` — MVC kontroleri (HTML stranice)
- **HomeController.cs** — početni dashboard, pretraga ruta, vodič (`Vodic`), privacy, HTTP status stranice.
- **AuthController.cs** — prijava, registracija, odjava, Google vanjska prijava.
- **KorisnikController.cs** — profil, postavke, KYC vozača, **saldo** (uplata/isplata), CRUD korisnika (admin).
- **VoznjaController.cs** — vožnje: pregled, kreiranje, uređivanje, aktivne/trenutne vožnje, start/završetak.
- **RezervacijaController.cs** — rezervacije: kreiranje, potvrda/odbijanje, plaćanje, detalji, admin CRUD.
- **OcjenaController.cs** — ocjene vožnji + **napojnica karticom**, prilozi (slike), admin feedback.
- **GradController.cs** — CRUD gradova (admin).
- **VoziloController.cs** — CRUD vozila (admin).
- **PlacanjeController.cs** — CRUD plaćanja (admin).
- **ObavijestController.cs** — obavijesti (notifikacije) korisnika.
- **AuditController.cs** — pregled audit zapisa (admin).
- **ConfirmationController.cs** — stranice potvrde rezervacije/vožnje.
- **AiController.cs** — backend za AI chat widget (prima poruke, vraća odgovor + pending akciju).
- **AiActionController.cs** — pregled i potvrda/odbijanje AI pripremljenih akcija (`prepare_*`).

### `Controllers/Api/` — REST API (JSON)
CRUD i pretraga preko `api/...` ruta, korištene od SPA dijelova, MCP-a i vanjskih klijenata.
- **GradoviApiController.cs**, **KorisniciApiController.cs**, **VozilaApiController.cs**,
  **VoznjeApiController.cs**, **RezervacijeApiController.cs**, **OcjeneApiController.cs**,
  **PlacanjaApiController.cs**, **SaldoTransakcijeApiController.cs** — REST CRUD nad pripadnim entitetima.
- **MapsApiController.cs** — `api/maps/route`, vraća geometriju rute (OSRM) za karte i pozadinsku animaciju.
- **SearchApiController.cs** — globalna pretraga (stranice, vožnje, rezervacije…).

### `Data/` — pristup podacima
- **SideSeatDbContext.cs** — EF Core DbContext; DbSetovi i konfiguracija modela/odnosa.
- **IdentityDataSeeder.cs** — seed Identity uloga (`Admin`, `Driver`, `Passenger`) i demo korisnika.
- **DummyDataCleaner.cs** — uklanjanje/čišćenje demo (dummy) podataka.

### `Models/` — domenski modeli i view modeli
- **Entities/** — domenski entiteti perzistirani u bazi: `Grad`, `Korisnik`, `Vozilo`, `Voznja`,
  `Rezervacija`, `Placanje`, `OcjenaVoznje`, `OcjenaSlika`, `SaldoTransakcija`, `Obavijest`, `RideChatMessage`.
- **Enums/** — enumeracije: `TipKorisnika`, `StatusVoznje`, `StatusRezervacije`
  (+`StatusRezervacijeExtensions` za prikazna imena), `NacinPlacanja`.
- **Demo/** — `Lab1Podaci`/`Lab1Demo`: in-memory demo podaci (povijesni lab materijal).
- **Forms/** — `VoznjaFormViewModel`, `RezervacijaFormViewModel`: form modeli za kreiranje/uređivanje.
- **Dashboard/** — `Lab2DashboardViewModel`: povijesni dashboard model.
- **Home/** — `HomeDashboardViewModel`: model početne stranice (statistika + pretraga ruta).
- **Auth/** — `LoginViewModel`, `RegisterViewModel`, `UserSettingsViewModel`,
  `DriverKycViewModel`, `ExternalLoginConfirmationViewModel`.
- **Korisnik/** — `KorisnikProfileViewModel`, `SaldoViewModel`, `MockTopUpViewModel` (mock checkout).
- **Ocjena/** — view modeli ocjena: kreiranje/uređivanje, slike, admin feedback, kartice/tabovi.
- **Rezervacija/** — `RezervacijaListItemViewModel`.
- **Voznja/** — `VoznjaListViewModel`, `VoznjaDetailsViewModel`.
- **Notifications/** — `NotificationBellViewModel` (zvonce obavijesti).
- **Ai/** — `AiChatMessage`, `AiChatRequest`, `AiChatResponse` (DTO-i AI chata).
- **Api/** — `SideSeatApiDtos` (REST DTO-i) i `ApiMapper` (mapiranje entitet ↔ DTO).
- **Commands/** — `CommandModels.cs`: command recordi (`CreateRideCommand`…), `SideSeatActionTypes`
  (konstante akcija), `PendingActionDescriptor`/`PendingActionEnvelope`, `CommandResult`.
- **ViewModels/** — zajednički view modeli i helperi: forme korisnika/plaćanja/admin rezervacija/ocjena,
  `RouteMapViewModel`, `AutocompleteLookupViewModel`, `DateTimeInputViewModel`, `PageSizeOptions`.
- **AppUser.cs** — ASP.NET Identity korisnik (vezan na domenski `Korisnik`).
- **AuditLog.cs** — zapis audita (tko/što/kada).
- **ErrorViewModel.cs** — model error stranice.

### `Services/` — poslovna logika i integracije
- **SideSeatCommandService.cs** (`ISideSeatCommandService`) — izvršava sve write akcije (CRUD, ride workflow,
  settlement salda/gotovine) uz autorizaciju i poslovna pravila; izvršava i potvrđene AI akcije.
- **PendingActionService.cs** (`IPendingActionService`) — priprema i pohranjuje AI akcije koje čekaju potvrdu.
- **AiToolService.cs** (`IAiToolService`) — definicije AI alata (tools) + izvršavanje; **role-bazirano serviranje alata**.
- **AiContextService.cs** (`IAiContextService`) — gradi poslovni kontekst/sitemap za AI.
- **OpenWebUiService.cs** (`IOpenWebUiService`) — komunikacija s AI providerom (OpenWebUI/DeepSeek), tool-calling petlja.
- **OpenWebUiOptions.cs**, **AiApiType.cs** — konfiguracija i tip AI API-ja.
- **PublicWebSearchService.cs** (`IPublicWebSearchService`) — **dohvat podataka s interneta** (Wikipedia + DuckDuckGo) s cacheom i timeoutima; **PublicWebSearchOptions.cs**.
- **OsrmRouteGeometryService.cs** (`IRouteGeometryService`) — geometrija ceste (OSRM) za rute.
- **NominatimCityGeocodingService.cs** (`ICityGeocodingService`) — geokodiranje gradova (Nominatim); **MapsOptions.cs**.
- **NotificationService.cs** (`INotificationService` u modelu obavijesti) — kreiranje obavijesti korisnicima.
- **AuditService.cs** (`IAuditService`) — pisanje audit zapisa.
- **PasswordHashingService.cs** (`IPasswordHashingService`) — hashiranje/verifikacija lozinki.
- **DatabaseHealthCheck.cs** — health check baze.
- **GlobalExceptionHandler.cs** — centralno rukovanje iznimkama.

### `Hubs/`
- **RideHub.cs** — SignalR hub za live vožnju (lokacije, chat, status u realnom vremenu).

### `Mcp/` — Model Context Protocol poslužitelj
- **SideSeatMcpTools.cs** — MCP alati (akcije aplikacije za vanjske AI klijente).
- **SideSeatMcpResources.cs** — MCP resursi (podaci dostupni MCP klijentu).

### `Middleware/`
- **McpApiKeyMiddleware.cs** — provjera MCP API ključa i uloga.
- **RequestObservabilityMiddleware.cs** — logging/telemetrija zahtjeva.

### `Security/`
- **SideSeatClaimTypes.cs** — konstante custom claimova (`sideseat:korisnik_id`).
- **ClaimsPrincipalExtensions.cs** — helperi (`GetKorisnikId()` itd.).
- **SideSeatUserClaimsPrincipalFactory.cs** — dodaje domenske claimove pri prijavi.

### `Repositories/`
- **SideSeatEfRepository.cs** — EF Core repozitorij (čitanje agregata).
- **LabMockRepository.cs** — in-memory repozitorij (povijesni lab materijal).

### `ViewComponents/`
- **ObavijestiBellViewComponent.cs** — komponenta zvonca obavijesti u navigaciji.

### `Views/` — Razor pogledi (`.cshtml`)
Po jedan folder za svaki kontroler (`Home`, `Auth`, `Korisnik`, `Voznja`, `Rezervacija`, `Ocjena`,
`Grad`, `Vozilo`, `Placanje`, `Audit`, `AiAction`, `Confirmation`) plus:
- **Shared/** — `_Layout`, `_AiAssistant` (AI widget), `_RouteMapBackground` (pozadinska animacija ruta),
  `_RouteMap`, validacijski/partial pogledi i `Components/ObavijestiBell`.

### `wwwroot/` — statički sadržaj
- **css/** — `site.css` (glavni), `performative.css` (efekti), `route-maps.css` (karte).
- **js/** — `site.js` (UI logika), `route-maps.js` (interaktivne karte/preview),
  `route-background.js` (animacija auta po stvarnim rutama u pozadini).
- **lib/** — vendor biblioteke (Bootstrap, jQuery, Leaflet).
- **images/**, **favicon.\*** — slike i ikone.

### `Migrations/`
- EF Core migracije baze (shema, ograničenja, audit, koordinate gradova, ride workflow).

### `Properties/`
- **launchSettings.json** — profili lokalnog pokretanja.

---

## `tests/SideSeat.IntegrationTests`
xUnit integracijski testovi koji koriste in-memory bazu (`SideSeatTestFactory`):
- **MvcCrudTests / ApiCrudTests** — CRUD i autorizacija kroz MVC i REST.
- **AiToolServiceTests** — AI alati, role-bazirani pristup, lookup gradova.
- **AiContextTests** — poslovni kontekst za AI.
- **OpenWebUiServiceTests** — tool-calling petlja prema AI provideru (fake HTTP).
- **RideWorkflowFeatureTests** — životni ciklus vožnje i settlement.
- **CityGeocodingServiceTests / RouteGeometryServiceTests** — karte/rute.
- **NavigationTests / SeminarFeatureTests / SqlServerConstraintTests** — navigacija, značajke, DB ograničenja.
- **TestAuthHandler** — test autentifikacija (uloge preko zaglavlja).

---

## `.github/`
- **workflows/** — CI (build/test).
- **skills/** — projektni skillovi (npr. `version-control-dockerhub` za izdavanje na Docker Hub).
- **agents/**, **hooks/** — agentske definicije i hookovi.
