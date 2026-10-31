# 02 · Contrato de la API

Base: `http://localhost:5080`. Documentación interactiva en **`/scalar/v1`** (OpenAPI en `/openapi/v1.json`). Todos los endpoints de negocio requieren `Authorization: Bearer <jwt>`. Los errores se devuelven como `ProblemDetails`: `title` = código del error, `detail` = mensaje y `errors` = errores por campo cuando es una validación.

## Autenticación (plantilla base · RF-00)

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/users/register` | Alta de usuario administrador. |
| POST | `/users/login` | Devuelve el `accessToken` y el `refreshToken`. |
| POST | `/users/refresh-token` | Rota el refresh token. |

## Propiedades (RF-01)

| Método | Ruta | Cuerpo / query | Respuesta | Errores |
|---|---|---|---|---|
| POST | `/properties` | `{ name, address, type }` | `200 Guid` | 400 |
| GET | `/properties` | `?search&status&page&pageSize` | `200 PagedList<Property>` | — |
| GET | `/properties/{id}` | — | `200 Property` | 404 |
| PUT | `/properties/{id}` | `{ name, address, type }` | `204` | 400, 404 |
| POST | `/properties/{id}/deactivate` | — | `204` | 404, 409 `Properties.HasActiveLease` |
| POST | `/properties/{id}/activate` | — | `204` | 404 |
| DELETE | `/properties/{id}` | — | `204` | 404, 409 `Properties.HasLeaseHistory` |

`type`: `Apartment` · `House` · `Room` · `Commercial`. `status`: `Available` · `Leased` · `Inactive` (los enums se serializan como texto).

## Contratos y pagos (RF-02 a RF-06)

| Método | Ruta | Cuerpo / query | Respuesta | Errores |
|---|---|---|---|---|
| POST | `/leases` | ver abajo | `200 Guid` | 400, 404 `Properties.NotFound`, 409 `Properties.Inactive`, 409 `Leases.Overlap` |
| GET | `/leases` | `?propertyId&status&search&page&pageSize` | `200 PagedList<Lease>` | — |
| GET | `/leases/{id}` | — | `200 Lease` | 404 |
| POST | `/leases/{id}/payments` | `{ period: "2026-10", amount, paidOn: "2026-10-05", method }` | `200 Guid` | 400, 400 `Leases.PeriodOutsideLease`, 400 `Leases.PaymentExceedsRent`, 400 `Leases.PaymentDateInFuture`, 404, 409 `Leases.NotActive` |
| GET | `/leases/{id}/statement` | — | `200 Statement` | 404 |
| POST | `/leases/{id}/terminate` | — | `204` | 404, 409 `Leases.NotActive`, 409 `Leases.HasOutstandingBalance` |

**Crear contrato**

```json
{
  "propertyId": "6d1c…",
  "tenantFullName": "María Quispe Huamán",
  "tenantDocumentType": "Dni",
  "tenantDocumentNumber": "45678912",
  "tenantPhone": "987654321",
  "tenantEmail": "maria@example.com",
  "startDate": "2026-08-01",
  "endDate": "2027-07-31",
  "monthlyRent": 1200.00,
  "deposit": 2400.00,
  "paymentDay": 5
}
```

**Estado de cuenta (`Statement`)**

```json
{
  "leaseId": "…",
  "today": "2026-10-20",
  "monthlyRent": 1200.00,
  "overdueDebt": 700.00,
  "outstandingBalance": 700.00,
  "periods": [
    { "period": "2026-08", "dueDate": "2026-08-05", "paid": 1200.00, "balance": 0, "status": "Paid", "daysOverdue": 0 },
    { "period": "2026-10", "dueDate": "2026-10-05", "paid": 500.00, "balance": 700.00, "status": "Overdue", "daysOverdue": 15 }
  ]
}
```

`method`: `Cash` · `BankTransfer` · `Yape` · `Plin`. `status` del período: `Paid` · `Partial` · `Overdue` · `Pending`.

## Resumen de cartera (RF-07)

| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/portfolio/summary` | `200 { today, totalProperties, leasedProperties, totalOverdueDebt, properties: [{ propertyId, name, type, status, leaseId?, tenantFullName?, currentPeriodStatus?, currentPeriodPaid?, overdueDebt, currentPeriodDueDate?, overduePeriods, maxDaysOverdue, nextDueDate? }] }` — `currentPeriodPaid`: lo abonado en el período del mes actual (alimenta el Meter de la UI). Bandeja de cobranza (RN-19): `currentPeriodDueDate` (`YYYY-MM-DD` o `null`): vencimiento del período del mes actual (RN-15); `overduePeriods` (`string[]`, nunca `null`): períodos `YYYY-MM` en estado Atrasado, del más antiguo al más reciente; `maxDaysOverdue` (`int`): días de atraso del período atrasado más antiguo, 0 si no hay; `nextDueDate` (`YYYY-MM-DD` o `null`): vencimiento del período siguiente al mes actual, solo si está dentro del contrato (no pasa del mes de fin ni del de terminación). Sin contrato vigente: `null`, `[]`, `0`, `null`. |

## Errores de negocio (catálogo)

| Código | HTTP | Regla |
|---|---|---|
| `Properties.NotFound` | 404 | — |
| `Properties.Inactive` | 409 | RN-04 |
| `Properties.HasActiveLease` | 409 | RN-02 |
| `Properties.HasLeaseHistory` | 409 | RN-03 |
| `Leases.NotFound` | 404 | — |
| `Leases.Overlap` | 409 | RN-05 |
| `Leases.NotActive` | 409 | RN-11, RN-13 |
| `Leases.HasOutstandingBalance` | 409 | RN-11 |
| `Leases.PeriodOutsideLease` | 400 | RN-13 |
| `Leases.PaymentExceedsRent` | 400 | RN-14 |
| `Leases.PaymentDateInFuture` | 400 | RN-12 |
