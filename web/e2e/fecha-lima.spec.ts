import { test, expect } from '@playwright/test'
import { ddmmaaaa, hoyLima } from './helpers'

// RN-18: "hoy" es la fecha de Lima aunque el navegador esté en otra zona horaria.
test.describe('Fecha de negocio en zona horaria distinta de Lima', () => {
  test.use({ timezoneId: 'Asia/Tokyo' })

  // RN-18
  test('RN-18: la fecha de pago por defecto es la de Lima', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('link', { name: /Local comercial A/ }).click()
    await page.getByRole('button', { name: /registrar pago/i }).click()
    await expect(page.getByRole('button', { name: /fecha de pago/i })).toHaveText(ddmmaaaa(hoyLima()))
  })

  // RN-18 (HU-04 desde el inicio, RF-07): solo abre el dialog, no registra nada sobre la demo.
  test('RN-18: registrar pago desde el inicio también propone la fecha de Lima', async ({ page }) => {
    await page.goto('/')
    const fila = page.getByRole('region', { name: /^Por cobrar ·/ }).getByRole('listitem').filter({ hasText: 'Local comercial A' })
    await fila.getByRole('button', { name: /registrar pago/i }).click()
    await expect(page.getByRole('dialog').getByRole('button', { name: /fecha de pago/i })).toHaveText(ddmmaaaa(hoyLima()))
  })
})
