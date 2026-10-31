import { test, expect } from '@playwright/test'
import { sinSesion } from './helpers'

test.describe('Login', () => {
  test.use({ storageState: sinSesion })

  test('credenciales correctas llevan al Resumen', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: /iniciar sesión/i }).click()
    await expect(page).toHaveURL(/\/$/)
    await expect(page.getByRole('heading', { name: 'Resumen' })).toBeVisible()
  })

  test('credenciales incorrectas muestran un toast de error y siguen en /login', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel('Contraseña').fill('Incorrecta123!')
    await page.getByRole('button', { name: /iniciar sesión/i }).click()
    // Mensaje genérico en español: no revela si el correo existe.
    await expect(page.getByText(/Correo o contraseña incorrectos/i).first()).toBeVisible()
    await expect(page).toHaveURL(/\/login$/)
  })
})
