import { test, expect } from '@playwright/test'
import { sinSesion } from './helpers'

test.describe('Sesión', () => {
  // RNF-03: sesión y autorización
  test('Cerrar sesión vuelve a /login', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Resumen' })).toBeVisible()
    await page.getByRole('button', { name: /cerrar sesión/i }).click()
    await expect(page).toHaveURL(/\/login$/)
  })

  test.describe('sin token', () => {
    test.use({ storageState: sinSesion })
    // RNF-03: sesión y autorización
    test('entrar a / sin token redirige a /login', async ({ page }) => {
      await page.goto('/')
      await expect(page).toHaveURL(/\/login$/)
    })
  })
})
