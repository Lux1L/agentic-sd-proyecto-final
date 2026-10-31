import { readFileSync } from 'node:fs'
import { expect, type APIRequestContext } from '@playwright/test'

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
