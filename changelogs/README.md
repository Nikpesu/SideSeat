# SideSeat Changelog Wiki

Ova mapa sadrži povijest verzija SideSeat aplikacije. Svako izdanje ima zasebnu stranicu s dodanim funkcijama, promjenama, popravcima i Docker podacima.

## Izdanja

| Verzija | Datum | Sažetak |
| --- | --- | --- |
| [v0.34](v0.34.md) | 2026-06-17 | Automatski izračun vremena dolaska vožnje te pozadina s granicama država, sporom kamerom i 5 s po ruti |
| [v0.33](v0.33.md) | 2026-06-17 | Nova pozadinska animacija auta po stvarnim rutama, role-bazirani AI alati s lookupom i kompletna dokumentacija |
| [v0.32](v0.32.md) | 2026-06-17 | Napojnica karticom pri ocjeni, vodič kroz vožnju, skrolabilan sidebar i profesionalna reorganizacija repozitorija |
| [v0.31](v0.31.md) | 2026-06-16 | AI javna pretraga, scrollabilan sidebar i tablične liste vožnji/rezervacija |
| [v0.30](v0.30.md) | 2026-06-16 | Stabilan hover bez pomicanja formi i pojačan kontrast dark teme |
| [v0.29](v0.29.md) | 2026-06-16 | AI/MCP full CRUD pending forme, live ride workflow i settlement plaćanja |
| [v0.28](v0.28.md) | 2026-06-15 | Točne OSRM cestovne rute, brzi fallback i stabilan hover prikaz |
| [v0.27](v0.27.md) | 2026-06-15 | Redizajnirana početna i carousel ruta s kartom |
| [v0.26](v0.26.md) | 2026-06-15 | OpenStreetMap rute, geocoding i MCP produkcijska konfiguracija |
| [v0.25](v0.25.md) | 2026-06-15 | Dvoredni navbar, role-aware sidebar i mobilna navigacija |
| [v0.23](v0.23.md) | 2026-06-11 | Ispravno formatirane AI rute i klikabilni detalji |
| [v0.22](v0.22.md) | 2026-06-11 | AI alati, autorizirani sitemap i trajna petominutna sesija |
| [v0.21](v0.21.md) | 2026-06-11 | AI provider podrška za Open WebUI i DeepSeek |
| [v0.20](v0.20.md) | 2026-06-11 | Potpuni AI poslovni kontekst i OpenWebUI konfiguracija kroz Docker |
| [v0.19](v0.19.md) | 2026-06-11 | AI kontekst, admin feedback i novi kartični prikaz podataka |
| [v0.18](v0.18.md) | 2026-06-10 | Docker release v0.18 i ažurirani hub tagovi |
| [v0.17](v0.17.md) | 2026-06-10 | Zeleno-crna tema i optimizirani vizualni efekti |
| [v0.16](v0.16.md) | 2026-06-10 | Performative AI dizajn i SideSeat Copilot preko Open WebUI-ja |
| [v0.14](v0.14.md) | 2026-06-08 | Popravljen payment modal, kartice i segmentirana adresa |
| [v0.13](v0.13.md) | 2026-06-08 | Dovršen mock payment tok i verzija u footeru |
| [v0.12](v0.12.md) | 2026-06-08 | Provjera salda, mock checkout i sigurniji Docker demo podaci |
| [v0.11](v0.11.md) | 2026-06-08 | Globalna animirana karta ruta i bolji kontrast notifikacija |
| [v0.10](v0.10.md) | 2026-06-08 | Light/dark tema, centralizirane boje i statusni brojači |
| [v0.9](v0.9.md) | 2026-06-08 | Privatnost korisničkih profila, pregledi vožnji i statusni filteri |
| [v0.8](v0.8.md) | 2026-06-08 | Sustav obavijesti i upravljanje rezervacijama iz detalja vožnje |
| [v0.7](v0.7.md) | 2026-06-08 | Potvrđivanje rezervacija, profilne slike i statusi |
| [v0.6](v0.6.md) | 2026-06-07 | Pregledi rezervacija prema ulozi |
| [v0.5](v0.5.md) | 2026-06-07 | Objedinjeni pregledi vožnji |
| [v0.4](v0.4.md) | 2026-06-07 | Docker posluživanje uploadanih slika |
| [v0.3](v0.3.md) | 2026-06-07 | Slike recenzija, Google prijava i release workflow |

## Trenutna verzija

Trenutna stabilna verzija je [v0.34](v0.34.md):

```text
nikolica/sideseat:v0.34
```

Docker Hub digest:

```text
sha256:8d25e3f3abcb731b53217a108b0c5cae964c8443b484babaf268e75468ee2e02
```

## Pravilo verzioniranja

Projekt koristi verzije oblika `v0.x`. Projektni skill `.github/skills/version-control-dockerhub`:

1. predlaže povećanje verzije za `0.1` ili prihvaća ručno unesenu verziju.
2. generira datoteku `changelogs/{verzija}.md`.
3. builda Linux Docker image.
4. objavljuje verzionirani tag i `latest`.
5. provjerava digest i verzijske labele.
