# Controles propios en las pruebas E2E

Patrones tomados de `web/e2e/` (`nuevo-contrato.spec.ts`, `detalle-errores.spec.ts`, `banner-deuda.spec.ts`, `fecha-lima.spec.ts`).

## DateField (Popover + DatePicker de Kumo, en español)

El disparador es un botón cuyo nombre empieza con el label ("Inicio: 30/10/2026"). `elegirFecha` abre, navega por "mes siguiente/anterior" hasta el mes y hace clic en el día `td[data-day="yyyy-MM-dd"]`.

```ts
await elegirFecha(page, /^inicio:/i, hoyLima())
await elegirFecha(page, /^fin:/i, hoyLima(1))          // +1 año
await expect(page.getByRole('button', { name: /fecha de pago/i })).toHaveText(ddmmaaaa(hoyLima()))
```

## Select de Kumo

Las opciones se montan de forma asíncrona tras el clic. Si lees `allTextContents()` de inmediato, puede devolver `[]`.

```ts
await page.getByLabel(/día de pago/i).click()
await expect(page.getByRole('option').first()).toBeVisible()   // espera obligatoria
const opciones = (await page.getByRole('option').allTextContents()).map((t) => t.trim())
await expect(page.getByRole('option', { name: '29', exact: true })).toHaveCount(0)
```

Para elegir una opción sin leer la lista, `getByRole('option', { name: /^DNI$/ }).click()` basta (Playwright espera la opción). Usa regex anclada (`^...$`) para no coincidir con prefijos ("5" vs "15").

## Diálogos de confirmación

```ts
await page.getByRole('button', { name: /terminar contrato/i }).click()
await page.getByRole('alertdialog').getByRole('button', { name: /terminar contrato/i }).click()
```

Acotar al `alertdialog` evita pulsar el botón de la página con el mismo nombre.

## Toasts y errores de la API

`await expect(page.getByText(/Correo o contraseña incorrectos/i).first()).toBeVisible()`. Afirma el mensaje en español que ve el usuario. Verifica también que no apareció el éxito (`toHaveCount(0)`) y que la URL no cambió.
