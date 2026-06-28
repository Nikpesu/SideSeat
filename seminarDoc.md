# Seminar - dokumentacija projekta SideSeat

Ovaj dokument opisuje implementaciju seminarskih kriterija iz datoteke
`seminar.pdf` za projekt **SideSeat**.

SideSeat je ASP.NET Core MVC aplikacija za dijeljenje prijevoza. Sustav podržava
registraciju i prijavu, uloge korisnika, objavu vožnji, rezervacije, potvrđivanje
putnika, mock plaćanja, saldo, ocjene, obavijesti, REST API, AI asistenta,
globalnu pretragu, MCP i Docker deployment.

Datum završne lokalne provjere: **15. lipnja 2026.**

---

## 1. Kriteriji i procjena bodova

Projektni dio nosi 30 bodova. Dodatnih 40 bodova odnosi se na usmenu provjeru
razumijevanja koda.

| Kriterij | Bodovi | Status u SideSeatu |
| --- | ---: | --- |
| Deploy na Google ili Azure | 3 | Nije ispunjen; koristi se kućni Docker server |
| Testovi za sve endpointe | 2 | Pokrivene su sve REST obitelji i ključni MVC tokovi |
| AI unos podataka | 3 | Implementiran prepare/confirm/cancel tok |
| Global search | 2 | Implementiran role-aware `Ctrl+K` search |
| Logging mehanizam | 2 | JSON logovi, correlation ID i audit tablica |
| Responsive mobile/web UI | 2 | Implementirani breakpointi i prilagodljive tablice |
| CRUD bez grešaka | 2 | Validacija, autorizacija, transakcije i constrainti |
| MCP i agentic IDE pristup | 2 | Streamable HTTP MCP endpoint `/mcp` |
| Opći dojam i stabilnost | 12 | Health checks, exception handler, CI i Docker recovery |

Realni projektni maksimum bez Google/Azure deploymenta je **27/30 bodova**.
Konačnu odluku o bodovima donosi nastavnik.

---

## 2. Arhitektura rješenja

Glavni slojevi aplikacije:

```text
Razor MVC / REST API / AI / MCP
                |
                v
      SideSeatCommandService
                |
        validacija i autorizacija
                |
        EF Core transakcija
                |
       SQL Server + AuditLog
```

Zajednički command servis koriste klasični aplikacijski tokovi, AI i MCP.
Time se smanjuje mogućnost da AI ili MCP zaobiđu poslovna pravila koja vrijede
u korisničkom sučelju.

Glavne reference:

- [Program.cs](src/SideSeat/Program.cs)
- [SideSeatCommandService.cs](src/SideSeat/Services/SideSeatCommandService.cs)
- [CommandModels.cs](src/SideSeat/Models/Commands/CommandModels.cs)
- [SideSeatDbContext.cs](src/SideSeat/Data/SideSeatDbContext.cs)

---

## 3. Stabilan CRUD i poslovna pravila

### Podržani resursi

SideSeat ima MVC i REST operacije za glavne domenske entitete:

- gradove
- korisnike
- vozila
- vožnje
- rezervacije
- plaćanja
- ocjene
- saldo transakcije

REST rute:

| Resurs | Ruta |
| --- | --- |
| Gradovi | `/api/gradovi` |
| Korisnici | `/api/korisnici` |
| Vozila | `/api/vozila` |
| Vožnje | `/api/voznje` |
| Rezervacije | `/api/rezervacije` |
| Plaćanja | `/api/placanja` |
| Ocjene | `/api/ocjene` |
| Saldo transakcije | `/api/saldo-transakcije` |

### Zajednički command servis

`SideSeatCommandService` obrađuje naredbe:

- `create_city`
- `create_vehicle`
- `create_ride`
- `create_reservation`
- `create_review`

Servis provjerava:

- je li korisnik prijavljen
- ima li potrebnu rolu
- pripada li objekt prijavljenom korisniku
- postoje li povezani zapisi
- postoji li duplikat
- ima li vožnja slobodnih mjesta
- ima li putnik dovoljan raspoloživi saldo
- je li statusni prijelaz dopušten
- je li korisnik već rezervirao ili ocijenio isti zapis

Kreiranje vožnje iz MVC-a, REST API-ja, AI-ja i MCP-a prolazi kroz isti servis.

### Transakcije

Relacijske write operacije u command servisu izvršavaju se unutar EF Core
transakcije. Neuspješna naredba radi rollback.

