# 02 · Requerimientos funcionales, reglas de negocio y no funcionales

Cada regla de negocio (RN) indica **dónde se aplica** (validador = error 400 de validación; handler = error de negocio 400/404/409) y su código de error. La trazabilidad completa está en [../technical/03-trazabilidad.md](../technical/03-trazabilidad.md).

## Requerimientos funcionales

| ID | Requerimiento | Prioridad |
|---|---|---|
| RF-01 | Registrar, editar, listar, ver, activar/inactivar y eliminar propiedades. | Alta |
| RF-02 | Registrar un contrato de alquiler sobre una propiedad, con los datos del arrendatario. | Alta |
| RF-03 | Listar contratos (paginado, filtro por propiedad y estado) y ver el detalle de uno. | Media |
| RF-04 | Registrar pagos (abonos) de un período de un contrato. | Alta |
| RF-05 | Consultar el estado de cuenta de un contrato: estado por período, días de atraso y deuda. | Alta |
| RF-06 | Terminar un contrato. | Media |
| RF-07 | Ver el resumen de la cartera como bandeja de cobranza: cada propiedad con su arrendatario actual, el estado del mes en curso, la deuda vencida, los períodos atrasados con sus días de atraso, y el vencimiento del mes en curso y el del mes siguiente. | Alta |
| RF-00 | Registrarse e iniciar sesión (JWT con refresh token). Viene de la plantilla base. | Alta |

## Reglas de negocio

### Propiedades (RF-01)

| ID | Regla | Dónde | Error |
|---|---|---|---|
| RN-01 | Nombre (máx. 100) y dirección (máx. 200) son obligatorios; el tipo es Departamento, Casa, Habitación o Local. | Validador | 400 validación |
| RN-02 | No se puede **inactivar** una propiedad con un contrato activo. | Handler | 409 `Properties.HasActiveLease` |
| RN-03 | Solo se puede **eliminar** una propiedad que nunca tuvo contratos. Si tiene historial, se inactiva (para no perder el historial de pagos). | Handler | 409 `Properties.HasLeaseHistory` |

### Contratos (RF-02, RF-06)

| ID | Regla | Dónde | Error |
|---|---|---|---|
| RN-04 | La propiedad debe existir y **no estar inactiva**. | Handler | 404 `Properties.NotFound` / 409 `Properties.Inactive` |
| RN-05 | **No puede haber solapamiento**: las fechas `[inicio, fin]` no pueden cruzarse con las de otro contrato activo de la misma propiedad. | Handler | 409 `Leases.Overlap` |
| RN-06 | El contrato dura al menos un mes: `fin ≥ inicio + 1 mes`. | Validador | 400 validación |
| RN-07 | Alquiler mensual > 0. Garantía ≥ 0 y **≤ 2 veces el alquiler** (política de la familia, no exigencia legal). | Validador | 400 validación |
| RN-08 | Día de pago entre 1 y 28 (así existe en todos los meses). | Validador | 400 validación |
| RN-09 | El documento del arrendatario se valida según su tipo: **DNI** = 8 dígitos; **CE** = 9 a 12 caracteres alfanuméricos; **RUC** = 11 dígitos, que empiece con 10, 15, 17 o 20, con **dígito verificador módulo 11** correcto. Nombre obligatorio; teléfono opcional (celular peruano: 9 dígitos que empiezan con 9); email opcional y válido. | Validador | 400 validación |
| RN-10 | Al registrar un contrato, la propiedad pasa a **Alquilada**. | Handler | — |
| RN-11 | **No se termina un contrato con saldo pendiente** en los períodos transcurridos hasta hoy (incluido el mes en curso). Al terminarlo, la propiedad vuelve a **Disponible** si no le queda otro contrato activo. | Handler | 409 `Leases.HasOutstandingBalance` / 409 `Leases.NotActive` |

### Pagos (RF-04)

| ID | Regla | Dónde | Error |
|---|---|---|---|
| RN-12 | Monto > 0; período con formato `YYYY-MM`; medio de pago: Efectivo, Transferencia, Yape o Plin; fecha de pago no futura (hora de Lima). | Validador + handler | 400 validación / 400 `Leases.PaymentDateInFuture` |
| RN-13 | Solo se registran pagos en contratos **activos** y para un período **dentro del contrato** (entre el mes de inicio y el mes de fin). Se permiten pagos adelantados de meses futuros del contrato. | Handler | 409 `Leases.NotActive` / 400 `Leases.PeriodOutsideLease` |
| RN-14 | Se permiten abonos parciales, pero la **suma de un período no puede superar el alquiler** mensual. | Handler | 400 `Leases.PaymentExceedsRent` |

### Estado de cuenta (RF-05, RF-07)

| ID | Regla |
|---|---|
| RN-15 | El período `M` **vence** el día de pago de `M`; **el primer período vence en la fecha de inicio del contrato si esta es posterior al día de pago** (se paga al firmar). Se listan los períodos desde el mes de inicio hasta el menor entre el mes de fin y el mes actual (en hora de Lima). |
| RN-16 | Estado de un período: **Pagado** si lo abonado ≥ alquiler; **Atrasado** si hoy > fecha de vencimiento y lo abonado < alquiler (aunque haya abonos parciales); **Parcial** si hay abonos y aún no vence; **Pendiente** si no hay abonos y aún no vence. |
| RN-17 | Días de atraso = hoy − fecha de vencimiento (solo períodos atrasados). **Deuda vencida** = suma de los saldos de los períodos atrasados. **Saldo pendiente** = suma de los saldos de todos los períodos listados. |
| RN-18 | "Hoy" es la fecha calendario en **America/Lima (UTC−5)**, no en UTC. Un registro hecho a las 21:00 de Lima del 5 de octubre cuenta como 5 de octubre, aunque en UTC ya sea el 6. |
| RN-19 | Bandeja de cobranza (resumen de cartera), por cada contrato vigente: **vencimiento del período actual** = la fecha de vencimiento (RN-15) del período del mes actual, vacía si ese mes no está en el estado de cuenta; **períodos atrasados** = los períodos en estado Atrasado (RN-16), del más antiguo al más reciente; **días de atraso máximos** = los del período atrasado más antiguo (RN-17), 0 si no hay; **próximo vencimiento** = la fecha de vencimiento (RN-15) del período siguiente al mes actual, solo si ese período está dentro del contrato (entre el mes de inicio y el menor entre el mes de fin y el de terminación); si no, no hay próximo vencimiento. Sin contrato vigente: fechas vacías, sin períodos atrasados y 0 días. |

## Requerimientos no funcionales

| ID | Requerimiento |
|---|---|
| RNF-01 | API REST en ASP.NET Core .NET 10, documentada con OpenAPI y Scalar (`/scalar/v1`). |
| RNF-02 | Persistencia en PostgreSQL 18 con EF Core 10 y migraciones versionadas. |
| RNF-03 | Todos los endpoints de negocio requieren JWT; los de autenticación tienen rate limiting. |
| RNF-04 | Los errores se devuelven como `ProblemDetails` (RFC 7807) con un código estable `{Feature}.{Motivo}`. |
| RNF-05 | Los montos usan `decimal(12,2)`; nunca `double`. |
| RNF-06 | Pruebas automatizadas: unitarias por regla de negocio, de integración por endpoint (Testcontainers con PostgreSQL real) y de arquitectura. |
| RNF-07 | El repo se levanta con `docker compose up -d` + `dotnet run`, sin pasos ocultos. Sin secretos reales en el repo. |
