#!/usr/bin/env bash
# Deja la base local de desarrollo en el estado de demo: borra SOLO el volumen pgdata
# de este proyecto y vuelve a levantar Postgres. Al arrancar, la API en Development
# aplica las migraciones y carga los datos de demo (DevelopmentSeeder).
#   ./scripts/reset-demo.sh        → solo Postgres (después: dotnet run + pnpm dev)
#   ./scripts/reset-demo.sh app    → todo en Docker (Postgres + API + frontend)
set -euo pipefail
cd "$(dirname "$0")/.."

if pgrep -f "dotnet.*src/Web.Api" >/dev/null; then
  echo "Detén la API (dotnet run) antes de resetear la base." >&2
  exit 1
fi

docker compose --profile full down --volumes

if [ "${1:-}" = "app" ]; then
  docker compose --profile app up -d --build --wait
  echo "Listo: http://localhost:5173 (API en http://localhost:5080/scalar/v1)"
else
  docker compose up -d --wait postgres
  echo "Base reseteada. Ahora: dotnet run --project src/Web.Api  (carga los datos de demo)"
fi
