import { mkdirSync, writeFileSync } from 'node:fs'
import { request } from '@playwright/test'
import { API, EMAIL, PASSWORD } from './helpers'

// El login está limitado a 10/min en la API: se inicia sesión una sola vez por corrida
// y el token se reutiliza (storageState y .auth/token.json para los fixtures por API).
export default async function globalSetup() {
  const ctx = await request.newContext()
  const r = await ctx.post(`${API}/users/login`, { data: { email: EMAIL, password: PASSWORD } })
  if (!r.ok()) throw new Error(`Login por API falló: ${r.status()}`)
  const { accessToken } = await r.json()
  await ctx.dispose()
  mkdirSync('.auth', { recursive: true })
  writeFileSync('.auth/token.json', JSON.stringify({ accessToken }))
  writeFileSync('.auth/state.json', JSON.stringify({
    cookies: [],
    origins: [{ origin: process.env.E2E_BASE_URL ?? 'http://localhost:5173', localStorage: [{ name: 'accessToken', value: accessToken }] }],
  }))
}
