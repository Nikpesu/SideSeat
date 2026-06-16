# REST API i MCP

[← Wiki](Home.md)

## REST API

JSON API je pod prefiksom `api/`. Kontroleri su u `src/SideSeat/Controllers/Api/`.

| Resurs | Ruta | Opis |
|---|---|---|
| Gradovi | `api/gradovi` | CRUD gradova |
| Korisnici | `api/korisnici` | CRUD korisnika |
| Vozila | `api/vozila` | CRUD vozila |
| Vožnje | `api/voznje` | CRUD vožnji |
| Rezervacije | `api/rezervacije` | CRUD rezervacija |
| Ocjene | `api/ocjene` | CRUD ocjena |
| Plaćanja | `api/placanja` | CRUD plaćanja |
| Saldo transakcije | `api/saldo-transakcije` | transakcije salda |
| Karte | `api/maps/route` | geometrija rute (OSRM) za karte i pozadinsku animaciju |
| Pretraga | (search API) | globalna pretraga stranica/vožnji/rezervacija |

> Točne rute i DTO-e provjeri u kontrolerima i `Models/Api/SideSeatApiDtos.cs`; mapiranje radi `ApiMapper`.
> Autorizacija prati uloge kao i MVC dio (npr. admin-only CRUD).

### Karta rute
`GET /api/maps/route?startLat&startLng&endLat&endLng` vraća `{ points: [[lat,lng]...], distanceMeters, durationSeconds }`.
Koristi se za prikaz ruta i za animaciju auta po stvarnim rutama u pozadini (`route-background.js`).

## MCP poslužitelj (Model Context Protocol)

SideSeat izlaže MCP poslužitelj za vanjske AI klijente:
- **SideSeatMcpTools** — akcije aplikacije kao MCP alati,
- **SideSeatMcpResources** — podaci dostupni MCP klijentu.

Pristup štiti `McpApiKeyMiddleware` (`MCP_API_KEY`), a identitet/uloge dolaze iz konfiguracije
(`MCP_USER_ID`, role postavke). Vidi [Konfiguraciju](Configuration.md).

## SignalR (live vožnja)
`RideHub` (`Hubs/RideHub.cs`) emitira lokacije, status i poruke trenutne vožnje u realnom vremenu
(stranice *Trenutna/Aktivna vožnja*).
