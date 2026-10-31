const money = new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' })
export const formatMoney = (n: number | null | undefined) => money.format(n ?? 0)

/** "2026-10-05" -> "05/10/2026" (sin pasar por Date para evitar desfases de zona horaria). */
export const formatDate = (iso?: string | null) => {
  if (!iso) return '—'
  const [y, m, d] = iso.slice(0, 10).split('-')
  return `${d}/${m}/${y}`
}
/** "2026-10" -> "10/2026" */
export const formatPeriod = (p: string) => {
  const [y, m] = p.split('-')
  return `${m}/${y}`
}
/**
 * "Hoy" en hora de Lima (RN-18), no en la zona del navegador: la API valida las fechas
 * de negocio con America/Lima. En-CA formatea como YYYY-MM-DD.
 */
export const todayIso = () =>
  new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Lima' }).format(new Date())
export const currentPeriod = () => todayIso().slice(0, 7)

export const propertyTypeLabel: Record<string, string> = {
  Apartment: 'Departamento',
  House: 'Casa',
  Room: 'Habitación',
  Commercial: 'Local comercial',
}
export const docTypeLabel: Record<string, string> = { Dni: 'DNI', Ce: 'CE', Ruc: 'RUC' }