Dodatno su `Serializable` transakcijom zaštićeni:

- kreiranje rezervacije
- potvrđivanje i odbijanje rezervacije
- završetak vožnje
- naplata putnika
- prijenos sredstava vozaču

To sprječava da paralelni zahtjevi:

- rezerviraju više mjesta nego što postoji
- dvaput potvrde istu rezervaciju
- dvaput naplate istu vožnju
- nekonzistentno promijene saldo

### Ograničenja baze

Migracija dodaje:

- `AuditLogs` tablicu
- unique indeks za kombinaciju naziva i države grada
- unique indeks za registraciju vozila
- preciznost decimalnih stupaca
- eksplicitna pravila relacija korisnika i vozila

Referenca:

- [AddAuditLoggingAndDataConstraints.cs](src/SideSeat/Migrations/20260614225553_AddAuditLoggingAndDataConstraints.cs)

### Standardizirane greške

Command rezultat razlikuje:

- `Validation`
- `Forbidden`
- `NotFound`
- `Conflict`
- `BusinessRule`

AI i API ih mapiraju na:

- `400 Bad Request`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`

Globalni `ProblemDetails` odgovor sadrži i correlation ID.

---

## 4. AI integracija i unos podataka

SideSeat AI radi preko backend gatewaya prema Open WebUI ili DeepSeek
kompatibilnom API-ju. API ključ nikada se ne šalje pregledniku.

### Read alati

AI može dohvatiti aktualne podatke kroz:

- `get_current_user`
- `get_rides`
- `get_reservations`
- `get_balance`

Rezultati se filtriraju prema prijavljenom korisniku i njegovim ulogama.

### Write alati

Implementirani su:

- `prepare_create_city`
- `prepare_create_vehicle`
- `prepare_create_ride`
- `prepare_create_reservation`
- `prepare_create_review`

AI ne zapisuje podatak odmah.

Tok unosa:

1. Korisnik prirodnim jezikom zatraži unos.
2. AI poziva odgovarajući `prepare_create_*` alat.
3. Backend pripremi strukturiranu naredbu.
4. Stvara se kriptografski nasumičan confirmation token.
5. Token vrijedi pet minuta.
6. UI prikazuje sažetak te gumbe **Potvrdi** i **Odustani**.
7. Tek potvrda poziva command servis i izvršava upis.

Sigurnosna pravila:

- token je vezan uz korisnika koji ga je stvorio
- token je jednokratan
- istekli token ne izvršava naredbu
- otkazani token ne izvršava naredbu
- token drugog korisnika vraća zabranu
- potvrđena operacija ponovno provjerava autorizaciju i poslovna pravila

Endpointi:

```text
POST /api/ai/chat
POST /api/ai/actions/{token}/confirm
POST /api/ai/actions/{token}/cancel
```

Reference:

- [AiController.cs](src/SideSeat/Controllers/AiController.cs)
- [AiToolService.cs](src/SideSeat/Services/AiToolService.cs)
- [PendingActionService.cs](src/SideSeat/Services/PendingActionService.cs)
- [_AiAssistant.cshtml](src/SideSeat/Views/Shared/_AiAssistant.cshtml)
- [site.js](src/SideSeat/wwwroot/js/site.js)

### Demonstracija

Primjer upita administratora:

```text
Kreiraj grad Osijek u Hrvatskoj s poštanskim brojem 31000.
```

Očekivano ponašanje:

1. AI vrati pregled akcije.
2. Grad još nije zapisan u bazu.
3. Klikom na **Potvrdi** grad se kreira.
4. Ponovljeni klik istim tokenom ne radi novi upis.
5. Operacija je vidljiva u audit zapisu.

---

## 5. Globalna pretraga

Globalna pretraga dostupna je prijavljenom korisniku:

- gumbom u navigaciji
- tipkovničkim prečacem `Ctrl+K`
- prečacem `Cmd+K` na macOS-u
- mobilnim prikazom navigacije

Endpoint:

```text
GET /api/search?q=pojam
```

Pretražuju se:

- stranice i izbornici
- vožnje
- rezervacije
- gradovi
- korisnici
- vozila

Rezultati su grupirani i podržavaju:

- debounce mrežnih zahtjeva
- kretanje strelicama
- potvrdu tipkom Enter
- zatvaranje tipkom Escape
- prikaz najviše 30 rezultata

### Role-aware zaštita

Putnik ne može kroz search dobiti:

- listu korisnika
- administratorske stranice
- administrativne podatke vozila i gradova
- rezervacije drugih korisnika

Administrator dobiva dodatne grupe rezultata. Vozač dobiva poveznice i podatke
vezane uz vlastite vožnje.

Reference:

- [SearchApiController.cs](src/SideSeat/Controllers/Api/SearchApiController.cs)
- [_Layout.cshtml](src/SideSeat/Views/Shared/_Layout.cshtml)
- [site.js](src/SideSeat/wwwroot/js/site.js)

---

## 6. Logging, audit i stabilnost

### Strukturirani logging

Aplikacija koristi JSON console logging. Svaki obrađeni HTTP zahtjev zapisuje:

- HTTP metodu
- putanju
- status odgovora
- trajanje u milisekundama
- korisnika
- correlation ID

Klijent može poslati `X-Correlation-ID`. Ako ga nema ili je neispravan, server
stvara vlastiti. Vrijednost se vraća kroz response header.

Referenca:

- [RequestObservabilityMiddleware.cs](src/SideSeat/Middleware/RequestObservabilityMiddleware.cs)

### Audit zapis

`AuditLog` čuva:

- UTC vrijeme
- ID korisnika
- naziv aktera
- izvor: MVC, API, AI ili MCP
- radnju
- tip i ID entiteta
- uspjeh ili odbijanje
- siguran sažetak
- correlation ID

Audit ne sprema:

- lozinke
- API ključeve
- puni broj kartice
- CVV
- OIB ili JMBG
- sadržaj uploadane datoteke

Administrator može otvoriti `/Audit` i filtrirati zapis prema izvoru, radnji i
rezultatu.

Reference:

- [AuditLog.cs](src/SideSeat/Models/AuditLog.cs)
- [AuditService.cs](src/SideSeat/Services/AuditService.cs)
- [AuditController.cs](src/SideSeat/Controllers/AuditController.cs)
- [Audit Index](src/SideSeat/Views/Audit/Index.cshtml)

### Globalni exception handler

Neočekivana API ili MCP greška vraća `ProblemDetails` s correlation ID-em.
MVC greška preusmjerava na sigurnu error stranicu koja prikazuje request ID,
bez izlaganja stack tracea korisniku.

Referenca:

- [GlobalExceptionHandler.cs](src/SideSeat/Services/GlobalExceptionHandler.cs)

### Health endpointi

```text
GET /health/live
GET /health/ready
```

`live` potvrđuje da proces radi. `ready` dodatno provjerava vezu s bazom.
Docker koristi `ready` endpoint prije nego Caddy počne slati promet aplikaciji.

---

## 7. Responsive mobile/web UI

Layout sadrži obavezni viewport meta tag:

```html
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
```

CSS koristi breakpointe za:

- mobilne uređaje do `767.98px`
- tablet i manji desktop do `991.98px`
- prikaze od `768px`, `1024px` i `1400px`

Prilagođeni su:

- navigacija
- forme
- tablice
- kartični prikazi
- AI panel
- globalna pretraga
- modalni prozori
- footer

Tablice se nalaze unutar `.ss-table-shell` spremnika s kontroliranim
horizontalnim scrollom. Time se izbjegava overflow cijele stranice.

Na mobilnom se tablice označene klasom `.is-card-table` pretvaraju u kartični
prikaz.

Reference:

- [site.css](src/SideSeat/wwwroot/css/site.css)
- [performative.css](src/SideSeat/wwwroot/css/performative.css)
- [_Layout.cshtml](src/SideSeat/Views/Shared/_Layout.cshtml)

Preporučene širine za ručnu demonstraciju:

- `390px`
- `768px`
- desktop širina

---

## 8. MCP i agentic IDE

Projekt koristi službeni paket:

```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="1.4.0" />
```

MCP koristi Streamable HTTP endpoint:

```text
/mcp
```

### Autentikacija

Endpoint zahtijeva:

```http
Authorization: Bearer <MCP_API_KEY>
```

Ključ se čita iz environment konfiguracije. MCP middleware stvara principal za
konfiguriranog servisnog korisnika:

- `Mcp__KorisnikId`
- `Mcp__Actor`
- `Mcp__Roles__0`, `Mcp__Roles__1`, ...

Neispravan ključ vraća `401 Unauthorized`. Ako ključ nije konfiguriran, endpoint
vraća `503 Service Unavailable`.

### MCP alati

Read alati:

- `get_current_user`
- `get_rides`
- `get_reservations`
- `get_balance`

Write tok:

- `prepare_create_city`
- `prepare_create_vehicle`
- `prepare_create_ride`
- `prepare_create_reservation`
- `prepare_create_review`
- `confirm_action`
- `cancel_action`

Write alati koriste isti confirmation i command sustav kao AI.

### MCP resursi

```text
sideseat://sitemap
sideseat://api-summary
```

Reference:

- [SideSeatMcpTools.cs](src/SideSeat/Mcp/SideSeatMcpTools.cs)
- [SideSeatMcpResources.cs](src/SideSeat/Mcp/SideSeatMcpResources.cs)
- [McpApiKeyMiddleware.cs](src/SideSeat/Middleware/McpApiKeyMiddleware.cs)

### Primjer IDE konfiguracije

```json
{
  "servers": {
    "sideseat": {
      "type": "http",
      "url": "https://sideseat.example.com/mcp",
      "headers": {
        "Authorization": "Bearer ${input:sideseat-mcp-key}"
      }
    }
  }
}
```

Nakon povezivanja agent može:

1. pročitati `sideseat://sitemap`
2. pozvati `get_rides`
3. pripremiti write operaciju
4. prikazati confirmation token
5. izvršiti `confirm_action`

