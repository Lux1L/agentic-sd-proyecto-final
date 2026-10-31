# ADR-002 · PostgreSQL 18 + EF Core 10

- **Estado:** Aceptada
- **Fecha:** 2026-10-30

## Contexto

La rúbrica acepta SQL Server, MySQL o PostgreSQL con EF Core. La plantilla base viene con PostgreSQL 17 (Npgsql). En la máquina de desarrollo ya está descargada la imagen `postgres:18.6`.

## Decisión

- **PostgreSQL 18.6** en `docker-compose.yml` (contenedor `arriendo-postgres`, puerto 5432) y en Testcontainers para las pruebas de integración: la misma versión en desarrollo y en pruebas.
- **EF Core 10 + Npgsql**, migraciones versionadas en `Database/Migrations`, nombres `snake_case`.
- En Development, las migraciones se aplican al iniciar la app (`ApplyMigrations()`) y se cargan datos de demostración.
- **No se reutiliza un contenedor Postgres existente** en la máquina: pertenece a un AppHost de Aspire de otro proyecto, con puerto y contraseña aleatorios que Aspire puede regenerar. El repo debe levantarse solo con `docker compose up -d` (RNF-07).

## Consecuencias

- PostgreSQL 18 cambió la ruta de datos de la imagen: el volumen se monta en `/var/lib/postgresql`, **no** en `/var/lib/postgresql/data` como en la plantilla (con 17 funcionaba, con 18 el contenedor no arranca).
- `DateOnly` se mapea a `date` de forma nativa, lo que permite fechas de negocio sin zona horaria (ver ADR-004).
- Las pruebas de integración necesitan Docker en ejecución.
