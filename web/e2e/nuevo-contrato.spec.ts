import { test, expect, type Page } from '@playwright/test'
import { crearPropiedad, elegirFecha, hoyLima } from './helpers'

async function llenarContrato(page: Page, tipo: 'RUC' | 'DNI', doc: string) {
  await page.getByLabel(/nombre completo/i).fill('María Quispe Huamán')
  await page.getByLabel(/tipo de documento/i).click()
  await page.getByRole('option', { name: new RegExp(`^${tipo}$`) }).click()
  await page.getByLabel(/número de documento/i).fill(doc)
  await page.getByLabel(/celular/i).fill('987654321')
  await elegirFecha(page, /^inicio:/i, hoyLima())
  await elegirFecha(page, /^fin:/i, hoyLima(1))
  await page.getByLabel(/alquiler mensual/i).fill('1800')
  await page.getByLabel(/garantía/i).fill('3600')
  await page.getByLabel(/día de pago/i).fill('5')
}

test.describe('Nuevo contrato sobre propiedad propia', () => {
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
      await expect(page.getByText(/Overlap|solap|traslap/i).first()).toBeVisible()
      await expect(page).toHaveURL(/new-lease$/)
    })
  })
})
