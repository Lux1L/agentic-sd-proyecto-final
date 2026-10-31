# ADR-003 · `Result` y catálogo de errores en vez de excepciones

- **Estado:** Aceptada
- **Fecha:** 2026-10-30

## Contexto

Las reglas de negocio (solapamiento, sobrepago, contrato con deuda) son **fallas esperadas**, no situaciones excepcionales. Necesitamos que cada una devuelva un HTTP y un código estable que el frontend y las pruebas puedan comprobar.

## Decisión

- Los handlers devuelven `Result` / `Result<T>`. Las fallas de negocio se crean desde catálogos estáticos (`PropertyErrors`, `LeaseErrors`) con el código `{Feature}.{Motivo}`.
- El tipo del error define el HTTP: `Validation`/`Problem` → 400, `NotFound` → 404, `Conflict` → 409. Se responde con `ProblemDetails` (RFC 7807) vía `CustomResults.Problem`.
- Las validaciones de formato van en FluentValidation (`Validator` del slice) y se ejecutan en el `ValidationDecorator` antes del handler. Las reglas que necesitan la base de datos van en el handler.
- **No se usa `ErrorType.Failure` para reglas de negocio**, porque la plantilla lo traduce a 500. Se usa `Problem` (regla violada con datos válidos) o `Conflict` (choca con el estado actual).
- Las excepciones quedan solo para errores no esperados, y las captura `GlobalExceptionHandler`.

## Consecuencias

- El catálogo de errores de [02-contrato-api.md](../../specs/technical/02-contrato-api.md) es exhaustivo y comprobable: las pruebas verifican `error.Code`, no mensajes.
- El código de los handlers es explícito (`if (…) return Result.Failure(…)`), sin `try/catch` de negocio.
