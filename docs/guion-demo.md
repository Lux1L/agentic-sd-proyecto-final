# Guion de la demo (≈ 3 minutos)

## Antes de presentar: dejar los datos de demo limpios

Todo lo que se cree durante los ensayos queda guardado en la base. Para volver al estado inicial:

```bash
# Todo en Docker (base nueva con los datos de demo):
./scripts/reset-demo.sh app        # → http://localhost:5173

# O para desarrollar (API y frontend fuera de Docker):
./scripts/reset-demo.sh            # solo Postgres
dotnet run --project src/Web.Api   # migra y vuelve a cargar los datos de demo
cd web && pnpm dev                 # http://localhost:5173
```

> **Importante:** la suite E2E (`pnpm e2e`) crea propiedades "E2E …" en la base. Corre el reset **después** de la última corrida de E2E.

Credenciales: `admin@arriendo.pe` / `Demo1234!` (ya precargadas en el login).

Las fechas de los datos de demo se calculan desde "hoy" en hora de Lima, así que la mora siempre se ve.

## Pasos

| # | Acción | Qué se ve | Regla |
|---|---|---|---|
| 1 | Iniciar sesión → **Resumen** (bandeja de cobranza) | Cobrado en octubre S/ 1,800.00 de S/ 4,950.00 y S/ 8,750.00 vencidos en 2 contratos | RF-07, RN-16/17/19 |
| 2 | Leer la primera fila de **Por cobrar** | Local comercial A: "3 meses sin pagar (ago, set, oct) · el más antiguo venció hace 91 días" | RF-07, RN-19 |
| 3 | **Registrar pago** en esa misma fila con un monto mayor al saldo | Toast `Leases.PaymentExceedsRent` con el saldo restante | RN-14 |
| 4 | Clic en la fila → detalle → **Terminar contrato** → confirmar | Toast `Leases.HasOutstandingBalance` con el monto | RN-11 |
| 5 | Volver → **Disponibles → Casa Chorrillos → Nuevo contrato**, tipo RUC `20601234560` | Error en español debajo de "Número de documento" | RN-09 (módulo 11) |
| 6 | Cambiar a DNI `45678912`, inicio = hoy, fin = +1 año, alquiler 1800, garantía 3600, día 5 → Guardar | Detalle del contrato; el período actual vence hoy y queda **Pendiente** | RN-15 |
| 7 | Volver al inicio → **Registrar pago** de S/ 800 en la fila de Casa Chorrillos | La fila muestra lo abonado del mes | RN-16 |
| 7b | Menú **Contratos** → filtro "Terminados" y buscar "Rímac" | Tabla paginada con filtros (Kumo `Table` + `Pagination`) | RF-03 / HU-03 |
| 8 | Abrir http://localhost:5080/scalar/v1 | Contrato OpenAPI; probar `GET /portfolio/summary` | RNF-01 |

## Si algo falla en vivo

- Respaldo: las capturas del recorrido están en el deck (láminas 3 y 12).
- La API responde en `GET /health`. Si cae, `dotnet run --project src/Web.Api`.
