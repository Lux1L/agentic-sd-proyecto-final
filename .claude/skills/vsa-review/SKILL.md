---
name: vsa-review
description: Revisa los cambios pendientes contra las convenciones de la plantilla de Vertical Slice Architecture — slices de un solo archivo, aislamiento entre slices, manejo de errores basado en Result, acceso directo al DbContext, validación, endpoints y cobertura de tests. Úsala cuando el usuario pida revisar cambios, verificar convenciones o auditar una feature antes de hacer commit.
argument-hint: "[opcional: archivos o feature específicos a revisar; por defecto, el diff del working tree]"
---

# Revisión de convenciones de Vertical Slice Architecture

Revisa el alcance indicado (por defecto: `git diff` + archivos sin seguimiento) contra las convenciones de esta plantilla. Reporta los hallazgos con referencias archivo:línea, ordenados por severidad. No corrijas nada a menos que te lo pidan.

## Checklist

### Estructura del slice (las violaciones son bloqueantes)
- Un caso de uso = un archivo: `src/Web.Api/Features/{Entity}/{UseCase}.cs`, namespace `Web.Api.Features.{Entity}`, que contiene una `public static class {UseCase}`.
- El slice está completamente anidado: `Command`/`Query`, `Validator` (solo commands), `Handler` y `Endpoint` están todos anidados dentro de esa clase estática. Sin proyecto Application/Domain/Infrastructure aparte, sin carpeta `Endpoints/` aparte.
- `Handler` es `internal sealed` con constructor primario e implementa el `ICommandHandler<>` / `IQueryHandler<>` propio (de `Web.Api.Common.Messaging`); sin MediatR.
- Los DTO de respuesta son el `Response` anidado del slice; las queries proyectan con `.Select(...)` y nunca devuelven entidades de dominio.

### Aislamiento entre slices (bloqueantes)
- **Sin referencias entre slices de distintas features.** Una feature puede referenciar el código compartido de `Common`/`Database`/`Authentication`/`Authorization` y los **tipos de dominio** de otra feature (entidad, `{Entity}Errors`, eventos de dominio). **Nunca** debe referenciar el `Command`/`Query`/`Handler`/`Validator`/`Endpoint` de otra feature.
- Sin registro manual en DI de handlers/validators/endpoints: se descubren mediante el escaneo de Scrutor, `AddValidatorsFromAssembly(includeInternalTypes: true)` y `AddEndpoints`. Señala cualquier cosa que registre un slice a mano.

### Acceso a datos
- Los handlers inyectan directamente el `ApplicationDbContext` concreto (de `Web.Api.Database`). **No existe `IApplicationDbContext`**: señala cualquier referencia a él.
- Los detalles de persistencia (claves, conversiones, relaciones) viven en clases `IEntityTypeConfiguration<>` dentro de `Database/Configurations/`, no en las entidades. Las entidades son tipos de dominio simples en `Features/{Entity}/`.

### Manejo de errores
- Los fallos esperados devuelven `Result` / `Result<T>` (de `Web.Api.Common`); sin excepciones para controlar el flujo, sin try/catch alrededor de reglas de negocio.
- Los errores provienen de factories estáticas `{Entity}Errors` con códigos `"{Feature}.{Reason}"` y el tipo semánticamente correcto (`NotFound` / `Conflict` / `Problem` / `Failure`).
- Los endpoints traducen los fallos únicamente mediante `result.Match(Results.Ok|NoContent, CustomResults.Problem)`.

### Validación y seguridad
- Cada command tiene un `Validator` de FluentValidation anidado; los handlers no vuelven a verificar la forma de la entrada (pero sí aplican las reglas de negocio). Las queries no tienen validator.
- Los handlers que actúan sobre datos propiedad del usuario verifican la propiedad: filtran por `IUserContext.UserId` o devuelven `UserErrors.Unauthorized()`.
- Los endpoints implementan `IEndpoint`, llaman a `.RequireAuthorization()` (o `.HasPermission(...)`) y `.WithTags(Tags.X)`, y solo contienen el mapeo request→command más el match del resultado; sin lógica de negocio.
- Sin `DateTime.UtcNow` / `DateTime.Now` en los handlers: usa `IDateTimeProvider`.

### Cambios de estado y cache
- Los commands que modifican el estado emiten un evento de dominio mediante `entity.Raise(new XDomainEvent(id))` antes de `SaveChangesAsync`.
- Toda lectura cacheada con `HybridCache` tiene su invalidación correspondiente (`cache.RemoveAsync`) en cada command que modifica esos datos; las claves provienen de la clase `{Feature}CacheKeys`.

### Tests
- Los handlers nuevos o modificados tienen tests unitarios que cubren cada camino `Result.Failure` más el camino feliz (estado persistido + eventos de dominio), usando los tipos anidados (`new {UseCase}.Handler(...)`).
- Los validators nuevos o modificados tienen tests con `TestValidate` por cada regla.
- Los endpoints nuevos o modificados tienen tests de integración sobre HTTP real.
- Los nombres de los tests y la estructura Arrange/Act/Assert coinciden con los tests existentes.

## Formato de salida

Agrupa los hallazgos en **Bloqueantes** (referencias entre slices de distintas features, uso de `IApplicationDbContext`, falta de auth, excepciones lanzadas para fallos esperados), **Violaciones de convención** (estructura de archivos/anidamiento, nombres, códigos de error, eventos/invalidaciones faltantes) y **Brechas de tests**. Para cada uno: `file:line`, qué está mal y la corrección en una línea. Cierra con un veredicto: listo para commit, o qué debe cambiar primero. Si todo pasa, dilo y ejecuta `dotnet build` + `dotnet test` para confirmarlo.
