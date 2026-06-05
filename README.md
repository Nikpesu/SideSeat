# SideSeat

SideSeat je seminarski ASP.NET Core MVC projekt za kolegij Razvoj web aplikacija. Aplikacija pokriva ridesharing domenu: korisnici, vozila, vožnje, rezervacije, plaćanja, ocjene, autentikacija, API i upload datoteka.

## Trenutni Status

- Projekt targetira `.NET 10` i koristi EF Core + SQL Server.
- Autentikacija je prebačena na ASP.NET Core Identity.
- Implementirane su role `Admin`, `Driver` i `Passenger`.
- Dodan je Google external login kroz konfiguraciju iz secrets/env varijabli.
- Implementirani su REST API endpointi s DTO modelima za glavne entitete.
- Upload datoteka vezan je uz konkretnu vožnju.
- Dodani su integracijski testovi preko `WebApplicationFactory`.
- Projekt se može pokrenuti lokalno, kroz Docker Compose ili preko Docker Hub imagea.

## Brzi Start — Docker Hub

Najjednostavnije pokretanje na Linux Docker hostu:

```bash
mkdir sideseat
cd sideseat
curl -o docker-compose.yml https://raw.githubusercontent.com/nikolica/SideSeat/main/docker-compose.hub.yml
docker compose up -d
```

Ako nemaš GitHub raw link ili repo nije dostupan, samo kopiraj lokalni `docker-compose.hub.yml` na Linux server i pokreni:

```bash
docker compose -f docker-compose.hub.yml up -d
```

Aplikacija je dostupna na:

```text
http://localhost:8080
```

Docker Hub image:

```text
nikolica/sideseat:v0.1
```

Image je buildan kao Linux image (`linux/amd64`).

## Lokalno Docker Pokretanje

Za lokalni build imagea iz source koda:

```bash
docker compose up --build
```

Servisi:

- Web aplikacija: `http://localhost:8080`
- SQL Server container: `localhost,14333`
- Upload volume: `sideseat-uploads`
- SQL data volume: `sideseat-sql-data`

Za gašenje:

```bash
docker compose down
```

Za brisanje baze i upload volumena:

```bash
docker compose down -v
```

## Environment Varijable

Primjer `.env` datoteke:

```text
SA_PASSWORD=SideSeat123!
DOCKERHUB_IMAGE=nikolica/sideseat:v0.1
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
```

Ako koristiš Google login u Dockeru, u Google Console dodaj redirect URI:

```text
http://localhost:8080/signin-google
```

Za lokalni HTTPS development redirect URI:

```text
https://localhost:7119/signin-google
```

## Lokalno Pokretanje Bez Dockera

Preduvjeti:

- `.NET SDK 10`
- SQL Server LocalDB ili drugi SQL Server

Pokretanje:

```bash
cd src/SideSeat
dotnet restore
dotnet ef database update
dotnet run
```

Build:

```bash
dotnet build src/SideSeat/SideSeat.csproj
```

Testovi:

```bash
dotnet test tests/SideSeat.IntegrationTests/SideSeat.IntegrationTests.csproj
```

## Demo Korisnici

- Admin: `admin@example.com` / `Admin123!`
- Vozač: `marko@example.com` / `User123!`
- Putnik: `ivana@example.com` / `User123!`

## Lab Pregled

- [x] Lab 1 — C# model, LINQ i async/await demo
- [x] Lab 2 — MVC prikaz, dashboard i navigacija
- [x] Lab 3 — forme, view modeli, validacija i korisnički tokovi
- [x] Lab 4 — autentikacija, role, plaćanja/saldo i napredniji tokovi
- [x] Lab 5 — API, DTO, Identity, Google login, upload i integracijski testovi

Detalji za Lab 5 nalaze se u `lab5Doc.md`.

## API Sažetak

API endpointi su dostupni pod:

- `/api/gradovi`
- `/api/korisnici`
- `/api/vozila`
- `/api/voznje`
- `/api/rezervacije`
- `/api/placanja`
- `/api/ocjene`
- `/api/saldo-transakcije`

`GET` endpointi vraćaju DTO modele. `POST`, `PUT` i `DELETE` endpointi su zaštićeni autorizacijom prema ulozi i poslovnom pravilu.

## Upload Datoteka

Upload je vezan uz `Voznja` jer projekt nema `Quiz` entitet. Datoteke se spremaju na disk:

```text
wwwroot/uploads/voznje/{voznjaId}
```

Metapodaci se spremaju u bazu kroz `VoznjaAttachment`.

## Docker Hub Push

Ako treba ponovno objaviti verziju `v0.1`:

```powershell
docker login
.\scripts\docker-push-v0.1.ps1 -DockerHubUser nikolica
```

Linux/macOS:

```bash
docker login
chmod +x scripts/docker-push-v0.1.sh
./scripts/docker-push-v0.1.sh nikolica
```

## Struktura Repozitorija

- `src/SideSeat/` — ASP.NET Core MVC aplikacija
- `tests/SideSeat.IntegrationTests/` — integracijski testovi
- `lab-1/` do `lab-5/` — materijali i zadaci po vježbama
- `docker-compose.yml` — lokalni build + SQL Server
- `docker-compose.hub.yml` — Linux/Docker Hub pokretanje bez lokalnog builda
- `lab5Doc.md` — Lab 5 dokumentacija i checklist

## Napomena

Tajne podatke ne commitati u repozitorij. Koristi `.env`, user secrets ili environment varijable.
