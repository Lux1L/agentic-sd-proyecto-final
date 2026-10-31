# Plantillas de slice de command

Un caso de uso de tipo command es **un archivo**: `src/Web.Api/Features/{Entity}/{UseCase}.cs`, namespace `Web.Api.Features.{Entity}`. Todo lo que sigue va anidado dentro de `public static class {UseCase}`. Reemplaza `{Entity}` (carpeta de la feature, en plural, p. ej. `Todos`), `{UseCase}` (p. ej. `ArchiveTodo`) y los nombres de los tipos de dominio en todo el archivo.

## Esqueleto completo del slice

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Web.Api.Authentication;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Todos;

public static class ArchiveTodo
{
    public sealed record Command(Guid TodoItemId) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.TodoItemId).NotEmpty();
        }
    }

    internal sealed class Handler(
        ApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IUserContext userContext) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            TodoItem? todoItem = await context.TodoItems
                .SingleOrDefaultAsync(
                    t => t.Id == command.TodoItemId && t.UserId == userContext.UserId,
                    cancellationToken);

            if (todoItem is null)
            {
                return Result.Failure(TodoItemErrors.NotFound(command.TodoItemId));
            }

            if (todoItem.IsArchived)
            {
                return Result.Failure(TodoItemErrors.AlreadyArchived(command.TodoItemId));
            }

            todoItem.IsArchived = true;
            todoItem.ArchivedAt = dateTimeProvider.UtcNow;

            todoItem.Raise(new TodoItemArchivedDomainEvent(todoItem.Id));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }

    // El endpoint va anidado en este mismo archivo — ver endpoint.md.
    public sealed class Endpoint : IEndpoint { /* ... */ }
}
```

## Command

Record posicional cuando hay pocos parámetros (anidado):

```csharp
public sealed record Command(Guid TodoItemId) : ICommand;
```

Clase con propiedades con setter cuando hay muchos parámetros (igual que `CreateTodo.Command`):

```csharp
public sealed class Command : ICommand<Guid>
{
    public Guid UserId { get; set; }
    public string Description { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = [];
    public Priority Priority { get; set; }
}
```

- `ICommand` → el handler devuelve `Result` (el endpoint responde `204 NoContent`).
- `ICommand<TResponse>` → el handler devuelve `Result<TResponse>` (el endpoint responde `200 Ok`).
- `ICommand`/`ICommand<T>` provienen de `Web.Api.Common.Messaging`.

## Validator

`public sealed class Validator : AbstractValidator<Command>` anidado. Se registra automáticamente y `ValidationDecorator` lo ejecuta antes de que corra el handler. Como `Command` está anidado, `AbstractValidator<Command>` se resuelve sin calificar dentro de la clase del slice.

```csharp
public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Priority).IsInEnum();
        RuleFor(c => c.Description).NotEmpty().MaximumLength(255);
        RuleFor(c => c.DueDate).GreaterThanOrEqualTo(DateTime.Today).When(x => x.DueDate.HasValue);
    }
}
```

## Handler

`internal sealed class Handler`, constructor primario, `ApplicationDbContext` para el acceso a datos — **inyectado directamente**, no existe `IApplicationDbContext`. Las cláusulas de guarda devuelven `Result.Failure` con los errores de la feature; el camino feliz modifica, emite un evento de dominio, guarda y devuelve.

```csharp
internal sealed class Handler(
    ApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<Command, Guid>
{
    public async Task<Result<Guid>> Handle(Command command, CancellationToken cancellationToken)
    {
        if (userContext.UserId != command.UserId)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized());
        }

        User? user = await context.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
        }

        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Description = command.Description,
            Priority = command.Priority,
            DueDate = command.DueDate,
            Labels = command.Labels,
            IsCompleted = false,
            CreatedAt = dateTimeProvider.UtcNow
        };

        todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

        context.TodoItems.Add(todoItem);

        await context.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}
```

Notas:
- `Handler` es `internal`, pero el escaneo del assembly lo encuentra (Scrutor `AssignableTo(ICommandHandler<,>)` sobre `DefinedTypes`, que incluye los tipos anidados).
- Propiedad: filtra por `userContext.UserId` en la consulta (preferido) o compara explícitamente y devuelve `Result.Failure(UserErrors.Unauthorized())`. `IUserContext` está en `Web.Api.Authentication`; `UserErrors`/`User` en `Web.Api.Features.Users` (agrega `using Web.Api.Features.Users;` — una referencia a un tipo de dominio, permitida entre features).
- `IDateTimeProvider` (de `Web.Api.Common`) para las marcas de tiempo — nunca `DateTime.UtcNow` directamente.
- Si el command invalida datos de queries cacheados, inyecta `HybridCache` y llama a `cache.RemoveAsync({Feature}CacheKeys.X(...), cancellationToken)` después de guardar.
- Para un command que devuelve un valor (`ICommand<Guid>`), devuelve el valor directamente — `Result<T>` tiene una conversión implícita: `return todoItem.Id;`.

## Agregados al dominio (si se necesitan)

Estos son tipos de dominio y viven en la carpeta de la feature (`Features/{Entity}/`), namespace `Web.Api.Features.{Entity}` — ver la skill `add-entity`.

Factory de error en la clase `{Entity}Errors` existente:

```csharp
public static Error AlreadyArchived(Guid todoItemId) => Error.Problem(
    "TodoItems.AlreadyArchived",
    $"The todo item with Id = '{todoItemId}' is already archived.");
```

Tipo de error → código HTTP (mediante `CustomResults.Problem`): `NotFound` → 404, `Conflict` → 409, `Problem`/`Validation` → 400, `Failure` → 500.

Evento de dominio, un archivo por cada uno en `Features/{Entity}/`:

```csharp
using Web.Api.Common;

namespace Web.Api.Features.Todos;

public sealed record TodoItemArchivedDomainEvent(Guid TodoItemId) : IDomainEvent;
```

Handler de evento opcional — un tipo pequeño e independiente (no es un caso de uso, así que no necesita estar anidado):

```csharp
using Web.Api.Common;

namespace Web.Api.Features.Todos;

internal sealed class TodoItemArchivedDomainEventHandler : IDomainEventHandler<TodoItemArchivedDomainEvent>
{
    public Task Handle(TodoItemArchivedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Efectos secundarios aquí (notificaciones, proyecciones, ...)
        return Task.CompletedTask;
    }
}
```
