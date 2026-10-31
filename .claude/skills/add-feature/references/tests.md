# Plantillas de tests

Stack: xUnit + Shouldly + NSubstitute (unitarios), FluentValidation.TestHelper (validators), WebApplicationFactory + Testcontainers (integración). Cada caso de uso nuevo lleva los tres. Como el slice es una sola clase estática, sus miembros se referencian de forma calificada: `CreateTodo.Command`, `new CreateTodo.Handler(...)`, `new CreateTodo.Validator()`.

`GlobalUsings.cs` ya importa `Xunit`, `NSubstitute` y `Shouldly`; no los vuelvas a agregar. `Result`/`Error` provienen de `Web.Api.Common`; el slice, la entidad, los errores y los eventos de dominio, de `Web.Api.Features.{Entity}`.

## Tests unitarios del handler

`tests/Web.Api.UnitTests/{Feature}/{UseCase}HandlerTests.cs`. Hereda de `BaseHandlerTest`, que provee `CreateDbContext()` (un `ApplicationDbContext` nuevo en memoria) y `CreateCache()` (un `HybridCache` real). En esta plantilla no existe `IApplicationDbContext`/`TestDbContext`: los handlers reciben el `ApplicationDbContext` concreto, así que los tests lo usan directamente. Haz mock solo de interfaces (`IUserContext`, `IDateTimeProvider`) con NSubstitute.

Cubre: cada camino de fallo (un test por cláusula de guarda) y el camino feliz, incluyendo el estado persistido y los eventos de dominio emitidos.

```csharp
using Microsoft.EntityFrameworkCore;
using Web.Api.Authentication;
using Web.Api.Common;
using Web.Api.Database;
using Web.Api.Features.Todos;

namespace Web.Api.UnitTests.Todos;

public sealed class ArchiveTodoHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenTodoDoesNotExist()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var command = new ArchiveTodo.Command(Guid.NewGuid());
        var handler = new ArchiveTodo.Handler(context, dateTimeProvider, userContext);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TodoItemErrors.NotFound(command.TodoItemId));
    }

    [Fact]
    public async Task Handle_Should_ArchiveTodoAndRaiseDomainEvent_WhenValid()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Description = "Archive me",
            CreatedAt = DateTime.UtcNow
        };
        context.TodoItems.Add(todoItem);
        await context.SaveChangesAsync();

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var command = new ArchiveTodo.Command(todoItem.Id);
        var handler = new ArchiveTodo.Handler(context, dateTimeProvider, userContext);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        TodoItem archived = await context.TodoItems.SingleAsync(t => t.Id == todoItem.Id);
        archived.IsArchived.ShouldBeTrue();
        archived.DomainEvents.ShouldContain(e => e is TodoItemArchivedDomainEvent);
    }
}
```

Convenciones:
- Nombres de tests: `Handle_Should_{Outcome}_When{Condition}`.
- Comentarios `// Arrange` / `// Act` / `// Assert` en cada test.
- Verifica los fallos comparando el error exacto: `result.Error.ShouldBe(TodoItemErrors.NotFound(id))`.
- Las propiedades nuevas de una entidad se reflejan automáticamente. Agregar un `DbSet` completamente nuevo es un paso de `add-entity` (va en `ApplicationDbContext`, que el contexto de test en memoria usa tal cual).

## Tests de validators

`tests/Web.Api.UnitTests/{Feature}/{Feature}ValidatorsTests.cs` (extiende el archivo existente si lo hay). Una sola clase cubre todos los validators de una feature; cada validator es el `Validator` anidado del slice.

```csharp
using FluentValidation.TestHelper;
using Web.Api.Features.Todos;

// ...
private readonly CreateTodo.Validator _createValidator = new();

[Fact]
public void CreateValidator_Should_HaveError_WhenDescriptionIsEmpty()
{
    var command = new CreateTodo.Command
    {
        UserId = Guid.NewGuid(),
        Description = string.Empty,
        Priority = Priority.Low
    };

    TestValidationResult<CreateTodo.Command> result = _createValidator.TestValidate(command);

    result.ShouldHaveValidationErrorFor(c => c.Description);
}
```

Cubre el fallo de cada regla más un command completamente válido (`ShouldNotHaveAnyValidationErrors`).

## Tests de integración

`tests/IntegrationTests/{Feature}/{Feature}Tests.cs` (extiende el archivo existente si lo hay). Hereda de `BaseIntegrationTest(factory)`, que ejecuta la API real contra un Postgres de Testcontainers y provee `HttpClient`, `RegisterAndLoginAsync()` y `Authenticate(token)`. Los tests pasan por HTTP real y nunca llaman a los handlers directamente, así que no referencian los tipos del slice: define records privados de DTO de respuesta dentro de la clase de test (ver `TodosTests.TodoDto`).

```csharp
[Fact]
public async Task ArchiveTodo_Should_MarkTodoAsArchived()
{
    // Arrange
    (Guid userId, AccessTokens tokens) = await RegisterAndLoginAsync();
    Authenticate(tokens.AccessToken);

    var createRequest = new
    {
        userId,
        description = "Todo to archive",
        labels = Array.Empty<string>(),
        priority = 1
    };
    HttpResponseMessage createResponse = await HttpClient.PostAsJsonAsync("todos", createRequest);
    createResponse.EnsureSuccessStatusCode();
    Guid todoId = await createResponse.Content.ReadFromJsonAsync<Guid>();

    // Act
    HttpResponseMessage response = await HttpClient.PutAsync($"todos/{todoId}/archive", null);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
}
```

Cobertura mínima por endpoint: un test de no autorizado (sin token → 401) si es una familia de rutas nueva, un test del camino feliz que verifique el estado observable mediante un GET posterior y un test de traducción de fallos (p. ej. id desconocido → 404) cuando el handler tenga caminos de fallo.

## Ejecutar

```
dotnet test
```

Los tests de integración necesitan Docker corriendo (Testcontainers).