---

## 9. Testovi

Završna lokalna provjera:

```text
Passed: 36
Failed: 0
Skipped: 0
```

Pokretanje:

```bash
dotnet restore SideSeat.slnx
dotnet build SideSeat.slnx --configuration Release --no-restore
dotnet test tests/SideSeat.IntegrationTests/SideSeat.IntegrationTests.csproj \
  --configuration Release
```

### Pokriveni REST resursi

Testirani su CRUD, validacija, autorizacija i `404` scenariji za:

- gradove
- korisnike
- vozila
- vožnje
- rezervacije
- plaćanja
- ocjene
- saldo transakcije

### Pokriveni poslovni tokovi

- prikaz podataka prema ulozi
- zaštita tuđeg korisničkog profila
- nedovoljan saldo
- mock nadoplata karticom, PayPalom i Revolut Payom
- potvrđivanje rezervacije
- zabrana završetka vožnje s neriješenim rezervacijama
- završetak vožnje i naplata
- profilne slike
- slike ocjena
- čišćenje dummy podataka
- AI kontekst i tool-call krug

### Seminar testovi

Posebno su testirani:

- skrivanje admin search rezultata putniku
- `/health/live` i `/health/ready`
- vlasništvo confirmation tokena
- jednokratnost tokena
- audit uspješne akcije
- AI prepare bez neposrednog upisa
- odbijanje neispravnog MCP ključa
- uspješan MCP `initialize` handshake
- HTTP metode API endpointa kroz `EndpointDataSource`

