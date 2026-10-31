# ADR-001 · Vertical Slice Architecture en un solo proyecto, con capas lógicas

- **Estado:** Aceptada
- **Fecha:** 2026-10-30

## Contexto

La rúbrica pide "Clean Architecture **o arquitectura en capas justificada**" y muestra como ejemplo cuatro proyectos (Domain, Application, Infrastructure, Api). El proyecto parte de una **plantilla base de Vertical Slice Architecture (VSA) para .NET 10**, adaptada para este dominio, que organiza el código por caso de uso y no por capa técnica, en un solo proyecto `Web.Api`.

El dominio es pequeño (3 entidades, 11 casos de uso) y el plazo es de una noche.

## Decisión

Mantener VSA en un solo proyecto, conservando la **separación de responsabilidades de Clean Architecture como capas lógicas** dentro de cada feature:

| Capa (Clean) | Dónde vive en este repo | Ejemplo |
|---|---|---|
| **Domain**: entidades, reglas y contratos | `Features/{Feature}/` → entidad, `{Entity}Errors`, cálculos puros | `Leases/Lease.cs`, `Leases/LeaseStatement.cs`, `Leases/TenantDocument.cs`, `Leases/LeaseErrors.cs` |
| **Application**: casos de uso, DTOs, validaciones, puertos | `Command`/`Query`, `Validator` y `Handler` anidados en cada slice; `Common/Messaging`, `Common/Behaviors` | `Leases/RecordPayment.cs` → `Command`, `Validator`, `Handler` |
| **Infrastructure**: EF Core y servicios externos | `Database/` (DbContext, configuraciones, migraciones), `Authentication/`, `Notifications/` | `Database/Configurations/LeaseConfiguration.cs` |
| **API**: endpoints, OpenAPI y seguridad | `Endpoint : IEndpoint` anidado en cada slice; `Program.cs`, `Common/Endpoints`, `Authorization/` | `RecordPayment.Endpoint` → `POST /leases/{id}/payments` |

Las reglas que en Clean Architecture garantizan los proyectos separados aquí se **verifican con pruebas de arquitectura** (NetArchTest):

- Los handlers y los endpoints son `sealed` (plantilla).
- **Aislamiento de features:** un slice no puede depender del `Command`/`Handler`/`Endpoint` de otra feature; solo de sus tipos de dominio (`FeatureIsolationTests`).
- Las reglas de dominio puras (`LeaseStatement`, `TenantDocument`, `LimaClock`) no dependen de EF Core ni de ASP.NET.

## Consecuencias

**A favor**
- Un cambio de negocio (p. ej. RN-14) se lee y se revisa en **un solo archivo**: el validador, el handler y el endpoint del caso de uso. Esto facilita la trazabilidad RF → código → prueba.
- Menos ceremonia: sin repositorios ni interfaces que solo reenvían llamadas, y sin mappers entre capas.
- Las skills de Claude Code del repo (`/add-feature`, `/add-entity`, …) generan código con esta forma, lo que reduce el riesgo de que la IA invente estructura.

**En contra / riesgos**
- El compilador no impide que el dominio referencie EF Core: lo cuidan las pruebas de arquitectura y la revisión (`/vsa-review`).
- Los handlers usan `ApplicationDbContext` directamente: cambiar de ORM tocaría cada slice. Es aceptable para el tamaño del sistema.

## Alternativa descartada

**Cuatro proyectos (Domain/Application/Infrastructure/Api).** Calza con el árbol de la rúbrica, pero costaba entre 1 y 2 horas de refactor sin ninguna funcionalidad nueva, y rompía las convenciones de las skills. Si el sistema crece (multi-tenant, portal del arrendatario), se puede extraer `Domain` a su propio proyecto sin reescribir los slices.
