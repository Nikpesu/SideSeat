# Lab 5 — dokumentacija za predaju

| Podatak | Vrijednost |
| --- | --- |
| Projekt | SideSeat |
| Kolegij | Razvoj web aplikacija |
| Tema | API, autentikacija, upload i integracijski testovi |
| Rok predaje | 12. lipnja |
| Stanje | Spremno za predaju |

## Sažetak

Lab 5 implementiran je na postojećoj SideSeat ridesharing domeni. Entitet iz primjera zadatka `Quiz` domenski je prilagođen vožnjama i ocjenama: API pokriva sve glavne SideSeat entitete, a upload slika vezan je uz konkretnu ocjenu završene vožnje.

Implementirani su:

- REST API i DTO/request modeli za osam glavnih entiteta.
- ASP.NET Core Identity s lokalnom registracijom i prijavom.
- role `Admin`, `Driver` i `Passenger`.
- Google OAuth prijava i dopuna obaveznih lokalnih podataka.
- spremanje slika na disk uz metapodatke i relativnu putanju u bazi.
- integracijski testovi kroz stvarni HTTP sloj.

## Checklist prema zadatku

### 1. Kompletna API podrška — 2 boda

- [x] `GET all` za sve glavne entitete.
- [x] `GET by id` za sve glavne entitete.
- [x] `POST` za kreiranje.
- [x] `PUT` za izmjenu.
- [x] `DELETE` za brisanje.
- [x] Query pretraga gdje ima smisla.
- [x] DTO modeli odvojeni od EF entiteta.
- [x] Request modeli s validacijskim anotacijama.
- [x] Ugniježđeni DTO modeli za povezane podatke.
- [x] Odgovarajući statusi `200`, `201`, `204`, `400`, `401/403` i `404`.

### 2. Lokalna autentikacija i autorizacija — 1 bod

- [x] ASP.NET Core Identity.
- [x] Lokalna registracija i prijava.
- [x] `AppUser` proširen s `OIB`, `JMBG` i `KorisnikId`.
- [x] Identity korisnik povezan s domenskim zapisom `Korisnik`.
- [x] Role `Admin`, `Driver` i `Passenger`.
- [x] Seed rola i demo korisnika.
- [x] CRUD akcije ograničene prema roli i poslovnom pravilu.
- [x] Custom claim za domenski `KorisnikId`.

### 3. Upload datoteka — 1 bod

- [x] Upload vezan uz konkretan domenski zapis `OcjenaVoznje`.
- [x] Slike se spremaju na disk servera.
- [x] U bazu se spremaju metapodaci i relativna putanja.
- [x] Disk I/O koristi asinkroni `CopyToAsync`.
- [x] Validiraju se tip, ekstenzija, broj i veličina slika.
- [x] Slike su uključene u API DTO ocjene.
- [x] Thumbnail prikaz na svim prikazima recenzija.
- [x] Uvećanje slike klikom.
- [x] Brisanje ocjene uklanja povezane zapise i datoteke.

> Napomena za obranu: zadatak koristi kviz kao primjer. SideSeat nema kvizove pa je upload, prema domeni projekta, vezan uz ocjenu konkretne završene vožnje. Odabir više slika koristi standardni `multipart/form-data`, a server ih sprema asinkrono.

### 4. Google autentikacija — 1 bod

- [x] Dodan Google authentication provider.
- [x] Google gumb postoji na prijavi i registraciji.
- [x] Prva Google prijava traži obavezne SideSeat podatke.
- [x] Google račun povezuje se s Identity korisnikom.
- [x] Tajne se čuvaju kroz user-secrets ili environment varijable.

### 5. Integracijski testovi — 2 boda

- [x] xUnit testni projekt.
- [x] `WebApplicationFactory` pokreće stvarnu aplikaciju.
- [x] SQL Server zamijenjen je EF InMemory bazom.
- [x] Test authentication handler omogućuje provjeru rola.
- [x] Pokriveni uspješni CRUD scenariji.
- [x] Pokriveni nepostojeći ID-evi.
- [x] Pokriveni neispravni request modeli.
- [x] Pokrivena autorizacija.
- [x] Pokriven upload i HTTP posluživanje slika.
- [x] Svih 18 integracijskih testova prolazi.

## Dokaz implementacije po kriterijima

