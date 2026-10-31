# Plantillas de endpoint

El endpoint **no es un archivo aparte**: es una `public sealed class Endpoint : IEndpoint` anidada dentro de la `public static class {UseCase}` del slice (el mismo archivo, `src/Web.Api/Features/{Entity}/{UseCase}.cs`). `IEndpoint`, `Tags` y `.HasPermission(...)` provienen de `Web.Api.Common.Endpoints`; `CustomResults`, de `Web.Api.Common`; la extensión `.Match(...)`, de `Web.Api.Common.Extensions`. `AddEndpoints`/`MapEndpoints` descubren los endpoints automáticamente (el escaneo lee `assembly.DefinedTypes`, que incluye los tipos anidados), así que no hace falta registrarlos.

Como `Command`/`Query`/`Response` son hermanos dentro de la misma clase estática, el endpoint los referencia sin calificar.

## Command con cuerpo de respuesta (POST → 200 + valor)

```csharp
public sealed class Endpoint : IEndpoint
{
    public sealed class Request
    {
        public Guid UserId { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public List<string> Labels { get; set; } = [];
        public int Priority { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("todos", async (
            Request request,
            ICommandHandler<Command, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new Command
            {
                UserId = request.UserId,
                Description = request.Description,
                DueDate = request.DueDate,
                Labels = request.Labels,
                Priority = (Priority)request.Priority
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Todos)
        .RequireAuthorization();
    }
}
```

## Command sin retorno a partir de un parámetro de ruta (PUT/DELETE → 204)

```csharp
public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("todos/{id:guid}/archive", async (
            Guid id,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new Command(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Todos)
        .RequireAuthorization();
    }
}
```

## Query (GET → 200)

```csharp
public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("todos/overdue", async (
            IQueryHandler<Query, List<Response>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new Query();

            Result<List<Response>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Todos)
        .RequireAuthorization();
    }
}
```

## Reglas

- Las rutas van en minúsculas, en plural y sin slash inicial: `todos`, `todos/{id:guid}`, `users/{userId:guid}/todos`. Restricciones de ruta (`:guid`) en todos los parámetros tipados.
- La clase `Request` anidada existe solo cuando hay un cuerpo JSON; se mapea 1:1 al `Command` dentro de la lambda (los valores de enum llegan como `int` y se castean). Cuando la entrada es un único parámetro de ruta, construye el `Command` directamente a partir de él, sin `Request`.
- Resuelve la interfaz del handler (`ICommandHandler<...>` / `IQueryHandler<...>`, de `Web.Api.Common.Messaging`) directamente como parámetro de la lambda; se inyecta la instancia decorada.
- Termina siempre con `.WithTags(Tags.{Feature})` y `.RequireAuthorization()` (o `.HasPermission(Permissions.X)` cuando exista una constante de permiso). Si la feature es nueva, agrega su constante a `Tags` (en `Common/Endpoints/`).
- Los fallos nunca reciben respuestas armadas a mano: `CustomResults.Problem` traduce el `Error` a un ProblemDetails RFC 7807 con el código de estado correcto.
- El endpoint puede referenciar los tipos de dominio de otra feature, pero nunca el `Command`/`Query`/`Handler` de otra feature.
