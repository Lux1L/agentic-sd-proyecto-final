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

/** "2026-10-05" -> Date local (medianoche en la zona del navegador, sin pasar por UTC). */
export const isoToDate = (iso: string): Date | undefined => {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso)
  return m ? new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3])) : undefined
}
/** Date local -> "2026-10-05" con getFullYear/getMonth/getDate (nunca toISOString). */
export const dateToIso = (d: Date) =>
  `${String(d.getFullYear()).padStart(4, '0')}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`

const monthFmt = new Intl.DateTimeFormat('es-PE', { month: 'long', year: 'numeric' })
/** "2026-10" -> "octubre 2026" (Date local, sin desfase de zona horaria). */
export const periodLabel = (p: string) => {
  const [y, m] = p.split('-').map(Number)
  return monthFmt.format(new Date(y, m - 1, 1))
}
/** Meses "YYYY-MM" desde startDate hasta endDate (ambos "YYYY-MM-DD"), inclusive. */
export const periodsBetween = (startIso: string, endIso: string): string[] => {
  let [y, m] = startIso.split('-').map(Number)
  const [ey, em] = endIso.split('-').map(Number)
  const out: string[] = []
  while (y < ey || (y === ey && m <= em)) {
    out.push(`${y}-${String(m).padStart(2, '0')}`)
    if (++m > 12) { m = 1; y++ }
  }
  return out
}
/** Período por defecto: el actual si está en la lista; si no, el último que ya pasó (o el primero). */
export const defaultPeriod = (periods: string[]) => {
  const cur = currentPeriod()
  if (periods.includes(cur)) return cur
  const past = periods.filter((p) => p < cur)
  return past.length ? past[past.length - 1] : periods[0] ?? cur
}

const longDateFmt = new Intl.DateTimeFormat('es-PE', { day: 'numeric', month: 'long', year: 'numeric' })
/** "2026-10-31" -> "31 de octubre de 2026" (Date local, sin desfase de zona horaria). */
export const formatLongDate = (iso: string) => {
  const d = isoToDate(iso)
  return d ? longDateFmt.format(d) : '—'
}
const dayMonthFmt = new Intl.DateTimeFormat('es-PE', { day: 'numeric', month: 'long' })
/** "2026-11-05" -> "5 de noviembre". */
export const formatDayMonth = (iso: string) => {
  const d = isoToDate(iso)
  return d ? dayMonthFmt.format(d) : '—'
}
/** "2026-10-10" -> 10 */
export const dayOfMonth = (iso: string) => Number(iso.slice(8, 10))

const monthNames = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'setiembre', 'octubre', 'noviembre', 'diciembre']
const monthShortNames = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'set', 'oct', 'nov', 'dic']
/** "2026-10" -> "octubre" */
export const monthName = (p: string) => monthNames[Number(p.slice(5, 7)) - 1] ?? p
/** "2026-09" -> "set" (abreviatura usada en Perú) */
export const monthShort = (p: string) => monthShortNames[Number(p.slice(5, 7)) - 1] ?? p
