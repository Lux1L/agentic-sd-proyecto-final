# 03 · Historias de usuario y criterios de aceptación

Formato: *Como … quiero … para …*, con escenarios Gherkin. Cada escenario apunta a una regla (RN) y a un test (ver la matriz de trazabilidad).

---

## HU-01 · Gestionar propiedades (RF-01)

**Como** administrador familiar **quiero** registrar y mantener mis propiedades **para** tener el inventario de lo que alquila la familia.

```gherkin
Escenario: Registrar una propiedad válida
  Dado que estoy autenticado
  Cuando registro "Dpto 201" en "Av. Arequipa 1234, Lince" de tipo Departamento
  Entonces recibo el id de la propiedad
  Y la propiedad queda en estado Disponible

Escenario: Datos obligatorios (RN-01)
  Cuando registro una propiedad sin nombre
  Entonces recibo 400 con el error de validación del campo "Name"

Escenario: No inactivar con contrato activo (RN-02)
  Dado que "Dpto 201" tiene un contrato activo
  Cuando intento inactivarla
  Entonces recibo 409 "Properties.HasActiveLease"

Escenario: No eliminar con historial (RN-03)
  Dado que "Dpto 201" tuvo un contrato
  Cuando intento eliminarla
  Entonces recibo 409 "Properties.HasLeaseHistory"
```

## HU-02 · Registrar un contrato (RF-02)

**Como** administrador **quiero** registrar el contrato de un nuevo inquilino **para** que el sistema calcule sus vencimientos desde el primer día.

```gherkin
Escenario: Contrato válido
  Dado la propiedad Disponible "Dpto 201"
  Cuando registro un contrato del 2026-01-01 al 2026-12-31 por S/ 1 200 con garantía S/ 2 400, día de pago 5, arrendatario con DNI "45678912"
  Entonces recibo el id del contrato
  Y la propiedad pasa a Alquilada (RN-10)

Escenario: Fechas solapadas (RN-05)
  Dado que "Dpto 201" tiene un contrato activo del 2026-01-01 al 2026-12-31
  Cuando registro otro contrato del 2026-06-01 al 2027-05-31 sobre "Dpto 201"
  Entonces recibo 409 "Leases.Overlap"

Escenario: Propiedad inactiva (RN-04)
  Cuando registro un contrato sobre una propiedad Inactiva
  Entonces recibo 409 "Properties.Inactive"

Escenario: Garantía excesiva (RN-07)
  Cuando registro un contrato de S/ 1 000 con garantía S/ 3 000
  Entonces recibo 400 de validación en "Deposit"

Escenario: Documento inválido (RN-09)
  Cuando registro un arrendatario con RUC "20601234560" (dígito verificador incorrecto)
  Entonces recibo 400 de validación en "TenantDocumentNumber"
```

## HU-03 · Consultar contratos (RF-03)

**Como** administrador **quiero** listar los contratos por propiedad y estado **para** encontrar rápido el de un inquilino.

```gherkin
Escenario: Filtrar contratos activos de una propiedad
  Dado dos contratos de "Dpto 201", uno Activo y uno Terminado
  Cuando listo los contratos con propertyId de "Dpto 201" y estado Activo
  Entonces recibo una página con 1 contrato
```

## HU-04 · Registrar un pago (RF-04)

**Como** administrador **quiero** registrar cada abono que recibo (efectivo, transferencia, Yape o Plin) **para** no depender de los vouchers del chat.

```gherkin
Escenario: Abono parcial
  Dado un contrato activo de S/ 1 200
  Cuando registro S/ 500 por Yape para el período 2026-10
  Entonces el pago queda registrado

Escenario: Sobrepago del período (RN-14)
  Dado que el período 2026-10 ya tiene S/ 1 000 abonados de S/ 1 200
  Cuando registro S/ 300 para 2026-10
  Entonces recibo 400 "Leases.PaymentExceedsRent"

Escenario: Período fuera del contrato (RN-13)
  Dado un contrato de 2026-01 a 2026-12
  Cuando registro un pago para 2027-02
  Entonces recibo 400 "Leases.PeriodOutsideLease"

Escenario: Contrato terminado (RN-13)
  Cuando registro un pago en un contrato Terminado
  Entonces recibo 409 "Leases.NotActive"
```

## HU-05 · Ver el estado de cuenta (RF-05)

**Como** administrador **quiero** ver mes a mes qué está pagado y qué está atrasado **para** cobrar con datos exactos.

```gherkin
Escenario: Mora calculada (RN-15, RN-16, RN-17)
  Dado un contrato desde 2026-08-01, S/ 1 200, día de pago 5
  Y pagos completos de agosto y septiembre, y S/ 500 de octubre
  Y hoy es 2026-10-20 en Lima
  Entonces agosto y septiembre están Pagados
  Y octubre está Atrasado con 15 días de atraso
  Y la deuda vencida es S/ 700

Escenario: Primer período de un contrato que empieza después del día de pago (RN-15)
  Dado un contrato desde 2026-10-30 con día de pago 5
  Y hoy es 2026-10-30
  Entonces el período 2026-10 vence el 2026-10-30 y está Pendiente, no Atrasado

Escenario: Medianoche en Lima (RN-18)
  Dado un período que vence el 2026-10-05
  Y el reloj marca 2026-10-06T03:00Z (22:00 del 5 de octubre en Lima)
  Entonces el período todavía NO está atrasado
```

## HU-06 · Terminar un contrato (RF-06)

**Como** administrador **quiero** cerrar un contrato cuando el inquilino se va **para** liberar la propiedad.

```gherkin
Escenario: Terminar con deuda (RN-11)
  Dado un contrato con saldo pendiente de S/ 700
  Cuando intento terminarlo
  Entonces recibo 409 "Leases.HasOutstandingBalance"

Escenario: Terminar al día (RN-11)
  Dado un contrato sin saldo pendiente
  Cuando lo termino
  Entonces queda Terminado y la propiedad vuelve a Disponible
```

## HU-07 · Resumen de la cartera (RF-07)

**Como** administrador **quiero** ver en una sola pantalla todas las propiedades con su inquilino, el estado del mes y la deuda **para** saber a quién llamar hoy.

```gherkin
Escenario: Resumen con una propiedad atrasada
  Dado tres propiedades: una al día, una con abono parcial no vencido y una atrasada
  Cuando consulto el resumen
  Entonces veo las tres con su estado del mes: Pagado, Parcial y Atrasado
  Y la deuda vencida total de la cartera es la suma de las deudas atrasadas

Escenario: Bandeja de cobranza con varios meses atrasados
  Dado un contrato con día de pago 1 que solo pagó junio y julio
  Y hoy es 31 de octubre de 2026
  Cuando consulto el resumen
  Entonces veo los períodos atrasados 2026-08, 2026-09 y 2026-10
  Y 91 días de atraso máximos
  Y el vencimiento del mes actual es el 1 de octubre y el próximo, el 1 de noviembre
```
