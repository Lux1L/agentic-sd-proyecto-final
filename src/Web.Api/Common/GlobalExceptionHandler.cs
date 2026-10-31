using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Common;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Un cuerpo que no se puede leer (JSON inválido, fecha nula…) es culpa del cliente: 400, no 500.
        if (exception is BadHttpRequestException badRequest)
        {
            logger.LogWarning(exception, "Bad request");

            var badRequestDetails = new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Request.Invalid",
                Detail = "El cuerpo de la solicitud no tiene el formato esperado"
            };

            httpContext.Response.StatusCode = badRequest.StatusCode;

            await httpContext.Response.WriteAsJsonAsync(badRequestDetails, cancellationToken);

            return true;
        }

        logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server failure"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