Reference:

- [ApiCrudTests.cs](tests/SideSeat.IntegrationTests/ApiCrudTests.cs)
- [MvcCrudTests.cs](tests/SideSeat.IntegrationTests/MvcCrudTests.cs)
- [SeminarFeatureTests.cs](tests/SideSeat.IntegrationTests/SeminarFeatureTests.cs)
- [SqlServerConstraintTests.cs](tests/SideSeat.IntegrationTests/SqlServerConstraintTests.cs)

### Stvarni SQL Server test

`SqlServerConstraintTests`:

1. stvara privremenu SQL Server bazu
2. primjenjuje cijeli migration chain
3. provjerava novu seminar migraciju
4. upisuje grad
5. pokušava upisati duplikat
6. očekuje `DbUpdateException`
7. briše privremenu bazu

Time se provjeravaju stvarni relacijski constrainti, a ne samo EF InMemory
ponašanje.

### CI

GitHub Actions izvršava:

- checkout
- instalaciju .NET 10 SDK-a
- NuGet restore
- Release build
- SQL Server 2022 servis
- InMemory i SQL Server integracijske testove
- TRX i code coverage artefakt

Referenca:

- [dotnet-ci.yml](.github/workflows/dotnet-ci.yml)

---

## 10. Produkcijski Docker deployment

Iako kriterij izričito traži Google ili Azure, projekt ima pripremljen stabilan
deployment za Linux/AMD64 kućni server.

