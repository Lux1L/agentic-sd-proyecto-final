import { test, expect } from '@playwright/test'
import { crearPropiedad } from './helpers'

// Solo lectura sobre "Local comercial A" (contrato activo con deuda vencida).
test.describe('Banner de deuda y día de pago', () => {
  // HU-05 (RF-05): estado de cuenta con deuda vencida
  test('Local comercial A muestra el Banner de deuda con monto y períodos', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('link', { name: /Local comercial A/ }).click()
    await expect(page.getByText('Días de atraso')).toBeVisible()
    const banner = page.getByText(/Este contrato tiene S\/.*vencidos en \d+ períodos?/)
    await expect(banner).toBeVisible()
    const texto = (await banner.innerText()).match(/vencidos en (\d+)/)
    const filasVencidas = await page.getByRole('row').filter({ hasText: 'Atrasado' }).count()
    expect(Number(texto![1])).toBe(filasVencidas)
  })

  // RN-08
  test('RN-08: el Select de día de pago solo ofrece 1 a 28 (por defecto 5)', async ({ page, request }) => {
    const prop = await crearPropiedad(request)
    await page.goto(`/properties/${prop.id}/new-lease`)
    const dia = page.getByLabel(/día de pago/i)
    await expect(dia).toHaveText('5')
    await dia.click()
    await expect(page.getByRole('option').first()).toBeVisible()
    await expect(page.getByRole('option')).toHaveText(Array.from({ length: 28 }, (_, i) => String(i + 1)))
    await expect(page.getByRole('option', { name: '0', exact: true })).toHaveCount(0)
    await expect(page.getByRole('option', { name: '29', exact: true })).toHaveCount(0)
  })
})