| Kriterij | Implementacija | Ključne datoteke |
| --- | --- | --- |
| API i DTO | osam API controllera, DTO/request modeli i mapper | `Controllers/Api`, `Models/Api` |
| Identity | Identity DbContext, `AppUser`, seed i claims | `Program.cs`, `SideSeatDbContext.cs`, `IdentityDataSeeder.cs` |
| Autorizacija | role i provjere vlasništva | API i MVC controlleri |
| Google login | provider i external-login confirmation | `Program.cs`, `AuthController.cs` |
| Upload | `OcjenaSlika`, disk storage i relativni URL | `OcjenaController.cs`, `OcjenaSlika.cs` |
| Testovi | HTTP integracijski testovi i InMemory baza | `tests/SideSeat.IntegrationTests` |

## API pregled

| Entitet | Ruta | GET pristup | Mutation pristup |
| --- | --- | --- | --- |
| Grad | `/api/gradovi` | javno | `Admin` |
| Korisnik | `/api/korisnici` | lista `Admin`; detalj admin ili vlasnik | `Admin` |
| Vozilo | `/api/vozila` | javno | `Admin` |
| Vožnja | `/api/voznje` | javno | `Admin`, `Driver` uz vlasništvo |
| Rezervacija | `/api/rezervacije` | prema korisniku i roli | `Admin` |
| Plaćanje | `/api/placanja` | `Admin` | `Admin` |
| Ocjena | `/api/ocjene` | javno | `Admin` |
| Saldo transakcija | `/api/saldo-transakcije` | `Admin` | `Admin` |

Podržani query parametri:

- `q` za gradove, korisnike, vozila, vožnje, rezervacije, ocjene i saldo transakcije.
- `date` za vožnje i plaćanja.

DTO modeli nalaze se u:

- `src/SideSeat/Models/Api/SideSeatApiDtos.cs`
- `src/SideSeat/Models/Api/ApiMapper.cs`

API ne vraća lozinke, Identity zapise ni EF navigacijski graf izravno.

## Identity i role

`SideSeatDbContext` nasljeđuje:

```csharp
IdentityDbContext<AppUser, IdentityRole<int>, int>
```

`AppUser` proširuje Identity korisnika s:

- `OIB`
- `JMBG`
- `KorisnikId`

Lokalna registracija stvara domenski `Korisnik` zapis i povezani Identity račun. Prijava koristi `SignInManager`, a opcija `Zapamti me` stvara trajni authentication cookie.

Role:

- `Admin` — puni administrativni pristup.
- `Driver` — upravljanje vlastitim vožnjama i rezervacijama putnika.
- `Passenger` — rezerviranje, pregled vlastitih rezervacija i ocjenjivanje.

Za pristup domenskim podacima koristi se custom claim `KorisnikId`, umjesto pogrešnog poistovjećivanja Identity ID-a i domenskog ID-a.

## Google login

Google provider registriran je u `Program.cs`. Pri prvoj prijavi korisnik potvrđuje:

- OIB
- JMBG
- adresu
- broj mobitela

Nakon potvrde stvaraju se povezani Identity i domenski korisnički zapisi.

