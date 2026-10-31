const TOKEN_KEY = 'accessToken'

export const getToken = () => localStorage.getItem(TOKEN_KEY)
export const setToken = (t: string) => localStorage.setItem(TOKEN_KEY, t)
export const clearToken = () => localStorage.removeItem(TOKEN_KEY)

export class ApiError extends Error {
  status: number
  code: string
  detail: string
  /** Errores de validación: { campo: [mensajes] } con el campo en camelCase. */
  errors: Record<string, string[]>
  constructor(status: number, code: string, detail: string, errors: Record<string, string[]>) {
    super(detail || code)
    this.status = status
    this.code = code
    this.detail = detail
    this.errors = errors
  }
}

export async function api<T = unknown>(
  path: string,
  options: { method?: string; body?: unknown } = {},
): Promise<T> {
  const token = getToken()
  const res = await fetch(`/api${path}`, {
    method: options.method ?? 'GET',
    headers: {
      ...(options.body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  })

  if (res.status === 401 && path !== '/users/login') {
    clearToken()
    window.location.assign('/login')
    throw new ApiError(401, 'Unauthorized', 'Sesión expirada.', {})
  }

  const text = await res.text()
  const data = text ? JSON.parse(text) : null
  if (!res.ok) {
    throw new ApiError(
      res.status,
      data?.title ?? 'Error',
      data?.detail ?? 'Ocurrió un error inesperado.',
      data?.errors ?? {},
    )
  }
  return data as T
}

/** Errores de validación por clave (en minúsculas). Solo se consideran los campos conocidos del formulario. */
export function parseValidation(err: unknown, known: string[]): { fields: Record<string, string>; unmapped: string[] } {
  const fields: Record<string, string> = {}
  const unmapped: string[] = []
  if (!(err instanceof ApiError)) return { fields, unmapped }
  const knownLower = known.map((k) => k.toLowerCase())
  for (const [k, v] of Object.entries(err.errors ?? {})) {
    const key = k.toLowerCase()
    if (knownLower.includes(key)) fields[key] = v.join(' ')
    else unmapped.push(...v)
  }
  return { fields, unmapped }
}

// ---- Tipos ----
export type PropertyType = 'Apartment' | 'House' | 'Room' | 'Commercial'
export type PeriodStatus = 'Paid' | 'Partial' | 'Overdue' | 'Pending'

export interface PortfolioProperty {
  propertyId: string
  name: string
  type: PropertyType
  status: 'Available' | 'Leased' | 'Inactive'
  leaseId?: string | null
  tenantFullName?: string | null
  monthlyRent?: number | null
  currentPeriodStatus?: PeriodStatus | null
  /** Abonado en el período del mes actual (null si no hay contrato o el mes está fuera del contrato). */
  currentPeriodPaid?: number | null
  overdueDebt: number
}
export interface PortfolioSummary {
  today: string
  totalProperties: number
  leasedProperties: number
  totalOverdueDebt: number
  properties: PortfolioProperty[]
}
export interface Lease {
  id: string
  propertyId: string
  propertyName: string
  tenantFullName: string
  tenantDocumentType: string
  tenantDocumentNumber: string
  tenantPhone: string
  tenantEmail?: string | null
  startDate: string
  endDate: string
  monthlyRent: number
  deposit: number
  paymentDay: number
  status: string
  terminatedOn?: string | null
}
export interface StatementPeriod {
  period: string
  dueDate: string
  paid: number
  balance: number
  status: PeriodStatus
  daysOverdue: number
}
export interface Statement {
  leaseId: string
  today: string
  monthlyRent: number
  overdueDebt: number
  outstandingBalance: number
  periods: StatementPeriod[]
}
