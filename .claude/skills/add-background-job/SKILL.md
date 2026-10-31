---
name: add-background-job
description: Agrega un background job (un BackgroundService hospedado) a la plantilla de Vertical Slice Architecture que se ejecuta de forma programada y resuelve servicios scoped de manera segura. Úsala cuando el usuario quiera una tarea recurrente, un worker, un job de limpieza o un procesamiento programado.
argument-hint: <qué hace el job, p. ej. "eliminar los refresh tokens expirados cada hora">
---

# Agregar un background job

El trabajo recurrente se ejecuta como un `BackgroundService` en `src/Web.Api/BackgroundJobs/`. Un hosted service es un **singleton**, así que los servicios scoped (`ApplicationDbContext`, handlers) deben resolverse dentro de un scope creado en cada iteración.

## Archivos a crear/modificar

1. **El job**: `src/Web.Api/BackgroundJobs/{Name}BackgroundService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Web.Api.Database;

namespace Web.Api.BackgroundJobs;

internal sealed class ExpiredRefreshTokensCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredRefreshTokensCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await context.RefreshTokens
                    .Where(token => token.ExpiresOnUtc < DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Background job {Job} failed", nameof(ExpiredRefreshTokensCleanupService));
            }
        }
    }
}
```

2. **Registro**: en `src/Web.Api/DependencyInjection.cs` (p. ej. dentro de `AddServices`, o en un pequeño helper `AddBackgroundJobs` conectado a `AddInfrastructure`):

```csharp
services.AddHostedService<ExpiredRefreshTokensCleanupService>();
```

## Reglas

- Un hosted service es un singleton: **nunca** inyectes `ApplicationDbContext`, handlers u otros servicios scoped/transient directamente. Resuélvelos desde un scope por iteración mediante `IServiceScopeFactory`. **No existe `IApplicationDbContext`**: resuelve el `ApplicationDbContext` concreto (de `Web.Api.Database`).
- Envuelve el cuerpo de cada iteración en try/catch para que un fallo no detenga el bucle; registra con `ILogger`.
- Usa `PeriodicTimer` para la programación y respeta siempre el `stoppingToken`.
- Si el trabajo es en realidad un caso de uso, es preferible resolver e invocar un command/handler existente (`ICommandHandler<...>` de `Web.Api.Common.Messaging`) dentro del scope en lugar de duplicar la lógica.
- Compila y ejecuta los tests al terminar.
