#!/usr/bin/env bash
# Deja la base local de desarrollo en el estado de demo: borra SOLO ./.containers/db
# (la base de este repo) y vuelve a levantar Postgres. Al arrancar, la API en Development
# aplica las migraciones y carga los datos de demo (DevelopmentSeeder).
set -euo pipefail
cd "$(dirname "$0")/.."

if pgrep -f "dotnet.*src/Web.Api" >/dev/null; then
  echo "Detén la API (dotnet run) antes de resetear la base." >&2
  exit 1
fi

docker compose down
# macOS puede recrear un .DS_Store mientras se borra la carpeta: reintentar.
for _ in 1 2 3; do
  rm -rf ./.containers/db 2>/dev/null && break
  sleep 1
done
if [ -e ./.containers/db ]; then
  echo "No se pudo borrar ./.containers/db" >&2
  exit 1
fi
docker compose up -d postgres

echo "Base reseteada. Ahora: dotnet run --project src/Web.Api  (carga los datos de demo)"
