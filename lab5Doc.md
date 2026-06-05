# Lab 5 dokumentacija — SideSeat

## Checklist

- [x] API podrška za sve glavne entitete
  - [x] `Grad`
  - [x] `Korisnik`
  - [x] `Vozilo`
  - [x] `Voznja`
  - [x] `Rezervacija`
  - [x] `Placanje`
  - [x] `OcjenaVoznje`
  - [x] `SaldoTransakcija`
- [x] DTO/request modeli za API
- [x] Lokalna autentikacija kroz ASP.NET Core Identity
- [x] Role `Admin`, `Driver`, `Passenger`
- [x] Google 3rd party login konfiguracija
- [x] Upload datoteka vezan uz konkretnu voznju
- [x] AJAX listanje i brisanje datoteka
- [x] Integracijski testovi za API CRUD i upload
- [x] Migracija za Identity tablice i `VoznjaAttachments`

## Što je napravljeno

### API i DTO

Dodani su DTO/request modeli u `src/SideSeat/Models/Api`. API ne vraća EF entitete direktno, nego kontrolirani JSON oblik bez internih polja poput `LozinkaHash`.

Dodani su API controlleri:

- `GET /api/gradovi`, `POST /api/gradovi`, `PUT /api/gradovi/{id}`, `DELETE /api/gradovi/{id}`
- `GET /api/korisnici`, `POST /api/korisnici`, `PUT /api/korisnici/{id}`, `DELETE /api/korisnici/{id}`
- `GET /api/vozila`, `POST /api/vozila`, `PUT /api/vozila/{id}`, `DELETE /api/vozila/{id}`
- `GET /api/voznje`, `POST /api/voznje`, `PUT /api/voznje/{id}`, `DELETE /api/voznje/{id}`
- `GET /api/rezervacije`, `POST /api/rezervacije`, `PUT /api/rezervacije/{id}`, `DELETE /api/rezervacije/{id}`
- `GET /api/placanja`, `POST /api/placanja`, `PUT /api/placanja/{id}`, `DELETE /api/placanja/{id}`
- `GET /api/ocjene`, `POST /api/ocjene`, `PUT /api/ocjene/{id}`, `DELETE /api/ocjene/{id}`
- `GET /api/saldo-transakcije`, `POST /api/saldo-transakcije`, `PUT /api/saldo-transakcije/{id}`, `DELETE /api/saldo-transakcije/{id}`

### Identity, role i Google login

Dodana je klasa `AppUser` s poljima `OIB`, `JMBG` i vezom na domenskog `Korisnik` zapisa. `SideSeatDbContext` sada nasljeđuje `IdentityDbContext<AppUser, IdentityRole<int>, int>`.

`AuthController` koristi `UserManager<AppUser>` i `SignInManager<AppUser>` za lokalni login/register. Seedani korisnici se mapiraju u Identity korisnike kroz `IdentityDataSeeder`.

Google login je dodan kroz `AddGoogle`. Kod prve Google prijave korisnik dopunjava lokalne podatke kroz `ExternalLoginConfirmation`.

### Upload datoteka

Dodana je tablica/model `VoznjaAttachment`. Upload je vezan uz konkretnu voznju jer projekt nema kviz.

Na `Voznja/Edit` stranici dodan je Dropzone upload. Datoteke se spremaju u:

```text
wwwroot/uploads/voznje/{voznjaId}
```

Metapodaci i web putanja spremaju se u bazu. Lista datoteka učitava se AJAX pozivom, a datoteke se mogu obrisati.

### Integracijski testovi

Dodan je projekt `tests/SideSeat.IntegrationTests`. Testovi koriste:

- `WebApplicationFactory`
- EF InMemory bazu
- test authentication handler
- stvarne HTTP pozive prema API endpointima

Pokriti su CRUD scenariji, nepostojeći ID-evi, invalid `POST` modeli, osnovna autorizacija i upload/list/delete za privitke vožnji.

## Što ti trebaš napraviti

### Docker pokretanje

Projekt se može pokrenuti kroz Docker Compose. Prvo po želji kopiraj primjer environment datoteke:

```powershell
Copy-Item .env.example .env
```

Ako koristiš Google login u Dockeru, u `.env` upiši `GOOGLE_CLIENT_ID` i `GOOGLE_CLIENT_SECRET`. U Google Console dodaj i Docker redirect URI:

```text
http://localhost:8080/signin-google
```

Pokretanje:

```powershell
docker compose up --build
```

Aplikacija će biti dostupna na:

```text
http://localhost:8080
```

SQL Server iz containera izložen je lokalno na `localhost,14333`. Aplikacija u Docker okruženju automatski pokreće EF migracije prije seeda Identity korisnika.

Za gašenje:

```powershell
docker compose down
```

Za brisanje Docker baze i upload volumena:

```powershell
docker compose down -v
```

Za Docker Hub varijantu bez lokalnog builda:

```powershell
.\scripts\docker-push-v0.1.ps1 -DockerHubUser nikolica
```

Na Linux hostu koristi `docker-compose.hub.yml`, postavi `.env`:

```text
DOCKERHUB_IMAGE=nikolica/sideseat:v0.1
SA_PASSWORD=SideSeat123!
```

Pokretanje na Linuxu:

```bash
docker compose -f docker-compose.hub.yml up -d
```

### 1. Unesi Google tajne lokalno

Ne stavljaj tajne u kod ni u Git. Pokreni:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "OVDJE_CLIENT_ID" --project src/SideSeat
dotnet user-secrets set "Authentication:Google:ClientSecret" "OVDJE_CLIENT_SECRET" --project src/SideSeat
```

U Google Console dodaj redirect URI:

```text
https://localhost:7119/signin-google
```

### 2. Primijeni migraciju na lokalnu bazu

Ako baza nije ažurirana:

```powershell
dotnet ef database update --project src/SideSeat --startup-project src/SideSeat
```

### 3. Provjeri aplikaciju ručno

Pokreni aplikaciju:

```powershell
dotnet run --project src/SideSeat
```

Zatim provjeri:

- lokalna registracija traži `OIB` i `JMBG`
- demo login radi (`admin@example.com / Admin123!`)
- Google login otvara Google consent screen
- `Voznja/Edit` prikazuje upload zonu
- upload, listanje i brisanje datoteka rade za admina ili vlasnika vožnje

## Validacija

Pokrenuto:

```powershell
dotnet build src/SideSeat/SideSeat.csproj --no-restore
dotnet test tests/SideSeat.IntegrationTests/SideSeat.IntegrationTests.csproj --no-restore
```

Rezultat: build prolazi i integracijski testovi prolaze.
