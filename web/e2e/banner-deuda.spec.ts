import { test, expect } from '@playwright/test'

// Solo lectura sobre "Local comercial A" (contrato activo con deuda vencida).
test.describe('Banner de deuda y día de pago', () => {
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

  test('RN-08: el Select de día de pago solo ofrece 1 a 28 (por defecto 5)', async ({ page, request }) => {
    const { crearPropiedad } = await import('./helpers')
    const prop = await crearPropiedad(request)
    await page.goto(`/properties/${prop.id}/new-lease`)
    const dia = page.getByLabel(/día de pago/i)
    await expect(dia).toHaveText('5')
    await dia.click()
    await expect(page.getByRole('option').first()).toBeVisible()
    const opciones = (await page.getByRole('option').allTextContents()).map((t) => t.trim())
    expect(opciones).toEqual(Array.from({ length: 28 }, (_, i) => String(i + 1)))
    await expect(page.getByRole('option', { name: '0', exact: true })).toHaveCount(0)
    await expect(page.getByRole('option', { name: '29', exact: true })).toHaveCount(0)
  })
})
