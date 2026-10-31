# Arriendo Familiar — notas para Claude Code

Sistema de administración de alquileres para una familia en **Perú** (soles S/, DNI/CE/RUC, hora de Lima). API REST .NET 10 con Vertical Slice Architecture. Antes de implementar, lee las specs: `specs/functional/` (RF, RN, HU, CU) y `specs/technical/` (modelo, contrato de la API, trazabilidad). Las decisiones de arquitectura están en `docs/adr/`.

## Reglas del proyecto (además de las convenciones de abajo)

- **Spec-first:** todo cambio de negocio debe corresponder a un RF/RN de `specs/`. Si la spec no lo cubre, se actualiza primero la spec.
- Código en **inglés** (`Property`, `Lease`, `Payment`); specs, docs, mensajes de error y UI en **español**. El glosario está en `specs/functional/01-vision-y-alcance.md`.
- **"Hoy" siempre es `dateTimeProvider.Today()`** (`Common/LimaClock.cs`, UTC−5). Nunca `DateTime.Now`, ni `UtcNow` para fechas de negocio.
- Fechas de negocio: `DateOnly`. Dinero: `decimal` con precisión (12,2).
- Errores de negocio: `Problem` (400) o `Conflict` (409), **nunca `Error.Failure`** (devuelve 500). Códigos `{FeaturePlural}.{Motivo}`, descripciones en español.
- Endpoints de negocio: `.RequireAuthorization()` (un solo actor, el administrador familiar; `PermissionProvider` no asigna permisos).
- Encima de cada test, un comentario `// RN-xx` con la regla que cubre (alimenta `specs/technical/03-trazabilidad.md`).
- Commits en español, conventional, con el prefijo del RF: `feat(RF-04): ...`.
- Los `.cs` de la plantilla usan **CRLF**: edítalos con Edit, nunca los reescribas desde un script.

### Excepciones deliberadas a la skill `/vsa-review`

- **Sin eventos de dominio (`Raise`) en los commands.** Ninguna spec los pide y no hay handlers que los consuman. Se agregan cuando exista un consumidor real (por ejemplo, los recordatorios de pago, una mejora pendiente).
- **Los errores de validación usan como código el nombre de la propiedad**, no `{Feature}.{Motivo}`, porque `CustomResults` los agrupa por campo (`{ campo: [mensajes] }`, RNF-04). Los errores de negocio sí usan `{Feature}.{Motivo}`.

## Estructura (un solo proyecto)

Todo vive en `src/Web.Api` (namespace raíz `Web.Api`). **No** hay proyectos Domain/Application/Infrastructure separados. Ver ADR-001 para el mapeo a capas lógicas.

- **`Features/{Entity}/`**: una carpeta por feature (`Properties`, `Leases`, `Portfolio`, `Users`). **Cada caso de uso es un archivo**: `public static class {UseCase}` que anida `Command`/`Query`, `Validator` (solo commands), un `internal sealed Handler` y un `Endpoint : IEndpoint`. La entidad, su catálogo `{Entity}Errors`, los enums y las funciones puras de dominio (`LeaseStatement`, `TenantDocument`, `Period`) viven en la misma carpeta.
- **`Common/`**: `Result`, `Error`, `Entity`, `IDateTimeProvider`, `LimaClock`, mensajería CQRS, decoradores, endpoints, `PagedList<T>`, extensiones.
- **`Database/`**: `ApplicationDbContext`, configuraciones en `Database/Configurations/`, migraciones en `Database/Migrations/` (marcadas como código generado en `.editorconfig`).
- **`Authentication/`** / **`Authorization/`**: JWT con refresh tokens.
- **`Notifications/`**: `IEmailSender` + `LoggingEmailSender`.

## Convenciones

- **CQRS sin MediatR**: `ICommand`/`ICommand<T>` + `ICommandHandler<...>`, `IQuery<T>` + `IQueryHandler<Q,R>`. Los handlers son `internal sealed` con constructor primario y se registran solos con Scrutor (`TryDecorate` con validación + logging). Nunca se registra un slice a mano.
- **Sin acoplamiento entre features:** un slice puede usar `Common`/`Database`/`Authentication` y los **tipos de dominio** de otra feature (entidad, errores, enums, funciones puras), pero nunca el `Command`/`Query`/`Handler`/`Validator`/`Endpoint` de otra.
- **Errores, no excepciones:** `Result.Failure(...)` y en el endpoint `result.Match(Results.Ok, CustomResults.Problem)` (o `Results.NoContent`).
- **Validación:** FluentValidation, `Validator` anidado; se ejecuta en el decorador (solo commands).
- **Datos:** se inyecta el `ApplicationDbContext` concreto. Las relaciones son FK sin navegaciones.
- **Listados:** `PagedList<T>` y proyección a un `Response` anidado con `.Select(...)`. Nunca se devuelven entidades.

## Build y pruebas

- `docker compose up -d` (PostgreSQL 18.6 en el puerto 5432) y `dotnet run --project src/Web.Api` → http://localhost:5080/scalar/v1
- `dotnet build ArriendoFamiliar.slnx`
- `dotnet test ArriendoFamiliar.slnx`: las pruebas de integración usan Testcontainers, así que Docker debe estar corriendo.
- **Analizadores estrictos:** `TreatWarningsAsErrors` + SonarAnalyzer. Usar `CultureInfo.InvariantCulture`, colecciones con `[.. x]` y `ToUpperInvariant`.

## Frontend (`web/`)

Vite + React + TypeScript + **Kumo** (`@cloudflare/kumo`) + Tailwind v4, con **pnpm**. `pnpm dev` levanta http://localhost:5173 (proxy `/api` → API en 5080). Reglas duras, todas salidas de defectos reales (ver `docs/ai-evidence/`, sección E2E):

- **"Hoy" = `todayIso()`** (`web/src/format.ts`, hora de Lima). Nunca `new Date()` para fechas de negocio ni `toISOString()`. La API valida con America/Lima.
- **Siempre componentes de Kumo** antes que HTML o estilos propios (`npx @cloudflare/kumo doc <Componente>`). Fechas con `DateField`, nunca `type="date"`/`"month"`. Cada `Select` lleva `items` para no mostrar valores crudos de enums.
- **Errores**: `api()` + `useErrorHandler`. El negocio va en toast (`title`/`detail`) y la validación debajo del campo, según la **clave** de `errors`. Nunca inferir el campo del texto del mensaje.
- Diseño: skill `kumo-design`. Excepción consciente: los KPI de montos en `text-3xl`.
- Cada pantalla tiene su spec E2E (Playwright sobre Google Chrome, `cd web && pnpm e2e`), en verde 2 veces seguidas. Después: `./scripts/reset-demo.sh`.

## Skills

Backend: `/add-feature` (caso de uso) · `/add-entity` · `/add-tests` · `/add-migration` · `/add-background-job` · `/bootstrap` · `/vsa-review` (revisión de convenciones).
Frontend: `/add-page` (pantalla, formulario o dialog) · `/add-e2e` (pruebas E2E en Chrome) · `kumo-design` (reglas visuales).
