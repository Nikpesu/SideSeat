# SideSeat

> ASP.NET Core MVC ridesharing aplikacija izrađena za kolegij **Razvoj web aplikacija**.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](src/SideSeat/SideSeat.csproj)
[![.NET CI](https://github.com/Nikpesu/SideSeat/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/Nikpesu/SideSeat/actions/workflows/dotnet-ci.yml)
[![Docker](https://img.shields.io/badge/Docker-Linux%20AMD64-2496ED?logo=docker&logoColor=white)](docker-compose.hub.yml)
[![Version](https://img.shields.io/badge/version-v0.37-2ea44f)](changelogs/v0.37.md)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

SideSeat povezuje vozače i putnike kroz objavu vožnji, rezervacije, potvrđivanje putnika, plaćanja, ocjene i obavijesti. Projekt uključuje ASP.NET Core Identity, Google prijavu, REST API, upload slika, AI asistenta za Open WebUI ili DeepSeek, SQL Server, Docker i integracijske testove.

## Sadržaj

- [Dokumentacija](#dokumentacija)
- [Mogućnosti](#mogućnosti)
- [Tehnologije](#tehnologije)
- [Brzi start preko Docker Huba](#brzi-start-preko-docker-huba)
- [Visual Studio](#visual-studio)
- [Lokalno pokretanje](#lokalno-pokretanje)
- [Konfiguracija](#konfiguracija)
- [Plaćanja i saldo](#plaćanja-i-saldo)
- [API](#api)
- [Upload slika](#upload-slika)
- [Testovi](#testovi)
- [Changelog wiki](#changelog-wiki)
- [Dokumentacija laboratorija](#dokumentacija-laboratorija)

## Dokumentacija

Sva dokumentacija je u Markdownu i čitljiva izravno na GitHubu, u folderu [`docs/`](docs/README.md):

| Dokument | Sadržaj |
| --- | --- |
| [docs/README.md](docs/README.md) | središnji indeks dokumentacije |
| [Wiki — početna](docs/wiki/Home.md) | wikipedia-style pomoć (vodiči po ulozi, značajke) |
| [Arhitektura, folderi i klase](docs/ARCHITECTURE.md) | kratko objašnjenje svakog foldera i klase |
| [Vodič za putnika](docs/wiki/User-Guide.md) · [vozača](docs/wiki/Driver-Guide.md) · [admina](docs/wiki/Admin-Guide.md) | upute po ulozi |
| [Saldo i plaćanja](docs/wiki/Payments-and-Balance.md) · [AI asistent](docs/wiki/AI-Assistant.md) | ključne značajke |
| [REST API i MCP](docs/wiki/REST-API.md) · [Konfiguracija](docs/wiki/Configuration.md) · [Deployment](docs/wiki/Deployment.md) | tehnička referenca |
| [Hodogram vožnje](docs/vožnja.md) | korak-po-korak za vozača i putnika |
| [Changelog](changelogs/README.md) | povijest verzija |

## Mogućnosti

- Registracija i prijava lokalnim računom ili Google računom.
- Role `Admin`, `Driver` i `Passenger` s autorizacijom po poslovnim pravilima.
- Objavljivanje i filtriranje dostupnih, vlastitih i odvezenih vožnji.
- Rezervacije koje vozač potvrđuje ili odbija prije vožnje.
- Provjera raspoloživog salda prije rezervacije, uključujući već rezervirana sredstva.
- Završetak vožnje, naplata potvrđenih rezervacija i evidencija salda.
- Mock checkout karticom, PayPalom ili Revolut Payom bez stvarne naplate.
- Ocjene s komentarom i do pet slika spremljenih na server.
- Profilne slike i ograničen prikaz osobnih podataka drugih korisnika.
- Trajne obavijesti sa zvoncem i brojačem za rezervacije, vožnje, naplatu, saldo i ocjene.
- Light i dark tema s trajno spremljenim korisničkim odabirom.
- Performative AI vizualni sustav s aurorom, glass karticama, animiranim metrikama i mikrointerakcijama.
- Globalna animirana karta rute i prikaz verzije aplikacije u footeru.
- SideSeat AI Copilot povezan na privatni Open WebUI server kroz sigurni backend proxy.
- REST API s DTO modelima, validacijom, pretragom i CRUD operacijama.
- Globalna role-aware pretraga dostupna preko `Ctrl+K`.
- AI i MCP write alati s pregledom i jednokratnom potvrdom prije upisa.
- JSON request logging, correlation ID, audit zapis i health endpointi.
- Docker Compose okruženje s aplikacijom, SQL Serverom i trajnim volumeima.

## Tehnologije

| Područje | Tehnologija |
| --- | --- |
| Backend | ASP.NET Core MVC, .NET 10 |
| Baza | SQL Server 2022, Entity Framework Core 10 |
| Autentikacija | ASP.NET Core Identity, Google OAuth |
| Frontend | Razor Views, Bootstrap, JavaScript, Performative UI inspirirani design system |
| AI | Open WebUI Chat Completions API preko server-side gatewaya |
| Testovi | xUnit, `WebApplicationFactory`, EF Core InMemory |
| Deployment | Docker, Docker Compose, Docker Hub |

## Brzi start preko Docker Huba

Za Linux Docker host dovoljan je `docker-compose.hub.yml`:

```bash
mkdir sideseat && cd sideseat
curl -o docker-compose.yml https://raw.githubusercontent.com/nikolica/SideSeat/main/docker-compose.hub.yml
docker compose up -d
```

Aplikacija je dostupna na `http://localhost:8080`.

Objavljeni image:

```text
nikolica/sideseat:v0.37
```

Tag `latest` pokazuje na isto izdanje. Image je namijenjen platformi `linux/amd64`.

```bash
docker pull nikolica/sideseat:v0.37
```

Digest izdanja:

```text
sha256:c541b4707587f12b36b663d8b2894d86d8bc3682cc4b6dc93cab07b4d0bd7709
```

SQL podaci i uploadane slike čuvaju se u Docker volumeima. Aplikacija pri pokretanju automatski primjenjuje EF Core migracije.

Korisne naredbe:

```bash
docker compose pull
docker compose up -d
docker compose logs -f sideseat-web
docker compose down
```

Za brisanje baze i uploada:

```bash
docker compose down -v
```

## Visual Studio

Otvori root solution `SideSeat.slnx` u Visual Studio 2022 s instaliranim workloadovima **ASP.NET and web development** i, za Docker način rada, **Container development tools**.

### Pokretanje bez Dockera

1. Kao startup projekt odaberi `SideSeat`.
2. U gornjem izborniku odaberi profil `https`.
3. Pokreni projekt tipkom `F5` ili `Ctrl+F5`.

Aplikacija koristi SQL Server LocalDB i pri pokretanju automatski primjenjuje EF Core migracije. Otvara se na `https://localhost:7119`.

### Pokretanje kroz Docker Compose

1. Pokreni Docker Desktop u Linux container načinu rada.
2. Desnim klikom na `docker-compose` odaberi **Set as Startup Project**.
3. Pokreni projekt tipkom `F5` ili `Ctrl+F5`.

Visual Studio tada pokreće i `sideseat-web` i `sideseat-db`. Aplikacija je dostupna na `http://localhost:8080`, a podaci i slike ostaju spremljeni u Docker volumeima.

## Lokalno pokretanje

### Preduvjeti

- .NET SDK 10
- SQL Server LocalDB ili drugi SQL Server
- EF Core CLI alat: `dotnet tool install --global dotnet-ef`

### Pokretanje bez Dockera

```bash
dotnet restore src/SideSeat/SideSeat.csproj
dotnet ef database update --project src/SideSeat --startup-project src/SideSeat
dotnet run --project src/SideSeat
```

### Lokalni Docker build

```bash
docker compose up --build
```

Servisi:

- aplikacija: `http://localhost:8080`
- SQL Server: `localhost,14333`
- upload volume: `sideseat-uploads`
- SQL volume: `sideseat-sql-data`

## Konfiguracija

Kopiraj primjer konfiguracije:

```powershell
Copy-Item .env.example .env
```

Najvažnije varijable:

```dotenv
SA_PASSWORD=SideSeat123!
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
DUMMY_DATA=false
AI_API_TYPE=OpenWebUi
AI_BASE_URL=https://ai.pesut.win
AI_API_KEY=
AI_MODEL=
APP_DOMAIN=sideseat.example.com
MCP_API_KEY=generiraj_dugi_nasumicni_kljuc
MCP_USER_ID=1
MAPS_ROUTING_BASE_URL=https://router.project-osrm.org
MAPS_ROUTING_TIMEOUT_MILLISECONDS=750
```

Verzija, `ASPNETCORE_ENVIRONMENT` i `ASPNETCORE_URLS` ugrađeni su u Docker image. Aplikacija u Dockeru automatski sastavlja connection string za `sideseat-db`; preko `.env` mijenja se samo `SA_PASSWORD`. Napredni overrideovi ostavljeni su zakomentirani u Compose datotekama.

`DUMMY_DATA` je zadano `false`. Za Docker demonstraciju sa seedanim testnim podacima postavi:

```dotenv
DUMMY_DATA=true
```

Točne cestovne linije dohvaćaju se preko OSRM route servisa. Aplikacija odmah crta lokalni fallback, čeka najviše 750 ms na OSRM te uspješne geometrije cacheira sedam dana.

SideSeat AI podržava Open WebUI i direktni DeepSeek API. API ključ ostaje samo u Docker environmentu i nikada se ne šalje pregledniku.

Open WebUI konfiguracija:

```dotenv
AI_API_TYPE=OpenWebUi
AI_BASE_URL=https://ai.pesut.win
AI_API_KEY=upiši_svoj_open_webui_api_ključ
AI_MODEL=
```

Direktna DeepSeek konfiguracija:

```dotenv
AI_API_TYPE=DeepSeek
AI_BASE_URL=https://api.deepseek.com
AI_API_KEY=upiši_svoj_deepseek_api_ključ
AI_MODEL=deepseek-v4-flash
```

`AI_MODEL` nije obavezan. Kada je prazan, aplikacija automatski dohvaća prvi model preko providerova models endpointa. Za lokalno pokretanje bez Dockera koristi user-secrets:

```powershell
dotnet user-secrets set "OpenWebUi:ApiType" "OpenWebUi" --project src/SideSeat
dotnet user-secrets set "OpenWebUi:ApiKey" "API_KEY" --project src/SideSeat
dotnet user-secrets set "OpenWebUi:Model" "MODEL_ID" --project src/SideSeat
dotnet user-secrets set "OpenWebUi:BaseUrl" "https://ai.pesut.win" --project src/SideSeat
```

Za DeepSeek promijeni `OpenWebUi:ApiType` u `DeepSeek` i `OpenWebUi:BaseUrl` u `https://api.deepseek.com`. AI zahtjevi idu preko `/api/ai/chat`, ograničeni su rate limiterom i ne izlažu providerov ključ klijentskom JavaScriptu.

AI system kontekst sadrži trenutačnu stranicu i sitemap svih stranica dostupnih ulozi prijavljenog korisnika. Za aktualne poslovne podatke agent koristi server-side read-only alate:

- `get_current_user` za sigurne podatke trenutačnog korisnika.
- `get_rides` za dostupne, putničke, vozačke ili administratorske vožnje.
- `get_reservations` za vlastite rezervacije i rezervacije korisnikovih vožnji.
- `get_balance` za saldo, rezervirana sredstva i transakcije.

Razgovor i otvoreno stanje AI panela čuvaju se unutar trenutačnog taba tijekom navigacije. Nakon pet minuta neaktivnosti razgovor se automatski briše, a gumb `↻` odmah pokreće novi razgovor.

Google tajne za lokalni razvoj postavljaju se izvan repozitorija:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "CLIENT_ID" --project src/SideSeat
dotnet user-secrets set "Authentication:Google:ClientSecret" "CLIENT_SECRET" --project src/SideSeat
```

Redirect URI-jevi:

- lokalni HTTPS: `https://localhost:7119/signin-google`
- Docker: `http://localhost:8080/signin-google`

Tajne se ne smiju commitati u Git.

## Demo korisnici

Demo korisnici postoje samo kada je `DUMMY_DATA=true`.

| Uloga | Email | Lozinka |
| --- | --- | --- |
| Admin | `admin@example.com` | `Admin123!` |
| Vozač | `marko@example.com` | `User123!` |
| Putnik | `ivana@example.com` | `User123!` |

## Plaćanja i saldo

Pri rezervaciji se provjerava može li saldo pokriti novu rezervaciju i sve ranije zakazane rezervacije. Ako sredstva nisu dovoljna, prikazuje se crveni toast s iznosom koji nedostaje i poveznicom na uplatu sredstava.

Checkout je demonstracijski i ne komunicira sa stvarnim platnim servisima:

- Kartica koristi format `4444 4444 4444 4444`, datum `MM/GG` te CVV od tri ili četiri znamenke.
- PayPal i Revolut Pay otvaraju modal preko zatamnjene stranice za potvrdu ili otkazivanje mock transakcije.
- Adresa plaćanja unosi se odvojeno kao ulica, kućni broj, poštanski broj i država.
- Povijest salda prikazuje korišteni servis i maskirani izvor, primjerice karticu `*4444` ili naziv PayPal/Revolut računa.
- Puni broj kartice i CVV ne spremaju se u bazu.

## API

| Resurs | Ruta | Pretraga |
| --- | --- | --- |
| Gradovi | `/api/gradovi` | `?q=` |
| Korisnici | `/api/korisnici` | `?q=` |
| Vozila | `/api/vozila` | `?q=` |
| Vožnje | `/api/voznje` | `?q=`, `?date=` |
| Rezervacije | `/api/rezervacije` | `?q=` |
| Plaćanja | `/api/placanja` | `?date=` |
| Ocjene | `/api/ocjene` | `?q=` |
| Saldo transakcije | `/api/saldo-transakcije` | `?q=` |

Svaki resurs podržava `GET all`, `GET by id`, `POST`, `PUT` i `DELETE` gdje poslovna pravila dopuštaju. API koristi DTO/request modele i ne izlaže EF entitete ni Identity podatke izravno.

Mutation endpointi zaštićeni su rolama. Korisnički podaci, plaćanja i saldo imaju stroža pravila pristupa.

## Upload slika

Slike recenzija spremaju se na server:

```text
wwwroot/uploads/ocjene/{ocjenaId}
```

U bazi se čuvaju metapodaci i relativna web-putanja, primjerice:

```text
/uploads/ocjene/12/slika.png
```

Podržani su JPG, PNG, GIF i WEBP, najviše pet slika i 5 MB po slici. Prikazuju se kao thumbnaili, a klik otvara uvećani prikaz.

Profilne slike koriste `wwwroot/uploads/profili/{korisnikId}` i također se u bazi sprema samo relativna putanja.

## MCP

Streamable HTTP MCP endpoint je `/mcp` i zahtijeva `Authorization: Bearer <MCP_API_KEY>`.
Servisni korisnik i uloge postavljaju se kroz `MCP_USER_ID` i `Mcp__Roles__*`. MCP izlaže
role-aware read alate, prepare/confirm write alate te resurse `sideseat://sitemap` i
`sideseat://api-summary`.

Primjer za Codex ili VS Code MCP konfiguraciju:

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

Endpoint se može provjeriti i službenim MCP Inspectorom uz isti URL i Bearer header.

## Produkcijski deployment

`docker-compose.prod.yml` pokreće SQL Server, aplikaciju i Caddy s automatskim HTTPS-om:

```bash
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
```

Prije nadogradnje napravi backup:

```bash
set -a && source .env && set +a
bash scripts/backup-sql.sh
```

Rollback se radi postavljanjem prethodnog immutable image taga u `SIDESEAT_IMAGE`, zatim:

```bash
docker compose -f docker-compose.prod.yml pull sideseat-web
docker compose -f docker-compose.prod.yml up -d --no-deps sideseat-web
```

Za povrat baze koristi `scripts/restore-sql.sh` s putanjom `.bak` datoteke unutar backup
volumea. SQL podaci, uploadi, backupi i Caddy certifikati ostaju u trajnim volumeima, a
Docker JSON logovi rotiraju se na pet datoteka po 10 MB.

## Testovi

GitHub Actions automatski pokreće Release build i sve integracijske testove na svaki push i pull request prema grani `main`. TRX rezultat i code coverage spremaju se kao `sideseat-test-results` artefakt.

```bash
dotnet test tests/SideSeat.IntegrationTests/SideSeat.IntegrationTests.csproj
```

Testovi pokrivaju:

- API CRUD, validaciju, `404` i autorizaciju.
- ograničen prikaz tuđeg korisničkog profila.
- preglede vožnji i rezervacija prema ulozi.
- životni ciklus rezervacije i završetak vožnje.
- provjeru nedovoljnog salda i preusmjeravanje na uplatu.
- mock uplate karticom, PayPalom i Revolut Payom te zapis servisa plaćanja.
- lokalnu prijavu s opcijom `Zapamti me`.
- profilne slike i slike recenzija.
- uključivanje i čišćenje demo podataka preko `DUMMY_DATA`.
- autorizirane AI alate, tool-call krug, sitemap i oba AI providera.
- confirmation tokene, globalnu pretragu, health endpointove, audit i MCP autentikaciju.
- stvarne SQL Server migracije i relacijska unique ograničenja u CI okruženju.

## Changelog wiki

Povijest izdanja organizirana je kao mala wiki baza. Klik na verziju otvara potpuni zapis promjena.

| Verzija | Datum | Najvažnije promjene | Docker |
| --- | --- | --- | --- |
| [v0.37](changelogs/v0.37.md) | 2026-06-17 | Pozadina je pravi OpenStreetMap s animiranim autićima kao overlay | `nikolica/sideseat:v0.37` |
| [v0.36](changelogs/v0.36.md) | 2026-06-17 | Veći kontrast pozadinske karte | `nikolica/sideseat:v0.36` |
| [v0.35](changelogs/v0.35.md) | 2026-06-17 | Pozadina prikazuje okolne države i jače je zumirana | `nikolica/sideseat:v0.35` |
| [v0.34](changelogs/v0.34.md) | 2026-06-17 | Automatski izračun vremena dolaska vožnje te pozadina s granicama država, sporom kamerom i 5 s po ruti | `nikolica/sideseat:v0.34` |
| [v0.33](changelogs/v0.33.md) | 2026-06-17 | Nova pozadinska animacija auta po stvarnim rutama, role-bazirani AI alati s lookupom i kompletna dokumentacija (wiki + arhitektura) | `nikolica/sideseat:v0.33` |
| [v0.32](changelogs/v0.32.md) | 2026-06-17 | Napojnica karticom pri ocjeni, vodič kroz vožnju, skrolabilan sidebar i profesionalna reorganizacija repozitorija | `nikolica/sideseat:v0.32` |
| [v0.31](changelogs/v0.31.md) | 2026-06-16 | AI javna pretraga, scrollabilan sidebar i tablične liste vožnji/rezervacija | `nikolica/sideseat:v0.31` |
| [v0.30](changelogs/v0.30.md) | 2026-06-16 | Stabilan hover bez pomicanja formi i pojačan kontrast dark teme | `nikolica/sideseat:v0.30` |
| [v0.29](changelogs/v0.29.md) | 2026-06-16 | AI/MCP full CRUD pending forme, live ride workflow, gotovina/saldo settlement i pravila kolizije | `nikolica/sideseat:v0.29` |
| [v0.28](changelogs/v0.28.md) | 2026-06-15 | Točne OSRM cestovne rute, brzi fallback, cache i stabilan hover prikaz | `nikolica/sideseat:v0.28` |
| [v0.27](changelogs/v0.27.md) | 2026-06-15 | Redizajnirana početna i beskonačni carousel do 20 ruta s kartom ispod | `nikolica/sideseat:v0.27` |
| [v0.26](changelogs/v0.26.md) | 2026-06-15 | OpenStreetMap rute, geocoding, AI/MCP potvrde i stabilniji produkcijski runtime | `nikolica/sideseat:v0.26` |
| [v0.25](changelogs/v0.25.md) | 2026-06-15 | Dvoredni navbar, role-aware sidebar i mobilna navigacija | `nikolica/sideseat:v0.25` |
| [v0.23](changelogs/v0.23.md) | 2026-06-11 | Ispravno formatirane AI rute i klikabilni detalji | `nikolica/sideseat:v0.23` |
| [v0.22](changelogs/v0.22.md) | 2026-06-11 | AI alati, autorizirani sitemap i trajna petominutna sesija | `nikolica/sideseat:v0.22` |
| [v0.21](changelogs/v0.21.md) | 2026-06-11 | AI provider podrška za Open WebUI i DeepSeek | `nikolica/sideseat:v0.21` |
| [v0.20](changelogs/v0.20.md) | 2026-06-11 | Potpuni AI poslovni kontekst i Docker OpenWebUI konfiguracija | `nikolica/sideseat:v0.20` |
| [v0.19](changelogs/v0.19.md) | 2026-06-11 | AI korisnički kontekst, admin feedback, kartični prikazi i cursor tilt | `nikolica/sideseat:v0.19` |
| [v0.18](changelogs/v0.18.md) | 2026-06-10 | Docker release v0.18 i ažurirani hub tagovi | `nikolica/sideseat:v0.18` |
| [v0.17](changelogs/v0.17.md) | 2026-06-10 | Zeleno-crna tema i optimizirani vizualni efekti | `nikolica/sideseat:v0.17` |
| [v0.16](changelogs/v0.16.md) | 2026-06-10 | Performative AI dizajn i SideSeat Copilot preko Open WebUI-ja | `nikolica/sideseat:v0.16` |
| [v0.14](changelogs/v0.14.md) | 2026-06-08 | Popravljen payment modal, kartice, adresa i prikaz servisa | `nikolica/sideseat:v0.14` |
| [v0.13](changelogs/v0.13.md) | 2026-06-08 | PayPal/Revolut popup, strogi kartični format i verzija u footeru | `nikolica/sideseat:v0.13` |
| [v0.12](changelogs/v0.12.md) | 2026-06-08 | Provjera salda, mock checkout, sigurniji demo podaci i file picker | `nikolica/sideseat:v0.12` |
| [v0.11](changelogs/v0.11.md) | 2026-06-08 | Globalna virtualna karta ruta i bolji dark-mode kontrast | `nikolica/sideseat:v0.11` |
| [v0.10](changelogs/v0.10.md) | 2026-06-08 | Light/dark tema, CSS varijable i statusni brojači | `nikolica/sideseat:v0.10` |
| [v0.9](changelogs/v0.9.md) | 2026-06-08 | Privatnost profila, novi pregledi vožnji i potpuni statusni filteri | `nikolica/sideseat:v0.9` |
| [v0.8](changelogs/v0.8.md) | 2026-06-08 | Obavijesti, upravljanje rezervacijama iz detalja vožnje | `nikolica/sideseat:v0.8` |
| [v0.7](changelogs/v0.7.md) | 2026-06-08 | Potvrđivanje rezervacija, profilne slike i životni ciklus vožnje | `nikolica/sideseat:v0.7` |
| [v0.6](changelogs/v0.6.md) | 2026-06-07 | Pregledi rezervacija prema ulozi i poveznice na vožnje | `nikolica/sideseat:v0.6` |
| [v0.5](changelogs/v0.5.md) | 2026-06-07 | Objedinjeni pregledi vožnji | `nikolica/sideseat:v0.5` |
| [v0.4](changelogs/v0.4.md) | 2026-06-07 | Ispravno posluživanje uploadanih slika u Dockeru | `nikolica/sideseat:v0.4` |
| [v0.3](changelogs/v0.3.md) | 2026-06-07 | Slike recenzija, Google gumb i Docker release workflow | `nikolica/sideseat:v0.3` |

Za objedinjeni indeks vidi [Changelog Wiki](changelogs/README.md).

## Dokumentacija laboratorija

- [Lab 3 dokumentacija](docs/labs/lab3Doc.md)
- [Lab 4 dokumentacija](docs/labs/lab4Doc.md)
- [Lab 5 dokumentacija za predaju](docs/labs/lab5Doc.md)
- [Originalni Lab 5 zadatak](docs/labs/lab-5/Lab5.md)
- [Vodič kroz vožnju](docs/vožnja.md)

Lab 5 pokriva REST API, DTO modele, ASP.NET Core Identity, autorizaciju, Google prijavu, upload i integracijske testove.

## Struktura repozitorija

```text
SideSeat/
├── src/SideSeat/                    # MVC aplikacija
├── tests/SideSeat.IntegrationTests/ # integracijski testovi
├── changelogs/                      # wiki izdanja
├── docs/                            # dokumentacija
│   ├── vožnja.md                    # vodič kroz vožnju
│   └── labs/                        # lab-1 ... lab-5 + labXDoc.md
├── docker-compose.yml               # lokalni build
└── docker-compose.hub.yml           # pokretanje gotovog imagea
```

## Licenca

Projekt je dostupan pod uvjetima iz datoteke [LICENSE](LICENSE).
