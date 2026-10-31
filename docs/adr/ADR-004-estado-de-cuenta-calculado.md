# ADR-004 · Estado de cuenta calculado al vuelo, con "hoy" en hora de Lima

- **Estado:** Aceptada
- **Fecha:** 2026-10-30

## Contexto

El estado de cada período (Pagado / Parcial / Atrasado / Pendiente) depende de **la fecha de hoy**. Si se persistiera, habría que actualizarlo con un job cada medianoche, y cualquier falla del job dejaría la mora mal calculada.

Además, el servidor trabaja en UTC y Perú está en UTC−5. Un período que vence el 5 de octubre no puede aparecer como atrasado a las 19:00 del 5 de octubre en Lima, que en UTC ya es 6 de octubre.

## Decisión

1. **No se persiste el estado de cuenta.** `LeaseStatement.Calculate(lease, payments, today)` es una **función pura**: recibe el contrato, sus pagos y la fecha de hoy, y devuelve los períodos con su estado, los días de atraso, la deuda vencida y el saldo pendiente (RN-15 a RN-17). La usan `GetLeaseStatement`, `TerminateLease` y `GetPortfolioSummary`.
2. **"Hoy" se obtiene con `LimaClock.Today(IDateTimeProvider)`**, que convierte `UtcNow` a UTC−5 con un **offset fijo**. Perú no usa horario de verano desde 1994. Un offset fijo evita depender de la base de datos de zonas horarias del sistema operativo o del contenedor (RN-18).
3. Las fechas de negocio se guardan como `DateOnly` / `date`.

## Consecuencias

- La mora siempre está al día, sin jobs.
- La función pura se prueba con fechas fijas (incluido el borde de medianoche en Lima), sin base de datos.
- El costo es calcularla en cada consulta. Con decenas de períodos por contrato y menos de 10 propiedades es despreciable. Si la cartera creciera mucho, se podría guardar un snapshot mensual.
- Si Perú volviera a adoptar horario de verano, habría que cambiar el offset fijo por `TimeZoneInfo` (un solo archivo).
