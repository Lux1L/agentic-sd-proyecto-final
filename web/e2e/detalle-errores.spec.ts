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

  test('RN-13: el Select de período solo ofrece meses dentro del contrato', async ({ page }) => {
    const vigencia = await page.getByText(/^\d{2}\/\d{2}\/\d{4} – \d{2}\/\d{2}\/\d{4}$/).textContent()
    const [ini, fin] = vigencia!.split(' – ').map((d) => d.split('/').reverse().map(Number)) // [y, m, d]
    const esperados: string[] = []
    const fmt = new Intl.DateTimeFormat('es-PE', { month: 'long', year: 'numeric' })
    for (let y = ini[0], m = ini[1]; y < fin[0] || (y === fin[0] && m <= fin[1]); m++ > 11 && ((m = 1), y++)) {
      esperados.push(fmt.format(new Date(y, m - 1, 1)))
    }
    await page.getByRole('button', { name: /registrar pago/i }).click()
    await page.getByLabel(/^período$/i).click()
    await expect(page.getByRole('option').first()).toBeVisible()
    const opciones = (await page.getByRole('option').allTextContents()).map((t) => t.trim())
    expect(opciones).toEqual(esperados)
    const antes = fmt.format(new Date(ini[0], ini[1] - 2, 1))
    const despues = fmt.format(new Date(fin[0], fin[1], 1))
    await expect(page.getByRole('option', { name: antes, exact: true })).toHaveCount(0)
    await expect(page.getByRole('option', { name: despues, exact: true })).toHaveCount(0)
  })
})
