# Guion de la demo (≈ 3 minutos)

## Antes de presentar: dejar los datos de demo limpios

Todo lo que se cree durante los ensayos queda guardado en la base. Para volver al estado inicial:

```bash
# 1. Detén la API si está corriendo (Ctrl+C)
./scripts/reset-demo.sh            # borra solo la base local de este repo y levanta Postgres
dotnet run --project src/Web.Api   # migra y vuelve a cargar los datos de demo
cd web && pnpm dev                 # http://localhost:5173
```

> **Importante:** la suite E2E (`pnpm e2e`) crea propiedades "E2E …" en la base. Corre el reset **después** de la última corrida de E2E.

Credenciales: `admin@arriendo.pe` / `Demo1234!` (ya precargadas en el login).

Las fechas de los datos de demo se calculan desde "hoy" en hora de Lima, así que la mora siempre se ve.

## Pasos

| # | Acción | Qué se ve | Regla |
|---|---|---|---|
| 1 | Iniciar sesión → **Resumen** | 4 propiedades y deuda vencida total destacada en rojo | RF-07, RN-16/17 |
| 2 | Clic en **Local comercial A** | 2 meses pagados y 3 atrasados, con días de atraso | RF-05, RN-15..18 |
| 3 | **Registrar pago** con un monto mayor al saldo de un período | Toast `Leases.PaymentExceedsRent` con el saldo restante | RN-14 |
| 4 | **Terminar contrato** → confirmar | Toast `Leases.HasOutstandingBalance` con el monto | RN-11 |
| 5 | Volver → **Casa Chorrillos → Nuevo contrato**, tipo RUC `20601234560` | Error en español debajo de "Número de documento" | RN-09 (módulo 11) |
| 6 | Cambiar a DNI `45678912`, inicio = hoy, fin = +1 año, alquiler 1800, garantía 3600, día 5 → Guardar | Detalle del contrato; el período actual vence hoy y queda **Pendiente** | RN-15 |
| 7 | Registrar un abono de S/ 800 | El período pasa a **Parcial** | RN-16 |
| 8 | Abrir http://localhost:5080/scalar/v1 | 14 rutas documentadas en OpenAPI; probar `GET /portfolio/summary` | RNF-01 |

## Si algo falla en vivo

- Respaldo: las capturas del recorrido están en el deck (láminas 3 y 10).
- La API responde en `GET /health`. Si cae, `dotnet run --project src/Web.Api`.
