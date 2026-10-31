// HU-03 (RF-03)
import { readFileSync } from 'node:fs'
import { test, expect, type APIRequestContext } from '@playwright/test'
import { API, crearPropiedad, hoyLima } from './helpers'

const auth = () => ({ Authorization: `Bearer ${JSON.parse(readFileSync('.auth/token.json', 'utf8')).accessToken}` })

/** Garantiza al menos un contrato Terminado: crea propiedad + contrato pagado al día y lo termina. */
async function asegurarTerminado(request: APIRequestContext) {
  const r = await request.get(`${API}/leases?status=Terminated&pageSize=1`, { headers: auth() })
  if ((await r.json()).totalCount > 0) return
  const { id: propertyId } = await crearPropiedad(request)
  const hoy = hoyLima()
  const l = await request.post(`${API}/leases`, {
    headers: auth(),
    data: {
      propertyId, tenantFullName: 'Inquilino Terminado E2E', tenantDocumentType: 'Dni', tenantDocumentNumber: '45678912',
      tenantPhone: '987654321', startDate: hoy, endDate: hoyLima(1), monthlyRent: 1000, deposit: 1000, paymentDay: 5,
    },
  })
  expect(l.ok()).toBeTruthy()
  const leaseId = (await l.json()) as string
  const p = await request.post(`${API}/leases/${leaseId}/payments`, {
    headers: auth(), data: { period: hoy.slice(0, 7), amount: 1000, paidOn: hoy, method: 'Cash' },
  })
  expect(p.ok()).toBeTruthy()
  expect((await request.post(`${API}/leases/${leaseId}/terminate`, { headers: auth() })).ok()).toBeTruthy()
}

test.describe('Pantalla de contratos', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/contratos')
    await expect(page.getByRole('heading', { name: 'Contratos' })).toBeVisible()
    await expect(page.getByRole('row').nth(1)).toBeVisible()
  })

  test('la tabla lista contratos con sus columnas', async ({ page }) => {
    await expect(page.locator('thead th')).toHaveText(['Propiedad', 'Arrendatario', 'Documento', 'Vigencia', 'Alquiler', 'Estado'])
    expect(await page.getByRole('row').count()).toBeGreaterThan(1)
    await expect(page.getByText(/^\d{2}\/\d{2}\/\d{4} – \d{2}\/\d{2}\/\d{4}$/).first()).toBeVisible()
  })

  test('el filtro Terminados muestra solo filas con badge Terminado', async ({ page, request }) => {
    await asegurarTerminado(request)
    await page.goto('/contratos')
    await page.getByRole('combobox', { name: /estado/i }).click()
    await page.getByRole('option', { name: 'Terminados' }).click()
    await expect(page.getByText('Terminado').first()).toBeVisible()
    await expect(page.getByText('Activo', { exact: true })).toHaveCount(0)
    const filas = await page.getByRole('row').count()
    await expect(page.getByText('Terminado', { exact: true })).toHaveCount(filas - 1)
  })

  test('la búsqueda por "Rímac" encuentra "Inversiones Rímac SAC"', async ({ page }) => {
    await page.getByLabel('Buscar').fill('Rímac')
    await expect(page.getByRole('cell', { name: 'Inversiones Rímac SAC' })).toBeVisible()
    await expect(page.getByRole('cell', { name: 'Inversiones Rímac SAC' })).toHaveCount(1)
  })

  test('un resultado vacío muestra el estado vacío', async ({ page }) => {
    await page.getByLabel('Buscar').fill('zzz-no-existe')
    await expect(page.getByText('No hay contratos')).toBeVisible()
  })

  test('clic en una fila navega al detalle y Enter también', async ({ page }) => {
    await page.getByRole('row').nth(1).click()
    await expect(page).toHaveURL(/\/leases\/[0-9a-f-]{36}$/)
    await page.goBack()
    await page.getByRole('row').nth(1).focus()
    await page.keyboard.press('Enter')
    await expect(page).toHaveURL(/\/leases\/[0-9a-f-]{36}$/)
  })

  test('la paginación avanza si hay más de 10, o queda en una sola página', async ({ page, request }) => {
    const total = (await (await request.get(`${API}/leases?pageSize=1`, { headers: auth() })).json()).totalCount as number
    const sigPag = page.getByRole('button', { name: /next|siguiente/i })
    await expect(sigPag.first()).toBeVisible()
    if (total > 10) {
      await expect(page.getByRole('row')).toHaveCount(11)
      await sigPag.first().click()
      await expect(page.getByRole('row')).toHaveCount(Math.min(total - 10, 10) + 1)
    } else {
      await expect(sigPag.first()).toBeDisabled()
    }
  })
})
