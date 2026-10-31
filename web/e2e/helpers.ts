import { readFileSync } from 'node:fs'
import { expect, type APIRequestContext, type Page } from '@playwright/test'

export const API = 'http://localhost:5080'
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
