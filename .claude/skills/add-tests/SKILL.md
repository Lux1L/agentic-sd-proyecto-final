---
name: add-tests
description: Completa los tests faltantes de casos de uso existentes en la plantilla de Vertical Slice Architecture — tests unitarios de handlers, tests de validators de FluentValidation y tests de integración HTTP. Úsala cuando el usuario pida agregar, mejorar o completar la cobertura de tests.
argument-hint: <caso de uso o feature a cubrir, p. ej. "CreateTodo" o "la feature Users">
---

# Agregar tests a un caso de uso existente

Completa los tres tipos de tests que esta plantilla espera para cada slice. Primero lee el archivo del slice objetivo (`Features/{Entity}/{UseCase}.cs`): su `Command`/`Query`, `Handler`, `Validator` y `Endpoint` anidados. Luego replica la estructura de la clase de tests existente más parecida.

## Flujo de trabajo

1. **Ubica el slice.** Abre `src/Web.Api/Features/{Entity}/{UseCase}.cs`. Lista cada resultado distinto del `Handler` anidado: cada cláusula de guarda (`return Result.Failure(...)`) y el camino feliz.
2. **Revisa lo que ya existe** en `tests/Web.Api.UnitTests/{Feature}/` y `tests/IntegrationTests/{Feature}/`: extiende las clases existentes, no las dupliques.
3. **Escribe los tests unitarios del handler**: un test por cada camino de fallo más un camino feliz que verifique el estado persistido y los eventos de dominio emitidos.
4. **Escribe los tests del validator** (solo para commands): un test que falle por cada regla más un command completamente válido.
5. **Escribe los tests de integración**: el camino feliz sobre HTTP real, verificando el estado con un GET posterior; la traducción de errores (404/409/400) cuando el handler tenga caminos de fallo; 401 sin token si la familia de rutas es nueva.
6. **Ejecuta** `dotnet test` (Docker debe estar corriendo para los tests de integración) y corrige los fallos antes de terminar.

## Convenciones

- **Frameworks:** xUnit + Shouldly + NSubstitute; `FluentValidation.TestHelper` para los validators. Los global usings ya cubren `Xunit`, `NSubstitute`, `Shouldly`. Agrega `using Web.Api.Common;` para `Result`/`Error` y `using Web.Api.Features.{Entity};` para el slice, la entidad, los errores y los eventos de dominio.
- **Referencia los miembros del slice de forma calificada:** `CreateTodo.Command`, `new CreateTodo.Handler(...)`, `new CreateTodo.Validator()`; están anidados en la `public static class {UseCase}`.
- **Base de tests unitarios:** hereda de `BaseHandlerTest`; usa `CreateDbContext()` para obtener un `ApplicationDbContext` (de `Web.Api.Database`) nuevo en memoria y `CreateCache()` para un `HybridCache` real. No existe `IApplicationDbContext`/`TestDbContext`: el handler recibe el `ApplicationDbContext` concreto, así que úsalo directamente. Sustituye solo interfaces (`IUserContext`, `IDateTimeProvider`, `IPasswordHasher`, `ITokenProvider`); nunca hagas mock del DbContext.
- **Base de tests de integración:** hereda de `BaseIntegrationTest(factory)` con el collection fixture `IntegrationTestWebAppFactory`; usa `RegisterAndLoginAsync()` + `Authenticate(token)` para las llamadas autenticadas. Define records privados de DTO de respuesta dentro de la clase de test (ver `TodosTests.TodoDto`); los tests de integración pasan por HTTP y no referencian los tipos del slice.
- **Nombres:** clases `{UseCase}HandlerTests` / `{Feature}ValidatorsTests` / `{Feature}Tests`; métodos `Handle_Should_{Outcome}_When{Condition}` (unitarios) o `{Action}_Should_{Outcome}[_When{Condition}]` (integración).
- **Estructura:** comentarios `// Arrange` / `// Act` / `// Assert` en cada test.
- **Aserciones:** compara errores exactos (`result.Error.ShouldBe(TodoItemErrors.NotFound(id))`); verifica el estado persistido volviendo a leer desde el contexto (unitarios) o mediante una petición GET (integración); verifica los eventos de dominio con `entity.DomainEvents.ShouldContain(e => e is XDomainEvent)`.

Plantillas completas comentadas: [../add-feature/references/tests.md](../add-feature/references/tests.md) (si la skill `add-feature` está instalada), o replica `CreateTodoCommandHandlerTests`, `TodoValidatorsTests` y `TodosTests` de este repo.
