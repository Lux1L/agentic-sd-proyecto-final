import { test, expect } from '@playwright/test'

test.describe('Resumen (HU-07)', () => {
  test.beforeEach(async ({ page }) => { await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Resumen' })).toBeVisible() })

  // HU-07 (RF-07)
  test('muestra las tarjetas KPI', async ({ page }) => {
    await expect(page.getByText('Propiedades', { exact: true })).toBeVisible()
    await expect(page.getByText('Alquiladas', { exact: true })).toBeVisible()
    await expect(page.getByText('Deuda vencida total')).toBeVisible()
  })

  // HU-07 (RF-07)
  test('Local comercial A aparece atrasado con deuda y navega al detalle', async ({ page }) => {
    const tarjeta = page.getByRole('link', { name: /Local comercial A/ })
    await expect(tarjeta).toBeVisible()
    await expect(tarjeta.getByText('Atrasado')).toBeVisible()
    const deuda = (await tarjeta.getByText(/Deuda:/).innerText()).replace(/[^\d.,]/g, '').replace(/,/g, '')
    expect(Number(deuda)).toBeGreaterThan(0)
    await tarjeta.click()
    await expect(page).toHaveURL(/\/leases\/[0-9a-f-]+$/)
    await expect(page.getByText('Días de atraso')).toBeVisible()
  })
})
