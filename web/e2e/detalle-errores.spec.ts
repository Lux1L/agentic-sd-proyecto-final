import { test, expect } from '@playwright/test'

// Solo lectura sobre "Local comercial A": ambas acciones son rechazadas por la API y no mutan datos.
test.describe('Detalle con deuda: errores de negocio', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Resumen' })).toBeVisible()
    await page.getByRole('link', { name: /Local comercial A/ }).click()
    await expect(page.getByText('Días de atraso')).toBeVisible()
  })

  test('HU-04 RN-14: un pago mayor al saldo es rechazado con un toast', async ({ page }) => {
    await page.getByRole('button', { name: /registrar pago/i }).click()
    await page.getByLabel(/monto/i).fill('99999')
    await page.getByRole('button', { name: /^registrar$/i }).click()
    await expect(page.getByText(/PaymentExceedsRent|excede|supera/i).first()).toBeVisible()
    await expect(page.getByText('Pago registrado')).toHaveCount(0)
  })

  test('HU-06 RN-11: terminar con saldo pendiente es rechazado y el contrato sigue activo', async ({ page }) => {
    await page.getByRole('button', { name: /terminar contrato/i }).click()
    await page.getByRole('alertdialog').getByRole('button', { name: /terminar contrato/i }).click()
    await expect(page.getByText(/HasOutstandingBalance|saldo pendiente/i).first()).toBeVisible()
    await expect(page.getByText('Contrato terminado')).toHaveCount(0)
    await expect(page.getByRole('button', { name: /registrar pago/i })).toBeVisible()
  })
})
