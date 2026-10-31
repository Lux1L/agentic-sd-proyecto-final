---
name: add-e2e
description: Agrega o actualiza pruebas E2E de Playwright del frontend (web/e2e) para una pantalla o flujo, o repara la suite cuando un cambio de UI la rompe. Úsala cuando el usuario pida una prueba E2E, un spec de Playwright, cubrir un flujo en Chrome, o cuando `pnpm e2e` falle tras tocar la UI.
argument-hint: <pantalla o flujo a cubrir, p. ej. "registrar pago" o "arreglar detalle-errores.spec.ts">
---

# Agregar pruebas E2E (Playwright sobre Chrome)

Los specs viven en `web/e2e/*.spec.ts`. Cada regla de abajo previene un defecto que la suite ya tuvo (E1–E7 en `docs/ai-evidence/02-decisiones-y-revision.md`). Antes de escribir, lee `web/e2e/helpers.ts` y el spec más parecido al tuyo.

## Cómo se corre

1. Levanta la API (`http://localhost:5080`) y Vite (`http://localhost:5173`). `playwright.config.ts` no tiene `webServer`: si no están arriba, todo falla.
2. `cd web && pnpm e2e`. Un spec: `pnpm e2e e2e/resumen.spec.ts`.
3. Usa el **Google Chrome instalado** (`channel: 'chrome'`), con `locale: 'es-PE'` y `timezoneId: 'America/Lima'`. No ejecutes `playwright install`.
4. La URL base sale de `E2E_BASE_URL` (por defecto `http://localhost:5173`); la leen `playwright.config.ts` y `global-setup.ts`. Para correr la suite contra otro Vite (p. ej. desde un worktree): `E2E_BASE_URL=http://localhost:5174 pnpm e2e`. La API sigue en 5080.
5. Al terminar de probar, `./scripts/reset-demo.sh` limpia las propiedades "E2E …" (detén la API antes; ver `docs/guion-demo.md`).

## Reglas

### Login y rate limit (E4)
La API permite 10 logins por minuto. `e2e/global-setup.ts` hace **un** login por corrida y guarda `.auth/state.json` (storageState, ya aplicado en la config: cada test arranca con sesión) y `.auth/token.json` (para llamadas por API). Por eso:

- Nunca agregues un login por test ni un `beforeEach` que inicie sesión.
- Solo `login.spec.ts` prueba el login por la UI; usa `test.use({ storageState: sinSesion })` (de `helpers.ts`) para arrancar sin sesión.

### Aislamiento de datos
Todo test que **muta** datos crea su propia propiedad por API:

```ts
crearPropiedad(request: APIRequestContext): Promise<{ id: string; name: string }>  // name = `E2E ${Date.now()}`
```

Las propiedades de demo ("Local comercial A", etc.) son **solo lectura**, o sirven para acciones que la API rechaza sin mutar (sobrepago RN-14, terminar con deuda RN-11). Si necesitas un contrato, créalo por la UI sobre tu propiedad. La segunda corrida seguida debe pasar con los datos que dejó la primera.

### Robustez ante datos concurrentes
Varios agentes o procesos pueden correr E2E a la vez contra la misma base. No supongas estado global: nada de "no hay resultados" ni "la lista tiene N filas" sobre datos compartidos. Para un estado vacío busca un término único del test (`zzz-sin-resultados-${Date.now()}`); para datos, crea los tuyos. Para listas de opciones usa `await expect(page.getByRole('option')).toHaveText([...])`, que reintenta, en vez de leer `allTextContents()` una sola vez.

### Selectores
Accesibles y en español: `getByRole`, `getByLabel`, `getByText`. Sin CSS, salvo el calendario (dentro de `elegirFecha`). Nada de `waitForTimeout`: espera con `expect(...).toBeVisible()` y `toHaveURL`.

Controles propios (detalle y ejemplos en [references/controles.md](references/controles.md)):

- **DateField**: `elegirFecha(page, /^inicio:/i, iso)`. Para leer la fecha mostrada: `ddmmaaaa(iso)` y `hoyLima(sumarAnios?)`.
- **Select de Kumo**: `getByLabel(...).click()`, luego **espera `getByRole('option').first()` visible** antes de leer o contar opciones (sin eso se leen cero opciones), y elige con `getByRole('option', { name: /^5$/ })`.
- **Diálogos**: `page.getByRole('alertdialog').getByRole('button', ...)`.
- **Toasts**: `expect(page.getByText(/mensaje/i).first()).toBeVisible()`; un `.first()` evita fallos por texto duplicado.

### Fechas (E1)
Todo flujo que muestre o envíe una fecha por defecto necesita un test con otra zona horaria que verifique que es la de Lima: `test.use({ timezoneId: 'Asia/Tokyo' })` y comparar con `ddmmaaaa(hoyLima())`. Modelo: `fecha-lima.spec.ts`. Los tests con Lima por defecto no detectan este defecto.

### Mensajes de error en español
Los asserts de errores de negocio verifican el código (`PaymentExceedsRent`) **y** el `detail` en español ("excede la renta del período"), para que un mensaje sin traducir haga fallar el test.

### Defecto de la app vs defecto del test (E2)
Si un test falla porque la app se comporta mal (mensaje en inglés, código de error equivocado, dato incorrecto), **no adaptes la aserción para que pase**: reproduce con curl, reporta el defecto y deja el test en rojo hasta que se corrija la app.

## Trazabilidad
Encima de **cada** `test(...)`, un comentario `// HU-xx (RF-xx)`, `// RN-xx` o `// RNF-03` (sesión y autorización) (o el prefijo en el título, como hace `detalle-errores.spec.ts`), y títulos en español. Registra el spec en la fila correspondiente de `specs/technical/03-trazabilidad.md` (esa matriz hoy no tiene referencias a `web/e2e`; añade ahí el nombre del spec en la columna Pruebas).

## Plantilla mínima

```ts
import { test, expect } from '@playwright/test'
import { crearPropiedad } from './helpers'

test.describe('Nueva pantalla', () => {
  // HU-xx (RF-xx): descripción en español
  test('HU-xx: hace X y muestra Y', async ({ page, request }) => {
    const prop = await crearPropiedad(request)        // propiedad propia, no la de demo
    await page.goto(`/properties/${prop.id}/new-lease`) // la sesión ya viene del storageState
    await page.getByLabel(/nombre completo/i).fill('María Quispe Huamán')
    await page.getByRole('button', { name: /guardar contrato/i }).click()
    await expect(page).toHaveURL(/\/leases\/[0-9a-f-]+$/)
  })
})
```

## Terminado cuando

- `cd web && pnpm e2e` pasa **dos veces seguidas** (la segunda prueba que el spec es repetible con los datos que quedaron).
- No hay `test.only` ni `waitForTimeout` (`grep -rn "test.only\|waitForTimeout" web/e2e`).
- `pnpm run build` y `pnpm run lint` en `web/` están limpios.
- El spec está en la matriz de trazabilidad.
