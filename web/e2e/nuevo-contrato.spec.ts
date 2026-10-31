import { test, expect } from '@playwright/test'
import { crearPropiedad, llenarContrato } from './helpers'

test.describe('Nuevo contrato sobre propiedad propia', () => {
  // HU-02 (RF-02), HU-04 (RF-04), HU-05 (RF-05); RN-05, RN-09, RN-15
  test('HU-02/04/05: valida documento, crea contrato, abona parcial y rechaza solapamiento', async ({ page, request }) => {
    const prop = await crearPropiedad(request)
    await test.step('RN-09: RUC de persona natural con número inválido muestra error bajo el campo', async () => {
      await page.goto(`/properties/${prop.id}/new-lease`)
      await llenarContrato(page, 'RUC', '20601234560')
      await page.getByRole('button', { name: /guardar contrato/i }).click()
      await expect(page.getByText(/documento|RUC|dígitos/i).filter({ hasNotText: /^Número de documento$|^Tipo de documento$/ }).first()).toBeVisible()
      await expect(page).toHaveURL(/new-lease$/)
    })

    await test.step('RN-15: corregir a DNI guarda y navega al detalle', async () => {
      await page.getByLabel(/tipo de documento/i).click()
      await page.getByRole('option', { name: /^DNI$/ }).click()
      await page.getByLabel(/número de documento/i).fill('45678912')
      await page.getByRole('button', { name: /guardar contrato/i }).click()
      await expect(page).toHaveURL(/\/leases\/[0-9a-f-]+$/)
      await expect(page.getByText('Días de atraso')).toBeVisible()
    })

    await test.step('HU-04: el período actual queda Pendiente', async () => {
      const fila = page.getByRole('row').nth(1)
      await expect(fila.getByText('Pendiente')).toBeVisible()
    })

    await test.step('HU-04/05: un abono de 800 deja el período en Parcial', async () => {
      await page.getByRole('button', { name: /registrar pago/i }).click()
      await page.getByLabel(/monto/i).fill('800')
      await page.getByRole('button', { name: /^registrar$/i }).click()
      await expect(page.getByText('Pago registrado')).toBeVisible()
      await expect(page.getByRole('row').nth(1).getByText('Parcial')).toBeVisible()
    })

    await test.step('RN-05: un segundo contrato solapado es rechazado', async () => {
      await page.goto(`/properties/${prop.id}/new-lease`)
      await llenarContrato(page, 'DNI', '45678913')
      await page.getByRole('button', { name: /guardar contrato/i }).click()
      await expect(page.getByText(/Overlap/).first()).toBeVisible()
      await expect(page.getByText(/Ya existe un contrato vigente para esta propiedad que se cruza con las fechas indicadas/).first()).toBeVisible()
      await expect(page).toHaveURL(/new-lease$/)
    })
  })
})
