#!/usr/bin/env bash
set -euo pipefail

backup="${1:?Usage: scripts/restore-sql.sh /var/opt/mssql/backups/file.bak}"

docker compose -f docker-compose.prod.yml stop sideseat-web
docker compose -f docker-compose.prod.yml exec -T sideseat-db \
  /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${SA_PASSWORD:?SA_PASSWORD is required}" \
  -Q "ALTER DATABASE [SideSeat] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [SideSeat] FROM DISK = N'${backup}' WITH REPLACE; ALTER DATABASE [SideSeat] SET MULTI_USER"
docker compose -f docker-compose.prod.yml start sideseat-web
