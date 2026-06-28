# Postavljanje (Deployment)

[← Wiki](Home.md)

## Docker Compose

- **`docker-compose.hub.yml`** — pokreće gotov image s Docker Huba + SQL Server.
- **`docker-compose.yml`** — lokalni build iz izvora (`src/SideSeat/Dockerfile`).
- **`docker-compose.prod.yml`** — produkcijska varijanta (image preko `SIDESEAT_IMAGE`).

```bash
cp .env.example .env
docker compose -f docker-compose.hub.yml up -d
# aplikacija: http://localhost:8080
```

Trajni podaci: `sideseat-sql-data` (baza) i `sideseat-uploads` (uploadane slike).

## Docker image

- Repozitorij: `nikolica/sideseat`
- Tagovi: `:vX.Y` (verzionirani) i `:latest`
- Platforma: `linux/amd64`
- Dockerfile: `src/SideSeat/Dockerfile`, build context = korijen repozitorija

```bash
docker pull nikolica/sideseat:latest
```

## Izdavanje nove verzije (Docker Hub)

Projekt ima skill `.github/skills/version-control-dockerhub` koji:
1. predloži povećanje verzije za `0.1` (ili ručni unos),
2. generira `changelogs/{verzija}.md`,
3. ažurira reference verzije (compose, Dockerfile, layout),
4. buildira Linux image i objavi verzionirani tag + `latest`,
5. provjeri remote digest.

Vidi i [changelog](../../changelogs/README.md).

## CI
`.github/workflows/dotnet-ci.yml` pokreće build i testove na svaki push/PR.