Produkcijski stack sadrži:

- `sideseat-web`
- SQL Server 2022
- Caddy reverse proxy
- automatski HTTPS certifikat
- health checkove
- trajne volume
- rotaciju Docker logova

Datoteke:

- [docker-compose.prod.yml](docker-compose.prod.yml)
- [Caddyfile](Caddyfile)
- [Dockerfile](src/SideSeat/Dockerfile)
- [backup-sql.sh](scripts/backup-sql.sh)
- [restore-sql.sh](scripts/restore-sql.sh)

### Potrebna konfiguracija

```dotenv
SA_PASSWORD=promijeni_jaku_lozinku
APP_DOMAIN=sideseat.example.com
SIDESEAT_IMAGE=nikolica/sideseat:latest
MCP_API_KEY=dugi_nasumicni_kljuc
MCP_USER_ID=1
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
AI_API_TYPE=OpenWebUi
AI_BASE_URL=https://ai.example.com
AI_API_KEY=
AI_MODEL=
DUMMY_DATA=false
```

### Pokretanje

```bash
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
```

### Backup

```bash
set -a
source .env
set +a
bash scripts/backup-sql.sh
```

### Rollback aplikacije

U `.env` se postavi prethodni immutable image tag:

```dotenv
SIDESEAT_IMAGE=nikolica/sideseat:v0.23
```

Zatim:

```bash
docker compose -f docker-compose.prod.yml pull sideseat-web
docker compose -f docker-compose.prod.yml up -d --no-deps sideseat-web
```

SQL podaci, uploadi, backupi i Caddy certifikati ostaju spremljeni u volumeima.

### Migracijski retry

Aplikacija pri Docker pokretanju pokušava primijeniti migracije do šest puta.
To rješava situaciju u kojoj je SQL Server container pokrenut, ali još nije
potpuno spreman za prihvat konekcije.

---

## 11. Predloženi scenarij demonstracije

### Korak 1: stabilnost

Otvoriti:

```text
/health/live
/health/ready
```

Oba endpointa trebaju vratiti `Healthy`.

### Korak 2: CRUD

1. Prijaviti se kao administrator.
2. Kreirati novi grad.
3. Pokušati kreirati isti grad ponovno.
4. Pokazati da je duplikat odbijen.
5. Otvoriti audit zapis.

### Korak 3: poslovni tok

1. Vozač kreira vožnju.
2. Putnik rezervira mjesto.
3. Vozač potvrđuje rezervaciju.
4. Vozač završava vožnju.
5. Pokazati plaćanje i promjene salda.
6. Putnik kreira ocjenu.

### Korak 4: AI unos

1. Administrator traži AI-em kreiranje grada.
2. Pokazati da zapis još ne postoji.
3. Kliknuti **Potvrdi**.
4. Pokazati novi grad i audit zapis.
5. Pokušati ponoviti isti token.

### Korak 5: global search

1. Pritisnuti `Ctrl+K`.
2. Pretražiti grad ili vožnju.
3. Kretati se strelicama i otvoriti rezultat Enterom.
4. Kao putnik pretražiti ime drugog korisnika.
5. Pokazati da nema administratorskih rezultata.

### Korak 6: MCP

1. Povezati MCP Inspector, Codex ili VS Code na `/mcp`.
2. Poslati Bearer ključ.
3. Dohvatiti `sideseat://sitemap`.
4. Pozvati `get_rides`.
5. Pozvati `prepare_create_city`.
6. Potvrditi akciju kroz `confirm_action`.

### Korak 7: responsive

U browser developer alatima pokazati:

- mobilni prikaz na `390px`
- tablet prikaz na `768px`
- desktop prikaz

Posebno otvoriti:

- tablicu rezervacija
- global search
- AI panel
- formu za vožnju

### Korak 8: restart

```bash
docker compose -f docker-compose.prod.yml restart
```

Nakon restarta pokazati da SQL podaci i uploadi nisu izgubljeni.

---

## 12. Pitanja za usmenu provjeru

### Zašto AI ne smije odmah napraviti upis?

AI može pogrešno protumačiti korisnikov zahtjev. Prepare/confirm tok korisniku
daje pregled strukturirane naredbe prije trajne promjene baze.

### Zašto confirmation token mora biti vezan uz korisnika?

Bez provjere vlasništva drugi prijavljeni korisnik mogao bi iskoristiti tuđi
token i izvršiti naredbu s tuđim namjerama.