### Obavezna lokalna konfiguracija

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "CLIENT_ID" --project src/SideSeat
dotnet user-secrets set "Authentication:Google:ClientSecret" "CLIENT_SECRET" --project src/SideSeat
```

U Google Cloud Console treba dodati:

```text
https://localhost:7119/signin-google
```

Za Docker:

```text
http://localhost:8080/signin-google
```

Google tajne nisu i ne smiju biti spremljene u repozitoriju.

## Upload slika recenzije

Model `OcjenaSlika` sprema:

- ID povezane ocjene.
- originalni naziv datoteke.
- relativnu web-putanju.
- MIME tip.
- veličinu.
- vrijeme stvaranja.

Datoteke se fizički spremaju u:

```text
src/SideSeat/wwwroot/uploads/ocjene/{ocjenaId}
```

Primjer zapisa u bazi:

```text
/uploads/ocjene/12/7e4621d9f7684cc58f7d918d5ea3e024.png
```

Ograničenja:

- najviše pet slika po recenziji.
- najviše 5 MB po slici.
- dopušteni JPG, JPEG, PNG, GIF i WEBP.
- nasumičan naziv spremljene datoteke sprječava kolizije.

Partial view `_OcjenaSlike.cshtml` koristi se na svim prikazima recenzija. Slike se prikazuju kao thumbnaili i otvaraju u modalnom prikazu.

Docker koristi volume:

```text
sideseat-uploads:/app/wwwroot/uploads
```

Zbog toga slike ostaju sačuvane nakon ponovnog stvaranja web containera.

## Integracijski testovi

Testni projekt:

```text
tests/SideSeat.IntegrationTests
```

Glavne komponente:

- `SideSeatTestFactory.cs` — testna aplikacija, InMemory baza i seed.
- `TestAuthHandler.cs` — testna autentikacija i role.
- `ApiCrudTests.cs` — API, autorizacija, upload i poslovni scenariji.

Pokriveni API resursi:

- gradovi
- korisnici
- vozila
- vožnje
- rezervacije
- plaćanja
- ocjene
- saldo transakcije

Dodatno se testiraju privatnost profila, prikazi prema ulozi, potvrđivanje rezervacije, završetak vožnje, `RememberMe`, profilna slika i slike recenzije.

### Pokretanje validacije

Iz root direktorija repozitorija:

```powershell
dotnet build src/SideSeat/SideSeat.csproj --no-restore
dotnet test tests/SideSeat.IntegrationTests/SideSeat.IntegrationTests.csproj --no-restore
```

Zadnji provjereni rezultat:

```text
Build succeeded
Passed: 15, Failed: 0, Skipped: 0
```

## Migracije

Za Lab 5 i naknadne dorade važne su migracije:

- Identity tablice i povezivanje korisnika.
- `MoveReviewImagesToOcjene`
- `AddProfileImagesAndReservationLifecycle`
- `AddUserNotifications`

Primjena migracija:

```powershell
dotnet ef database update --project src/SideSeat --startup-project src/SideSeat
```

Naredba se pokreće u rootu repozitorija, gdje se nalazi ova datoteka.

Docker aplikacija pri pokretanju automatski primjenjuje migracije prije seeda.

## Pokretanje za demonstraciju

### Bez Dockera

```powershell
dotnet restore src/SideSeat/SideSeat.csproj
dotnet ef database update --project src/SideSeat --startup-project src/SideSeat
dotnet run --project src/SideSeat
```

### S Dockerom

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Aplikacija:

```text
http://localhost:8080
```

### Preko Docker Huba

```bash
docker compose -f docker-compose.hub.yml pull
docker compose -f docker-compose.hub.yml up -d
```

Image:

```text
nikolica/sideseat:v0.11
```

Docker zadano koristi `DUMMY_DATA=false`. Za demonstraciju sa seedanim korisnicima u `.env` postavi:

```text
DUMMY_DATA=true
```

## Demo korisnici

Sljedeći korisnici postoje samo kada je `DUMMY_DATA=true`.

| Uloga | Email | Lozinka |
| --- | --- | --- |
| Admin | `admin@example.com` | `Admin123!` |
| Driver | `marko@example.com` | `User123!` |
| Passenger | `ivana@example.com` | `User123!` |

## Predloženi tijek obrane

1. Prijaviti se kao administrator i pokazati role i zaštićene prikaze.
2. Otvoriti jedan javni API `GET`, primjerice `/api/voznje`.
3. Pokazati DTO modele i jedan API controller s CRUD metodama.
4. Pokazati da anonimni korisnik ne može pozvati zaštićeni mutation endpoint.
5. Registrirati ili prijaviti lokalnog korisnika.
6. Pokazati Google gumb i objasniti user-secrets konfiguraciju.
7. Završiti rezervaciju i dodati ocjenu sa slikama.
8. Pokazati datoteku na disku i relativnu putanju u bazi/API odgovoru.
9. Pokrenuti integracijske testove i pokazati rezultat `18/18`.

## Što treba napraviti prije predaje

- [ ] Postaviti vlastiti Google Client ID i Client Secret kroz user-secrets.
- [ ] Provjeriti redirect URI u Google Cloud Console.
- [ ] Primijeniti migracije na lokalnu bazu.
- [ ] Pokrenuti aplikaciju i ručno provjeriti lokalnu i Google prijavu.
- [ ] Pokrenuti `dotnet test`.
- [ ] Ne commitati `.env`, lozinke ni Google tajne.

Sve ostale programske cjeline Lab 5 zadatka implementirane su u repozitoriju.
