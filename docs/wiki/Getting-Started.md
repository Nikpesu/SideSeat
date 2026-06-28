# Početak rada

[← Wiki](Home.md)

## Preduvjeti

- **.NET 10 SDK**
- **SQL Server** (lokalno ili preko Dockera) — ili pokretanje cijelog stacka kroz Docker Compose
- (opcionalno) **Docker Desktop** u Linux modu za kontejnere

## Pokretanje kroz Docker Compose (preporučeno)

```bash
cp .env.example .env        # popuni vrijednosti po potrebi
docker compose -f docker-compose.hub.yml up -d
```

Aplikacija je dostupna na `http://localhost:8080`. Compose podiže i SQL Server kontejner.
Za lokalni build umjesto gotovog imagea koristi `docker-compose.yml`.

## Lokalno pokretanje (dotnet)

```bash
dotnet restore
dotnet run --project src/SideSeat/SideSeat.csproj
```

Postavi connection string i ostale postavke kroz `appsettings.Development.json` ili varijable okruženja
(vidi [Konfiguraciju](Configuration.md)).

## Demo podaci

Kad je `DUMMY_DATA=true`, seedaju se demo korisnici i podaci. Prijava (vidljiva i na login formi):

| Email | Lozinka | Uloga |
|---|---|---|
| `admin@example.com` | `Admin123!` | Admin |
| `marko@example.com` | `User123!` | Vozač/Putnik |
| `ivana@example.com` | `User123!` | Putnik |

## Testovi

```bash
dotnet test tests/SideSeat.IntegrationTests/SideSeat.IntegrationTests.csproj
```

Testovi koriste in-memory bazu, pa ne traže SQL Server.

## Sljedeći korak

- Putnik? → [Vodič za putnika](User-Guide.md)
- Vozač? → [Vodič za vozača](Driver-Guide.md)
- Admin? → [Vodič za administratora](Admin-Guide.md)