### Zašto se token briše prije izvršavanja naredbe?

Time postaje jednokratan i smanjuje se mogućnost dvostrukog izvršavanja zbog
ponovljenog klika ili mrežnog retryja.

### Zašto su AI, MCP, MVC i API spojeni na zajednički servis?

Poslovna pravila i autorizacija moraju biti ista bez obzira na ulazni kanal.
Inače bi jedan kanal mogao dopustiti operaciju koju drugi pravilno zabranjuje.

### Zašto se koristi transakcija?

Jedna poslovna operacija može promijeniti više tablica. Ako dio operacije ne
uspije, rollback vraća bazu u prethodno konzistentno stanje.

### Zašto je za mjesta i saldo važna `Serializable` izolacija?

Paralelni zahtjevi ne smiju oba pročitati isto staro stanje i zatim zajedno
prekoračiti kapacitet vožnje ili dvaput promijeniti saldo.

### Koja je razlika između request loga i audit loga?

Request log opisuje tehnički HTTP zahtjev. Audit log opisuje poslovnu radnju,
aktera, entitet i rezultat te ostaje spremljen u bazi.

### Zašto audit ne sprema cijeli request body?

Request može sadržavati lozinke, osobne podatke, kartične podatke ili sadržaj
datoteke. Audit sprema samo kontrolirani sažetak.

### Što je correlation ID?

To je identifikator zahtjeva koji povezuje odgovor korisniku, request log,
exception log i audit zapis iste operacije.

### Koja je razlika između `/health/live` i `/health/ready`?

`live` provjerava radi li proces. `ready` provjerava može li aplikacija stvarno
posluživati promet, uključujući dostupnost baze.

### Zašto search mora biti role-aware?

Pretraga je dodatni način pristupa podacima. Ako nije autorizirana, može izložiti
osobne ili administrativne podatke iako su njihove originalne stranice zaštićene.

### Kako je MCP zaštićen?

Bearer API ključ provjerava se constant-time usporedbom. Nakon provjere stvara se
principal konfiguriranog servisnog korisnika, pa alati koriste postojeća pravila
uloga i vlasništva.

### Zašto se koriste MCP resursi?

Resursi agentu daju stabilan, čitljiv kontekst poput sitemap-a i sažetka
mogućnosti bez potrebe da svaki put poziva poslovni alat.

### Zašto InMemory testovi nisu dovoljni?

InMemory provider ne provodi sva SQL relacijska pravila, transakcije i unique
indekse kao SQL Server. Zato postoji poseban SQL Server migration/constraint test.

### Zašto cloud kriterij nije označen kao ispunjen?

Specifikacija izričito navodi Google ili Azure. Kućni server s Dockerom, domenom
i HTTPS-om tehnički je deployment, ali nije deployment na traženom cloud
provideru.

---

## 13. Završna provjera

- [x] Release build prolazi bez warninga i grešaka
- [x] Svih 36 testova prolazi
- [x] SQL Server migration test prolazi
- [x] MCP autentikacija i initialize handshake prolaze
- [x] AI write akcije zahtijevaju potvrdu
- [x] Confirmation token je jednokratan i vezan uz korisnika
- [x] Global search filtrira rezultate prema ulozi
- [x] JSON logging i correlation ID su uključeni
- [x] Audit pregled dostupan je administratoru
- [x] Health endpointi su implementirani
- [x] Produkcijski Compose je sintaktički valjan
- [x] SQL backup, restore i image rollback su dokumentirani
- [x] Responsive CSS pokriva mobile, tablet i desktop
- [ ] Deployment na Google ili Azure nije napravljen
- [ ] Playwright browser testovi nisu dodani

---

## 14. Zaključak

SideSeat ispunjava funkcionalne seminarske kriterije za AI unos podataka,
globalnu pretragu, logging, responsive sučelje, stabilan CRUD i MCP. Stabilnost
je dodatno ojačana transakcijama, relacijskim ograničenjima, audit zapisom,
globalnim exception handlerom, health checkovima, CI testovima i produkcijskim
Docker stackom.

Jedino jasno odstupanje od specifikacije je hosting: aplikacija je pripremljena
za kućni Linux Docker server, a ne za Google Cloud ili Azure. Zbog toga je
realni maksimum projektnog dijela **27/30 bodova**, prije usmene provjere
razumijevanja koda.
