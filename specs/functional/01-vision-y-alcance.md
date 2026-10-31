# 01 · Visión y alcance — Arriendo Familiar

## Problema

Una familia en Perú que alquila entre 2 y 6 unidades (departamentos, habitaciones, locales) lleva los contratos en papel y los pagos en una hoja de cálculo o en el chat familiar. Las consecuencias son concretas:

- Nadie sabe al instante **quién está atrasado ni cuánto se debe**. Hay que revisar mensajes y vouchers de Yape o transferencias.
- Los abonos parciales se pierden: "me pagó S/ 500 de los S/ 1 200 de octubre" queda en la memoria de una sola persona.
- Se puede ofrecer la misma unidad a dos inquilinos para fechas que se cruzan.
- Al terminar un contrato no hay un corte claro de lo que quedó pendiente.

## Valor del producto

Un solo lugar donde el administrador familiar ve, por cada propiedad, **quién la ocupa, si el mes está pagado y cuánto se adeuda**, con la mora calculada automáticamente a partir de las fechas del contrato. Las reglas de negocio evitan los errores de arriba: contratos que se solapan, pagos fuera del período o por encima del alquiler, y terminar un contrato con saldo pendiente.

## Actores

| Actor | Descripción |
|---|---|
| Administrador familiar | Persona de la familia que registra propiedades, contratos y pagos. Único usuario del sistema en esta versión. |
| Arrendatario | Inquilino. **No usa el sistema**: sus datos se guardan dentro del contrato. |

## Supuestos

- Una instalación sirve a **una familia**: todo usuario autenticado administra todas las propiedades (no hay multi-tenant ni roles).
- Moneda única: **soles (S/, PEN)**.
- Zona horaria de negocio: **America/Lima (UTC−5, sin horario de verano)**. "Hoy" siempre se evalúa en hora de Lima.
- Cada mes del contrato vale el alquiler completo (sin prorrateo del primer o último mes).

## Dentro del alcance (v1)

RF-01 a RF-07 (ver [02-requerimientos.md](02-requerimientos.md)): propiedades, contratos con datos del arrendatario, pagos con abonos parciales, estado de cuenta con mora, término de contrato y resumen de la cartera.

## Fuera del alcance (mejoras pendientes)

- Reajuste anual del alquiler.
- Recordatorios automáticos por email o WhatsApp (el sistema ya tiene `IEmailSender` como punto de extensión).
- Portal del arrendatario y roles diferenciados.
- Adelanto, devolución de garantía y descuentos por daños.
- Prorrateo de meses incompletos.
- Gastos de servicios (luz, agua, mantenimiento).
- Adjuntar el contrato firmado (PDF) o los vouchers.

## Glosario (español ↔ código)

| Negocio | Código | Nota |
|---|---|---|
| Propiedad / unidad | `Property` | Departamento, casa, habitación o local. |
| Contrato de alquiler | `Lease` | Incluye los datos del arrendatario. |
| Arrendatario / inquilino | `Tenant*` (campos de `Lease`) | No es una entidad aparte. |
| Pago / abono | `Payment` | Un período puede tener varios abonos. |
| Período | `Period` (`YYYY-MM`) | Mes calendario que cubre un pago. |
| Día de pago | `PaymentDay` | Día del mes (1–28) en que vence cada período. |
| Garantía | `Deposit` | |
| Estado de cuenta | `Statement` | Calculado al vuelo, no se persiste. |
| Mora / atraso | `Overdue` | Período vencido con saldo. |
