# Arriendo Familiar

Sistema para administrar los alquileres de una familia en Perú: propiedades, contratos, pagos con abonos parciales y **estado de cuenta con mora calculada automáticamente**.

Es el proyecto integrador del curso *Agentic Software Development .NET 10*: se construyó **spec-first** con Claude Code y se documentó la evidencia del uso de IA.

## El problema, en una línea

La familia que alquila 2 a 6 unidades lleva los contratos en papel y los pagos en el chat: no sabe al instante quién está atrasado ni cuánto se le debe. → [Visión y alcance](specs/functional/01-vision-y-alcance.md)

## Stack

| Capa | Tecnología |
|---|---|
| API | ASP.NET Core **.NET 10**, Minimal APIs, Vertical Slice Architecture ([ADR-001](docs/adr/ADR-001-vertical-slice-en-un-proyecto.md)) |
| Datos | **PostgreSQL 18.6** + EF Core 10 (Npgsql), migraciones versionadas ([ADR-002](docs/adr/ADR-002-postgresql-18-ef-core.md)) |
| Validación y errores | FluentValidation, `Result` + `ProblemDetails` ([ADR-003](docs/adr/ADR-003-result-en-vez-de-excepciones.md)) |
| Seguridad | JWT con refresh tokens y rate limiting en la autenticación |
| Documentación de la API | OpenAPI + Scalar (`/scalar/v1`) |
| Pruebas | xUnit, Shouldly, NSubstitute, Testcontainers (PostgreSQL real), NetArchTest |
| Frontend | Vite + React + TypeScript + Tailwind v4 + **Kumo** (`@cloudflare/kumo`) en `web/` ([ADR-005](docs/adr/ADR-005-frontend-kumo.md)) |

## Cómo ejecutarlo

Requisitos: .NET SDK 10, Docker y Node 20+ (para el frontend).

```bash
# 1. Base de datos (PostgreSQL 18.6 en el puerto 5432)
docker compose up -d

# 2. API: aplica las migraciones y carga los datos de demo en Development
dotnet run --project src/Web.Api
#    → http://localhost:5080/scalar/v1
```

Opcional: `docker compose --profile observability up -d` levanta Grafana/Loki/Tempo en http://localhost:3000.

### Credenciales de prueba (solo Development)

| Usuario | Contraseña |
|---|---|
| `admin@arriendo.pe` | `Demo1234!` |

Los datos de demo incluyen 4 propiedades: una al día, una con abono parcial, una atrasada varios meses y una disponible para crear un contrato en vivo. Las fechas se calculan desde la fecha de hoy, así que la mora siempre se ve.

> El secreto JWT de `appsettings.Development.json` es un valor de ejemplo solo para desarrollo local. No hay secretos reales en el repo.

## Pruebas

```bash
dotnet test ArriendoFamiliar.slnx   # requiere Docker en ejecución (Testcontainers)
cd web && pnpm e2e                  # E2E Playwright; requiere API (5080) + Vite (5173) corriendo; usa Google Chrome instalado
```

- **Unitarias**: cada regla de negocio (RN-xx) tiene su prueba, con un comentario `// RN-xx`.
- **Integración**: cada endpoint contra PostgreSQL 18.6 real.
- **Arquitectura**: aislamiento entre features y pureza de las funciones de dominio.

## Estructura del repositorio

```
├── src/Web.Api/                 API (.NET 10)
│   ├── Features/                Un folder por feature, un archivo por caso de uso
│   │   ├── Properties/          RF-01
│   │   ├── Leases/              RF-02..06 (contratos, pagos, estado de cuenta)
│   │   ├── Portfolio/           RF-07
│   │   └── Users/               Autenticación (plantilla base)
│   ├── Common/                  Result, errores, CQRS, LimaClock, endpoints
│   └── Database/                DbContext, configuraciones, migraciones, datos de demo
├── tests/                       Unit, Integration, Architecture
├── specs/
│   ├── functional/              Visión, RF/RN, historias (Gherkin), casos de uso
│   └── technical/               Modelo de datos, contrato de la API, trazabilidad
├── docs/
│   ├── adr/                     Decisiones de arquitectura
│   └── ai-evidence/             Plan, prompts y registro de revisión humana
├── web/                         Frontend (Vite + React + Kumo)
├── .claude/skills/              Skills de Claude Code con las convenciones del repo
└── docker-compose.yml
```

## Especificaciones y trazabilidad

- [Requerimientos y reglas de negocio](specs/functional/02-requerimientos.md): RF-01..07, RN-01..18, RNF
- [Historias de usuario](specs/functional/03-historias-de-usuario.md) · [Casos de uso](specs/functional/04-casos-de-uso.md)
- [Modelo de datos](specs/technical/01-modelo-de-datos.md) · [Contrato de la API](specs/technical/02-contrato-api.md)
- [**Matriz de trazabilidad**](specs/technical/03-trazabilidad.md): RF → HU → CU → RN → slice → endpoint → prueba

## Presentación y demo

- Deck: Slides artifact privado (13 láminas, se exporta a .pptx/PDF), compartido por el autor.
- [Guion de la demo y cómo resetear los datos](docs/guion-demo.md)

## Evidencia de IA

Se usó **Claude Code**: Opus como orquestador y agentes Sonnet como ejecutores en paralelo, con revisión de cada diff. → [docs/ai-evidence](docs/ai-evidence/README.md)

- [Plan aprobado](docs/ai-evidence/01-plan-aprobado.md)
- [Decisiones: aceptadas, corregidas y rechazadas](docs/ai-evidence/02-decisiones-y-revision.md)
- [Prompts clave](docs/ai-evidence/03-prompts-clave.md)

## Mejoras pendientes

Bloqueo optimista en el registro de pagos concurrentes · reajuste anual del alquiler · recordatorios por email/WhatsApp · portal del arrendatario y roles · adelanto y devolución de garantía · prorrateo de meses incompletos · adjuntar el contrato en PDF.

## Origen

Se partió de una plantilla base de Vertical Slice Architecture para .NET 10, adaptada para este dominio (ver ADR-001).
