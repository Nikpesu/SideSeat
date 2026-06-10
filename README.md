# SideSeat

> ASP.NET Core MVC ridesharing aplikacija izrađena za kolegij **Razvoj web aplikacija**.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](src/SideSeat/SideSeat.csproj)
[![.NET CI](https://github.com/Nikpesu/SideSeat/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/Nikpesu/SideSeat/actions/workflows/dotnet-ci.yml)
[![Docker](https://img.shields.io/badge/Docker-Linux%20AMD64-2496ED?logo=docker&logoColor=white)](docker-compose.hub.yml)
[![Version](https://img.shields.io/badge/version-v0.20-2ea44f)](changelogs/v0.20.md)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

SideSeat povezuje vozače i putnike kroz objavu vožnji, rezervacije, potvrđivanje putnika, plaćanja, ocjene i obavijesti. Projekt uključuje ASP.NET Core Identity, Google prijavu, REST API, upload slika, Open WebUI AI asistenta, SQL Server, Docker i integracijske testove.

## Sadržaj

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
nikolica/sideseat:v0.20
```

Tag `latest` pokazuje na isto izdanje. Image je namijenjen platformi `linux/amd64`.

```bash
docker pull nikolica/sideseat:v0.20
```

Digest izdanja:

```text
sha256:f80a84e538890b61ddb9c2120b53e1e9c545a3908ef6709f0e598f03f2ea88fb
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
OPENWEBUI_API_KEY=
OPENWEBUI_MODEL=
OPENWEBUI_BASE_URL=https://ai.pesut.win
```

Verzija, `ASPNETCORE_ENVIRONMENT` i `ASPNETCORE_URLS` ugrađeni su u Docker image. Aplikacija u Dockeru automatski sastavlja connection string za `sideseat-db`; preko `.env` mijenja se samo `SA_PASSWORD`. Napredni overrideovi ostavljeni su zakomentirani u Compose datotekama.

`DUMMY_DATA` je zadano `false`. Za Docker demonstraciju sa seedanim testnim podacima postavi:

```dotenv
DUMMY_DATA=true
```

SideSeat AI koristi Open WebUI na `https://ai.pesut.win`. API ključ ostaje samo u Docker environmentu i nikada se ne šalje pregledniku:

```dotenv
OPENWEBUI_API_KEY=upiši_svoj_open_webui_api_ključ
OPENWEBUI_MODEL=
OPENWEBUI_BASE_URL=https://ai.pesut.win
```

`OPENWEBUI_MODEL` nije obavezan. Kada je prazan, aplikacija preko `/api/models` automatski odabire prvi model dostupan tom API ključu. `OPENWEBUI_BASE_URL` aplikacija učitava iz Docker Compose okruženja. Za lokalno pokretanje bez Dockera koristi user-secrets:

```powershell
dotnet user-secrets set "OpenWebUi:ApiKey" "API_KEY" --project src/SideSeat
dotnet user-secrets set "OpenWebUi:Model" "MODEL_ID" --project src/SideSeat
dotnet user-secrets set "OpenWebUi:BaseUrl" "https://ai.pesut.win" --project src/SideSeat
```

AI zahtjevi idu preko `/api/ai/chat`, ograničeni su rate limiterom i ne izlažu Open WebUI ključ klijentskom JavaScriptu.

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

## Testovi

GitHub Actions automatski pokreće Release build i sve integracijske testove na svaki push i pull request prema grani `main`. TRX rezultat i code coverage spremaju se kao `sideseat-test-results` artefakt.

```bash
dotnet test tests/SideSeat.IntegrationTests/SideSeat.IntegrationTests.csproj
```

Trenutni rezultat: **23/23 integracijskih testova prolazi**.

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

## Changelog wiki

Povijest izdanja organizirana je kao mala wiki baza. Klik na verziju otvara potpuni zapis promjena.

| Verzija | Datum | Najvažnije promjene | Docker |
| --- | --- | --- | --- |
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

- [Lab 3 dokumentacija](lab3Doc.md)
- [Lab 4 dokumentacija](lab4Doc.md)
- [Lab 5 dokumentacija za predaju](lab5Doc.md)
- [Originalni Lab 5 zadatak](lab-5/Lab5.md)

Lab 5 pokriva REST API, DTO modele, ASP.NET Core Identity, autorizaciju, Google prijavu, upload i integracijske testove.

## Struktura repozitorija

```text
SideSeat/
├── src/SideSeat/                    # MVC aplikacija
├── tests/SideSeat.IntegrationTests/ # integracijski testovi
├── changelogs/                      # wiki izdanja
├── lab-1/ ... lab-5/                # zadaci i materijali
├── docker-compose.yml               # lokalni build
├── docker-compose.hub.yml           # pokretanje gotovog imagea
└── lab5Doc.md                       # dokumentacija za predaju
```

## Licenca

Projekt je dostupan pod uvjetima iz datoteke [LICENSE](LICENSE).
