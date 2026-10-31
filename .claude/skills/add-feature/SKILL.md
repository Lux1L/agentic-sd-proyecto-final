---
name: add-feature
description: Genera una feature completa de Vertical Slice Architecture — un archivo con un command o query anidado, handler propio, validator de FluentValidation, endpoint de minimal API y tests (unitarios, de validator y de integración). Úsala cuando el usuario pida agregar una feature, caso de uso, command, query o endpoint a esta plantilla de Vertical Slice Architecture.
argument-hint: <descripción de la feature, p. ej. "archivar un todo" u "obtener los todos que vencen esta semana">
---

# Agregar una feature (vertical slice)

Genera un caso de uso completo siguiendo las convenciones de esta plantilla: un único archivo autocontenido bajo `Features/{Entity}/` que contiene el command/query, su handler propio, su validator y su endpoint de minimal API. Sin MediatR: este código usa sus propias abstracciones `ICommand`/`IQuery` (`Web.Api.Common.Messaging`) con handlers y decorators registrados por Scrutor.

## La regla de un solo archivo

Un caso de uso es **un archivo**: `src/Web.Api/Features/{Entity}/{UseCase}.cs`, namespace `Web.Api.Features.{Entity}`. Ese archivo es una `public static class {UseCase}` cuyos miembros anidados conforman todo el slice:

```csharp
public static class ArchiveTodo
{
    public sealed record Command(...) : ICommand;              // o Query : IQuery<T>
    public sealed class Validator : AbstractValidator<Command> { ... }  // solo commands
    internal sealed class Handler(...) : ICommandHandler<Command> { ... }
    public sealed class Endpoint : IEndpoint { ... }
}
```

Todo lo del slice vive aquí: no hay proyecto Application/Domain/Infrastructure aparte ni carpeta `Endpoints/` aparte. El código transversal vive en `Common/`, `Database/`, `Authentication/`, `Authorization/`; cada feature vive en `Features/{Entity}/`.

## Flujo de trabajo

1. **Clasifica el caso de uso.** Un cambio de estado es un **command**; una lectura es una **query**. Nombra el slice con el verbo del caso de uso + la entidad, p. ej. `ArchiveTodo`, `GetOverdueTodos`; ese nombre es el del archivo, el del vecino en la carpeta y el de la clase estática.
2. **Revisa los tipos de dominio de la feature.** Si la entidad, su clase `{Entity}Errors` o un evento de dominio necesario no existen en `Features/{Entity}/`, agrégalos primero (ver la skill `add-entity`). Los commands que cambian el estado deben emitir un evento de dominio mediante `entity.Raise(...)`.
3. **Crea el archivo del slice** en `src/Web.Api/Features/{Entity}/{UseCase}.cs` con el `Command`/`Query`, `Validator` (solo commands), `Handler` y `Endpoint` anidados. Plantillas: [references/command-slice.md](references/command-slice.md), [references/query-slice.md](references/query-slice.md) y [references/endpoint.md](references/endpoint.md).
4. **Escribe los tests**: tests unitarios del handler, tests del validator y un test de integración. Plantillas: [references/tests.md](references/tests.md).
5. **Verifica:** `dotnet build` y luego `dotnet test`. Todo debe pasar.

## Convenciones no negociables

- **Archivo = caso de uso.** Un archivo por caso de uso bajo `src/Web.Api/Features/{Entity}/` (p. ej. `Features/Todos/ArchiveTodo.cs`), que contiene todo el slice como tipos anidados de `public static class {UseCase}`.
- **Los handlers son `internal sealed`** con constructores primarios, anidados como `Handler`, e implementan `ICommandHandler<Command>`, `ICommandHandler<Command, TResponse>` o `IQueryHandler<Query, TResponse>` (de `Web.Api.Common.Messaging`).
- **Sin registro manual en DI.** Los handlers, validators y endpoints se descubren por escaneo del assembly: Scrutor (`AssignableTo(ICommandHandler<,>)`), `AddValidatorsFromAssembly(includeInternalTypes: true)` y `AddEndpoints`. Los tipos anidados están en `assembly.DefinedTypes`, así que se encuentran automáticamente. Nunca registres un slice nuevo a mano.
- **Devuelve `Result` / `Result<T>`, nunca lances excepciones** para fallos esperados. Los errores provienen de métodos factory estáticos en `{Entity}Errors` (en la carpeta de la feature) con códigos como `"Todos.NotFound"`. `Result`, `Error`, `ErrorType` viven en `Web.Api.Common`.
- **La validación vive en el `Validator` anidado** (FluentValidation). Se ejecuta automáticamente mediante `ValidationDecorator`; el handler nunca valida la forma de la entrada por sí mismo. Las queries no tienen validators (el decorator solo envuelve commands).
- **Acceso a datos directamente mediante `ApplicationDbContext`** (de `Web.Api.Database`). **No existe `IApplicationDbContext`**: inyecta el `ApplicationDbContext` concreto en el handler.
- **Verificación de autorización** en los handlers que actúan sobre datos propiedad del usuario: compara `IUserContext.UserId` (de `Web.Api.Authentication`) y devuelve `UserErrors.Unauthorized()` si no coincide, o filtra las queries por `userContext.UserId`.
- **Las queries proyectan directamente a un DTO `Response` anidado** con `.Select(...)`; nunca devuelven entidades de dominio. Cachea las lecturas frecuentes con `HybridCache` usando una clase estática `{Feature}CacheKeys`; invalida en los commands que modifican los datos cacheados.
- **El `Endpoint` anidado** implementa `IEndpoint`, resuelve la interfaz del handler directamente desde la DI, usa `result.Match(Results.Ok, CustomResults.Problem)` (o `Results.NoContent` para commands sin retorno), se etiqueta con la clase `Tags` y llama a `.RequireAuthorization()`.
- **Sin acoplamiento entre features.** Un slice puede referenciar los **tipos de dominio** de otra feature (p. ej. `Todos` usa `User`/`UserErrors` mediante `using Web.Api.Features.Users;`) y el código compartido de `Common`/`Database`/`Authentication`/`Authorization`. **Nunca** debe referenciar el `Command`/`Query`/`Handler`/`Validator`/`Endpoint` de otra feature.

## Referencia de nombres

| Artefacto | Patrón | Ejemplo |
|---|---|---|
| Archivo y clase del slice | `{Verb}{Entity}` / `Get{X}` | `Features/Todos/ArchiveTodo.cs` → `public static class ArchiveTodo` |
| Command | `Command` anidado | `ArchiveTodo.Command` |
| Query | `Query` anidado | `GetOverdueTodos.Query` |
| Handler | `internal sealed Handler` anidado | `ArchiveTodo.Handler` |
| Validator | `Validator` anidado | `ArchiveTodo.Validator` |
| Response | `Response` anidado | `GetOverdueTodos.Response` |
| Endpoint | `Endpoint : IEndpoint` anidado | `ArchiveTodo.Endpoint` |
| Test unitario | `{UseCase}HandlerTests` | `ArchiveTodoHandlerTests` |
| Método de test | `Handle_Should_{Outcome}_When{Condition}` | `Handle_Should_ReturnNotFound_WhenTodoDoesNotExist` |
