import { readFileSync } from 'node:fs'
import { expect, type APIRequestContext, type Page } from '@playwright/test'

export const API = process.env.E2E_API_URL ?? 'http://localhost:5080'
export const EMAIL = 'admin@arriendo.pe'
export const PASSWORD = 'Demo1234!'

/** Estado sin sesión (para probar login y redirecciones). */
export const sinSesion = { cookies: [], origins: [] }

/** Crea por API una propiedad propia de la prueba (no toca las de demo). */
export async function crearPropiedad(request: APIRequestContext): Promise<{ id: string; name: string }> {
  const { accessToken } = JSON.parse(readFileSync('.auth/token.json', 'utf8'))
  const name = `E2E ${Date.now()}`
  const p = await request.post(`${API}/properties`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { name, address: 'Av. Prueba 123, Lima', type: 'Apartment' },
  })
  expect(p.ok()).toBeTruthy()
  return { id: (await p.json()) as string, name }
}

/** Fecha de hoy en Lima (yyyy-MM-dd), sumando años opcionalmente. */
export function hoyLima(sumarAnios = 0): string {
  const [y, m, d] = new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Lima' }).format(new Date()).split('-').map(Number)
  return `${String(y + sumarAnios).padStart(4, '0')}-${String(m).padStart(2, '0')}-${String(d).padStart(2, '0')}`
}

/** "YYYY-MM-DD" -> "DD/MM/YYYY" (formato que muestra DateField). */
export const ddmmaaaa = (iso: string) => iso.split('-').reverse().join('/')

/** Abre el DateField con ese label, navega hasta el mes de `iso` y hace clic en el día. */
export async function elegirFecha(page: Page, label: RegExp, iso: string) {
  await page.getByRole('button', { name: label }).click()
  const dia = page.locator(`td[data-day="${iso}"]:not(.rdp-outside) button`)
  const visible = page.locator('.rdp-root td[data-day]:not(.rdp-outside)').first()
  await expect(visible).toBeVisible()
  for (let i = 0; i < 60 && (await dia.count()) === 0; i++) {
    const mes = ((await visible.getAttribute('data-day')) ?? '').slice(0, 7)
    await page.getByRole('button', { name: iso.slice(0, 7) > mes ? /mes siguiente/i : /mes anterior/i }).click()
    await expect(page.locator(`.rdp-root td[data-day^="${mes}"]:not(.rdp-outside)`).first()).toBeHidden()
  }
  await dia.click()
  await expect(page.locator('.rdp-root')).toHaveCount(0)
}

/** Llena el formulario de nuevo contrato (inicio hoy, fin en un año, S/ 1,800, día de pago 5). */
export async function llenarContrato(page: Page, tipo: 'RUC' | 'DNI', doc: string) {
  await page.getByLabel(/nombre completo/i).fill('María Quispe Huamán')
  await page.getByLabel(/tipo de documento/i).click()
  await page.getByRole('option', { name: new RegExp(`^${tipo}$`) }).click()
  await page.getByLabel(/número de documento/i).fill(doc)
  await page.getByLabel(/celular/i).fill('987654321')
  await elegirFecha(page, /^inicio:/i, hoyLima())
  await elegirFecha(page, /^fin:/i, hoyLima(1))
  await page.getByLabel(/alquiler mensual/i).fill('1800')
  await page.getByLabel(/garantía/i).fill('3600')
  await page.getByLabel(/día de pago/i).click()
  await page.getByRole('option', { name: /^5$/ }).click()
}
