namespace Web.Api.Common;

/// <summary>
/// Reloj de negocio en hora de Lima (RN-18, ADR-004). Perú no tiene horario de verano,
/// por lo que un offset fijo de UTC-5 es suficiente.
/// </summary>
public static class LimaClock
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(-5);

    public static DateOnly Today(this IDateTimeProvider provider) =>
        DateOnly.FromDateTime(provider.UtcNow.Add(Offset));
}
