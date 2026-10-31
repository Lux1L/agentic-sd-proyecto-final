import { test, expect, type Page } from '@playwright/test'
import { crearPropiedad, llenarContrato } from './helpers'

const GRUPOS = ['Por cobrar', 'Al día', 'Disponibles', 'Inactivas']

/** Sección de un grupo del inicio (su nombre accesible es el título: "Por cobrar · 2"). */
const grupo = (page: Page, nombre: string) => page.getByRole('region', { name: new RegExp(`^${nombre} ·`) })
/** Fila de un grupo que contiene el texto (nombre de la propiedad). */
const fila = (page: Page, nombre: string, texto: string) => grupo(page, nombre).getByRole('listitem').filter({ hasText: texto })

test.describe('Resumen: bandeja de cobranza (HU-07)', () => {
  // HU-07 (RF-07)
  test('muestra la fecha de hoy en Lima y las tarjetas de cobranza del mes y deuda vencida', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Resumen' })).toBeVisible()
    const hoy = new Intl.DateTimeFormat('es-PE', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'America/Lima' }).format(new Date())
    await expect(page.getByText(`Hoy, ${hoy}`)).toBeVisible()
    await expect(page.getByText(/^Cobranza de [a-z]+$/)).toBeVisible()
    await expect(page.getByText(/cobrado de S\/\s?[\d,.]+/)).toBeVisible()
    await expect(page.getByText(/\d+ de \d+ contratos? (pagó|pagaron) completo/)).toBeVisible()
    // "Local comercial A" (demo) tiene deuda vencida: la tarjeta la resume.
    await expect(page.getByText('Deuda vencida', { exact: true })).toBeVisible()
    await expect(page.getByText(/^en \d+ contratos? · la más antigua, \d+ días?$/)).toBeVisible()
  })

  // HU-07 (RF-07)
  test('agrupa en orden Por cobrar, Al día, Disponibles e Inactivas, cada grupo con su conteo', async ({ page, request }) => {
    const prop = await crearPropiedad(request) // asegura el grupo Disponibles
    await page.goto('/')
    await expect(fila(page, 'Disponibles', prop.name)).toBeVisible()
    await expect(fila(page, 'Por cobrar', 'Local comercial A')).toBeVisible()

    const presentes = (await page.getByRole('heading', { level: 2 }).allTextContents())
      .map((t) => t.split(' · ')[0].trim())
      .filter((t) => GRUPOS.includes(t))
    expect(presentes).toEqual(GRUPOS.filter((g) => presentes.includes(g)))

    for (const g of presentes) {
      const titulo = await grupo(page, g).getByRole('heading', { level: 2 }).textContent()
      await expect(grupo(page, g).getByRole('listitem')).toHaveCount(Number(titulo!.split('·')[1]))
    }

    await fila(page, 'Disponibles', prop.name).getByRole('button', { name: /nuevo contrato/i }).click()
    await expect(page).toHaveURL(new RegExp(`/properties/${prop.id}/new-lease$`))
  })

  // HU-07 (RF-07); RN-17
  test('Local comercial A explica sus meses atrasados y su fila es un enlace al contrato', async ({ page }) => {
    await page.goto('/')
    const local = fila(page, 'Por cobrar', 'Local comercial A')
    await expect(local.getByText('Atrasado', { exact: true })).toBeVisible()
    await expect(local).toContainText(/\d+ meses sin pagar \([a-z]{3}(, [a-z]{3})+\) · el más antiguo venció hace \d+ días/)
    const deuda = (await local.getByText(/^S\/\s?[\d,.]+$/).first().innerText()).replace(/[^\d.]/g, '')
    expect(Number(deuda)).toBeGreaterThan(0)
    await local.getByRole('link', { name: /Local comercial A/ }).click()
    await expect(page).toHaveURL(/\/leases\/[0-9a-f-]+$/)
    await expect(page.getByText('Días de atraso', { exact: true })).toBeVisible()
  })

  // HU-04 (RF-04), HU-07 (RF-07); RN-16
  test('registrar un pago desde el inicio mueve el contrato de Por cobrar a Al día', async ({ page, request }) => {
    const prop = await crearPropiedad(request)
    await page.goto(`/properties/${prop.id}/new-lease`)
    await llenarContrato(page, 'DNI', '45678912')
    await page.getByRole('button', { name: /guardar contrato/i }).click()
    await expect(page).toHaveURL(/\/leases\/[0-9a-f-]+$/)

    await page.goto('/')
    const pendiente = fila(page, 'Por cobrar', prop.name)
    await expect(pendiente.getByText('Pendiente', { exact: true })).toBeVisible()
    await expect(pendiente).toContainText(/vence el \d{1,2} de [a-z]+/)
    // Orden de Por cobrar: la mayor deuda vencida (Local comercial A) va antes que un mes sin vencer.
    const textos = await grupo(page, 'Por cobrar').getByRole('listitem').allTextContents()
    expect(textos.findIndex((t) => t.includes('Local comercial A'))).toBeLessThan(textos.findIndex((t) => t.includes(prop.name)))

    await pendiente.getByRole('button', { name: /registrar pago/i }).click()
    const dialog = page.getByRole('dialog')
    await expect(dialog.getByText(prop.name)).toBeVisible() // el contrato se carga al abrir
    await dialog.getByLabel(/monto/i).fill('1800')
    await dialog.getByRole('button', { name: /^registrar$/i }).click()
    await expect(page.getByText('Pago registrado').first()).toBeVisible()

    await expect(fila(page, 'Por cobrar', prop.name)).toHaveCount(0)
    const alDia = fila(page, 'Al día', prop.name)
    await expect(alDia.getByText('Pagado', { exact: true })).toBeVisible()
    await expect(alDia).toContainText(/[a-z]+ pagado completo/)
  })
})
