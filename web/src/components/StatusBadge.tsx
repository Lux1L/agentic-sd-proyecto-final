import { Badge } from '@cloudflare/kumo'

const map: Record<string, { label: string; variant: 'success' | 'warning' | 'error' | 'neutral' | 'info' }> = {
  Paid: { label: 'Pagado', variant: 'success' },
  Partial: { label: 'Parcial', variant: 'warning' },
  Overdue: { label: 'Atrasado', variant: 'error' },
  Pending: { label: 'Pendiente', variant: 'info' },
  Available: { label: 'Disponible', variant: 'neutral' },
  Inactive: { label: 'Inactiva', variant: 'neutral' },
}

export function StatusBadge({ status }: { status?: string | null }) {
  const s = map[status ?? 'Available'] ?? map.Available
  return <Badge variant={s.variant}>{s.label}</Badge>
}
