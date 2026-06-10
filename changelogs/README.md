# SideSeat Changelog Wiki

Ova mapa sadrži povijest verzija SideSeat aplikacije. Svako izdanje ima zasebnu stranicu s dodanim funkcijama, promjenama, popravcima i Docker podacima.

## Izdanja

| Verzija | Datum | Sažetak |
| --- | --- | --- |
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

Trenutna stabilna verzija je [v0.20](v0.20.md):

```text
nikolica/sideseat:v0.20
```

Docker Hub digest:

```text
sha256:f80a84e538890b61ddb9c2120b53e1e9c545a3908ef6709f0e598f03f2ea88fb
```

## Pravilo verzioniranja

Projekt koristi verzije oblika `v0.x`. Projektni skill `.github/skills/version-control-dockerhub`:

1. predlaže povećanje verzije za `0.1` ili prihvaća ručno unesenu verziju.
2. generira datoteku `changelogs/{verzija}.md`.
3. builda Linux Docker image.
4. objavljuje verzionirani tag i `latest`.
5. provjerava digest i verzijske labele.
