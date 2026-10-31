using Web.Api.Common;

namespace Web.Api.Features.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"No se encontró el usuario con Id = '{userId}'");

    // Problem (400), no Failure: Failure se traduce a 500 (ADR-003).
    public static Error Unauthorized() => Error.Problem(
        "Users.Unauthorized",
        "No tienes permiso para realizar esta acción");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "No se encontró un usuario con ese correo");

    // El mismo error para correo inexistente y contraseña incorrecta: no revela qué correos existen.
    public static readonly Error InvalidCredentials = Error.Problem(
        "Users.InvalidCredentials",
        "Correo o contraseña incorrectos");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "Ya existe un usuario con ese correo");

    public static readonly Error InvalidRefreshToken = Error.Problem(
        "Users.InvalidRefreshToken",
        "El refresh token no es válido o expiró");
}
