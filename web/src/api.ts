const TOKEN_KEY = 'accessToken'

export const getToken = () => localStorage.getItem(TOKEN_KEY)
export const setToken = (t: string) => localStorage.setItem(TOKEN_KEY, t)
export const clearToken = () => localStorage.removeItem(TOKEN_KEY)

export interface ValidationItem { code?: string; description: string }

export class ApiError extends Error {
  status: number
  code: string
  detail: string
  /** Mapa { Campo: [mensajes] } (contrato) o lista [{ code, description }] (lo que devuelve la API real). */
  errors: Record<string, string[]> | ValidationItem[]
  constructor(status: number, code: string, detail: string, errors: Record<string, string[]> | ValidationItem[]) {
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
      data?.errors ?? [],
    )
  }
  return data as T
}

// La API real devuelve la lista de errores sin nombre de campo: se infiere por el texto del mensaje.
const hints: [RegExp, string][] = [
  [/número de documento/i, 'tenantdocumentnumber'],
  [/teléfono/i, 'tenantphone'],
  [/fecha de fin/i, 'enddate'],
  [/garantía/i, 'deposit'],
  [/período/i, 'period'],
]
function inferField(msg: string): string | null {
  for (const [re, f] of hints) if (re.test(msg)) return f
  const q = msg.match(/^'([^']+)'/) // FluentValidation: 'Tenant Full Name' must not be empty.
  return q ? q[1].replace(/\s+/g, '').toLowerCase() : null
}

/** Errores de validación por campo (claves en minúsculas) y los mensajes que no se pudieron asignar a un campo. */
export function parseValidation(err: unknown): { fields: Record<string, string>; unmapped: string[] } {
  const fields: Record<string, string> = {}
  const unmapped: string[] = []
  if (!(err instanceof ApiError)) return { fields, unmapped }
  const add = (k: string, m: string) => { fields[k] = fields[k] ? `${fields[k]} ${m}` : m }
  if (Array.isArray(err.errors)) {
    for (const e of err.errors) {
      const f = inferField(e.description)
      if (f) add(f, e.description)
      else unmapped.push(e.description)
    }
  } else {
    for (const [k, v] of Object.entries(err.errors ?? {})) add((k.split('.').pop() ?? k).toLowerCase(), v.join(' '))
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
