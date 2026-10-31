# Plantillas de slice de query

Un caso de uso de tipo query es **un archivo**: `src/Web.Api/Features/{Entity}/{UseCase}.cs`, namespace `Web.Api.Features.{Entity}`. Todo lo que sigue va anidado dentro de `public static class {UseCase}`. Las queries son lecturas: sin validator, sin eventos de dominio, sin `SaveChangesAsync`.

## Esqueleto completo del slice

```csharp
using Microsoft.EntityFrameworkCore;
using Web.Api.Authentication;
using Web.Api.Common;
using Web.Api.Common.Endpoints;
using Web.Api.Common.Extensions;
using Web.Api.Common.Messaging;
using Web.Api.Database;

namespace Web.Api.Features.Todos;

public static class GetOverdueTodos
{
    public sealed record Query : IQuery<List<Response>>;

    public sealed class Response
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public List<string> Labels { get; set; } = [];
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    internal sealed class Handler(
        ApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, List<Response>>
    {
        public async Task<Result<List<Response>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            List<Response> todos = await context.TodoItems
                .Where(todoItem => todoItem.UserId == userContext.UserId &&
                                   !todoItem.IsCompleted &&
                                   todoItem.DueDate < dateTimeProvider.UtcNow)
                .Select(todoItem => new Response
                {
                    Id = todoItem.Id,
                    UserId = todoItem.UserId,
                    Description = todoItem.Description,
                    DueDate = todoItem.DueDate,
                    Labels = todoItem.Labels,
                    IsCompleted = todoItem.IsCompleted,
                    CreatedAt = todoItem.CreatedAt,
                    CompletedAt = todoItem.CompletedAt
                })
                .ToListAsync(cancellationToken);

            return todos;
        }
    }

    // El endpoint va anidado en este mismo archivo — ver endpoint.md.
    public sealed class Endpoint : IEndpoint { /* ... */ }
}
```

## Query

Sin parámetros (anidado):

```csharp
public sealed record Query : IQuery<List<Response>>;
```

Con parámetros: `public sealed record Query(Guid TodoItemId) : IQuery<Response>;`

`IQuery<T>` / `IQueryHandler<Q, T>` provienen de `Web.Api.Common.Messaging`.

## DTO de respuesta

Una `public sealed class Response` anidada: plana, fácil de serializar y nunca una entidad de dominio. Cada slice tiene su propio `Response`; no compartas DTOs entre slices aunque hoy se vean idénticos. Como está anidada, el handler y el endpoint la referencian sin calificar como `Response`.

## Handler

`internal sealed class Handler`, con `ApplicationDbContext` inyectado directamente (sin `IApplicationDbContext`). Limita al usuario actual y proyecta con `.Select` directamente al `Response` anidado. Para queries de un solo elemento, devuelve `Result.Failure<Response>(TodoItemErrors.NotFound(id))` cuando no haya coincidencias.

`IUserContext` está en `Web.Api.Authentication`; `IDateTimeProvider` y `Result`, en `Web.Api.Common`; `TodoItemErrors`, en el namespace de la feature.

## Cache (opcional, solo para lecturas frecuentes)

La clase de claves de cache es un helper a nivel de feature (no un caso de uso), así que va en su propio archivo pequeño dentro de la carpeta de la feature:

```csharp
namespace Web.Api.Features.Todos;

internal static class TodoCacheKeys
{
    internal static string ById(Guid userId, Guid todoItemId) => $"todos-{userId}-{todoItemId}";
}
```

Inyecta `HybridCache` en el handler y envuelve la consulta a la base de datos:

```csharp
Response? todo = await cache.GetOrCreateAsync(
    TodoCacheKeys.ById(userId, query.TodoItemId),
    async cancellation => await context.TodoItems
        .Where(...)
        .Select(...)
        .SingleOrDefaultAsync(cancellation),
    cancellationToken: cancellationToken);
```

Cada command que modifica los datos cacheados debe invalidar la misma clave con `cache.RemoveAsync(...)`. Si no puedes enumerar las claves afectadas, no uses cache.
