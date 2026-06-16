#!/usr/bin/env bash
set -euo pipefail

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup="/var/opt/mssql/backups/SideSeat-${timestamp}.bak"

docker compose -f docker-compose.prod.yml exec -T sideseat-db \
  /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SA_PASSWORD:?SA_PASSWORD is required}" \
  -Q "BACKUP DATABASE [SideSeat] TO DISK = N'${backup}' WITH COPY_ONLY, COMPRESSION, CHECKSUM"

echo "Backup created: ${backup}"
