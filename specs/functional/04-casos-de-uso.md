# 04 · Casos de uso

Actor principal en todos: **Administrador familiar** (autenticado con JWT). Cada CU corresponde a un *slice* del código (`Features/{Feature}/{CasoDeUso}.cs`).

| CU | Nombre | RF / HU | Slice |
|---|---|---|---|
| CU-01 | Registrar propiedad | RF-01 / HU-01 | `Properties/CreateProperty` |
| CU-02 | Editar propiedad | RF-01 / HU-01 | `Properties/UpdateProperty` |
| CU-03 | Listar / ver propiedades | RF-01 / HU-01 | `Properties/GetProperties`, `GetPropertyById` |
| CU-04 | Inactivar / reactivar propiedad | RF-01 / HU-01 | `Properties/DeactivateProperty`, `ActivateProperty` |
| CU-05 | Eliminar propiedad | RF-01 / HU-01 | `Properties/DeleteProperty` |
| CU-06 | Registrar contrato | RF-02 / HU-02 | `Leases/CreateLease` |
| CU-07 | Listar / ver contratos | RF-03 / HU-03 | `Leases/GetLeases`, `GetLeaseById` |
| CU-08 | Registrar pago | RF-04 / HU-04 | `Leases/RecordPayment` |
| CU-09 | Consultar estado de cuenta | RF-05 / HU-05 | `Leases/GetLeaseStatement` |
| CU-10 | Terminar contrato | RF-06 / HU-06 | `Leases/TerminateLease` |
| CU-11 | Ver resumen de cartera | RF-07 / HU-07 | `Portfolio/GetPortfolioSummary` |

---

## CU-06 · Registrar contrato

**Precondición:** la propiedad existe.
**Flujo principal**
1. El administrador envía la propiedad, los datos del arrendatario (nombre, tipo y número de documento, teléfono, email), las fechas de inicio y fin, el alquiler, la garantía y el día de pago.
2. El sistema valida el formato (RN-06, RN-07, RN-08, RN-09).
3. El sistema verifica que la propiedad no esté inactiva (RN-04).
4. El sistema verifica que no haya otro contrato activo de esa propiedad con fechas solapadas (RN-05).
5. El sistema crea el contrato en estado Activo y marca la propiedad como Alquilada (RN-10).
6. Devuelve el id del contrato.

**Flujos alternativos**
- 2a. Datos inválidos → 400 con los errores por campo.
- 3a. La propiedad no existe → 404 `Properties.NotFound`. Está inactiva → 409 `Properties.Inactive`.
- 4a. Hay solapamiento → 409 `Leases.Overlap`.

## CU-08 · Registrar pago

**Precondición:** el contrato existe.
**Flujo principal**
1. El administrador envía el período (`YYYY-MM`), el monto, la fecha de pago y el medio de pago.
2. El sistema valida el formato (RN-12).
3. El sistema verifica que el contrato esté activo y que el período esté dentro del contrato (RN-13).
4. El sistema suma lo ya abonado en el período y verifica que con este pago no se supere el alquiler (RN-14).
5. Registra el pago y devuelve su id.

**Flujos alternativos**
- 1a. La fecha de pago es futura en hora de Lima → 400 `Leases.PaymentDateInFuture`.
- 3a. El contrato no existe → 404 `Leases.NotFound`; está terminado → 409 `Leases.NotActive`; el período está fuera del contrato → 400 `Leases.PeriodOutsideLease`.
- 4a. Se supera el alquiler → 400 `Leases.PaymentExceedsRent`.

## CU-09 · Consultar estado de cuenta

**Flujo principal**
1. El administrador pide el estado de cuenta de un contrato.
2. El sistema obtiene "hoy" en hora de Lima (RN-18).
3. Lista los períodos desde el mes de inicio hasta el menor entre el mes de fin y el mes actual (RN-15).
4. Para cada período calcula lo abonado, el saldo, el estado y los días de atraso (RN-16, RN-17).
5. Devuelve los períodos, la deuda vencida y el saldo pendiente.

**Flujo alternativo:** 1a. El contrato no existe → 404 `Leases.NotFound`.

## CU-10 · Terminar contrato

**Flujo principal**
1. El administrador pide terminar un contrato.
2. El sistema verifica que esté activo.
3. Calcula el estado de cuenta (CU-09) y verifica que el saldo pendiente sea 0 (RN-11).
4. Marca el contrato como Terminado con la fecha de hoy (Lima).
5. Si la propiedad no tiene otro contrato activo, la marca como Disponible.

**Flujos alternativos:** 2a. Ya está terminado → 409 `Leases.NotActive`. 3a. Hay saldo → 409 `Leases.HasOutstandingBalance`, con el monto en el detalle.

## CU-11 · Ver resumen de cartera

**Flujo principal**
1. El administrador abre el resumen.
2. Por cada propiedad (activas primero), el sistema muestra el tipo, el estado, el arrendatario del contrato activo (si lo hay), el estado del período actual y la deuda vencida (RN-16, RN-17).
3. Devuelve además los totales de la cartera: propiedades, alquiladas y deuda vencida total.

## CU-01 a CU-05 · Propiedades (CRUD)

Flujo estándar de alta, edición, consulta y baja con validación RN-01. Alternativos: 404 `Properties.NotFound`; inactivar con contrato activo → 409 `Properties.HasActiveLease` (RN-02); eliminar con historial → 409 `Properties.HasLeaseHistory` (RN-03).
