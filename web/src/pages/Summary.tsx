import { useCallback, useEffect, useId, useState, type ReactNode } from 'react'
import { Button, Empty, Grid, LayerCard, Link, Loader, Meter, Surface, Text } from '@cloudflare/kumo'
import { MoneyIcon, PlusIcon } from '@phosphor-icons/react'
import { useNavigate } from 'react-router-dom'
import { api, type PortfolioProperty, type PortfolioSummary } from '../api'
import { dayOfMonth, formatDayMonth, formatLongDate, formatMoney, monthName, monthShort, propertyTypeLabel } from '../format'
import { StatusBadge } from '../components/StatusBadge'
import { RegisterPaymentDialog } from '../components/RegisterPaymentDialog'
import { useErrorHandler } from '../errors'

const days = (n: number) => `${n} ${n === 1 ? 'día' : 'días'}`
const pct = (part: number, total: number) => (total > 0 ? Math.min(100, Math.round((part / total) * 100)) : 0)

/** Mes actual no pagado completo (solo si el mes está dentro del contrato). */
const monthUnpaid = (p: PortfolioProperty) =>
  p.currentPeriodPaid != null && p.monthlyRent != null && p.currentPeriodPaid < p.monthlyRent

/** Por cobrar: mayor deuda vencida, luego más días de atraso, luego vencimiento más cercano. */
function byUrgency(a: PortfolioProperty, b: PortfolioProperty) {
  if (b.overdueDebt !== a.overdueDebt) return b.overdueDebt - a.overdueDebt
  if (b.maxDaysOverdue !== a.maxDaysOverdue) return b.maxDaysOverdue - a.maxDaysOverdue
  const da = a.currentPeriodDueDate ?? '9999-12-31'
  const db = b.currentPeriodDueDate ?? '9999-12-31'
  return da < db ? -1 : da > db ? 1 : 0
}

/** Línea que explica la situación del contrato, solo con lo que da la API. */
function situation(p: PortfolioProperty, period: string): { text: string; meter?: number } {
  const rent = p.monthlyRent ?? 0
  const paid = p.currentPeriodPaid
  const month = monthName(period)
  const abono = paid != null && paid > 0 ? `abonó ${formatMoney(paid)} de ${formatMoney(rent)}` : null
  const meter = paid != null && paid > 0 && paid < rent ? pct(paid, rent) : undefined
  // El mes actual todavía por vencer, para mencionarlo junto a la deuda de meses anteriores.
  const currentDue = p.currentPeriodDueDate && monthUnpaid(p) && !p.overduePeriods.includes(period)
    ? `${month} vence el ${dayOfMonth(p.currentPeriodDueDate)}`
    : null
  const n = p.overduePeriods.length

  if (n >= 2) {
    const list = p.overduePeriods.map(monthShort).join(', ')
    return { text: [`${n} meses sin pagar (${list})`, `el más antiguo venció hace ${days(p.maxDaysOverdue)}`, currentDue].filter(Boolean).join(' · ') }
  }
  if (n === 1) {
    const overdue = p.overduePeriods[0]
    if (overdue === period && p.currentPeriodDueDate) {
      return {
        text: [`${month} venció el ${dayOfMonth(p.currentPeriodDueDate)}`, abono ?? 'sin abonos', `${days(p.maxDaysOverdue)} de atraso`].join(' · '),
        meter,
      }
    }
    return { text: [`${monthName(overdue)} venció hace ${days(p.maxDaysOverdue)}`, currentDue].filter(Boolean).join(' · ') }
  }
  if (monthUnpaid(p)) {
    const due = p.currentPeriodDueDate ? `vence el ${formatDayMonth(p.currentPeriodDueDate)}` : `${month} pendiente`
    return { text: [due, abono].filter(Boolean).join(' · '), meter }
  }
  const next = p.nextDueDate ? `próximo vence el ${formatDayMonth(p.nextDueDate)}` : null
  const head = paid != null ? `${month} pagado completo` : 'sin cobro este mes'
  return { text: [head, next].filter(Boolean).join(' · ') }
}

function Group({ title, count, dimmed, children }: { title: string; count: number; dimmed?: boolean; children: ReactNode }) {
  const id = useId()
  return (
    <section aria-labelledby={id} className={`flex flex-col gap-2 ${dimmed ? 'opacity-60' : ''}`}>
      <Text as="h2" variant="heading">
        <span id={id}>{title} <Text as="span" variant="secondary">· {count}</Text></span>
      </Text>
      <LayerCard className="p-0">
        <ul className="divide-y divide-kumo-line">{children}</ul>
      </LayerCard>
    </section>
  )
}

function Row({ main, aside, action }: { main: ReactNode; aside?: ReactNode; action?: ReactNode }) {
  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-3 px-5 py-4">
      <div className="min-w-0 flex-[999_1_320px]">{main}</div>
      {aside && <div className="flex flex-[1_1_140px] flex-col items-end gap-0.5 text-right">{aside}</div>}
      {action}
    </li>
  )
}

function LeaseRow({ p, period, upToDate, onPay }: { p: PortfolioProperty; period: string; upToDate: boolean; onPay?: () => void }) {
  const s = situation(p, period)
  const status = p.overdueDebt > 0 ? 'Overdue' : p.currentPeriodStatus
  return (
    <Row
      main={
        <Link
          href={`/leases/${p.leaseId}`}
          variant="plain"
          className="group flex min-w-0 flex-col gap-1 rounded-md text-kumo-default hover:text-kumo-default focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-kumo-focus"
        >
          <span className="flex flex-wrap items-center gap-2">
            <span className="group-hover:underline"><Text as="span" bold>{p.name}</Text></span>
            {status && <StatusBadge status={status} />}
          </span>
          <Text variant="secondary">{p.tenantFullName} · {s.text}</Text>
          {s.meter != null && (
            <div className="max-w-80 pt-1">
              <Meter label="Abonado del mes" value={s.meter} />
            </div>
          )}
        </Link>
      }
      aside={
        <>
          <Text variant={p.overdueDebt > 0 ? 'error' : upToDate ? 'success' : 'secondary'} bold>{formatMoney(p.overdueDebt)}</Text>
          <Text variant="secondary">alquiler {formatMoney(p.monthlyRent)}</Text>
        </>
      }
      action={onPay && (
        <Button variant="primary" icon={MoneyIcon} aria-label={`Registrar pago: ${p.name}`} onClick={onPay}>
          Registrar pago
        </Button>
      )}
    />
  )
}

export default function Summary() {
  const navigate = useNavigate()
  const handle = useErrorHandler()
  const [data, setData] = useState<PortfolioSummary | null>(null)
  const [payLeaseId, setPayLeaseId] = useState<string | null>(null)
  const [payOpen, setPayOpen] = useState(false)

  const load = useCallback(async () => {
    try {
      setData(await api<PortfolioSummary>('/portfolio/summary'))
    } catch (e) {
      handle(e)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => { load() }, [load])

  if (!data) return <div className="flex justify-center p-12"><Loader size={32} /></div>

  const period = data.today.slice(0, 7)
  const leased = data.properties.filter((p) => !!p.leaseId)
  const toCollect = leased.filter((p) => p.overdueDebt > 0 || monthUnpaid(p)).sort(byUrgency)
  const upToDate = leased.filter((p) => !toCollect.includes(p))
  const available = data.properties.filter((p) => !p.leaseId && p.status === 'Available')
  const inactive = data.properties.filter((p) => !p.leaseId && p.status === 'Inactive')

  // Cobranza del mes: solo contratos cuyo mes actual está dentro de la vigencia.
  const billed = leased.filter((p) => p.currentPeriodPaid != null && p.monthlyRent != null)
  const expected = billed.reduce((sum, p) => sum + (p.monthlyRent ?? 0), 0)
  const collected = billed.reduce((sum, p) => sum + Math.min(p.currentPeriodPaid ?? 0, p.monthlyRent ?? 0), 0)
  const paidInFull = billed.filter((p) => !monthUnpaid(p)).length
  const debtors = leased.filter((p) => p.overdueDebt > 0)
  const oldest = Math.max(0, ...debtors.map((p) => p.maxDaysOverdue))
  const hasDebt = data.totalOverdueDebt > 0

  const openPayment = (leaseId: string) => {
    setPayLeaseId(leaseId)
    setPayOpen(true)
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <Text as="h1" variant="heading" size="lg">Resumen</Text>
        <Text variant="secondary">Hoy, {formatLongDate(data.today)}</Text>
      </div>

      {/* Excepción consciente a kumo-design content-text-size: KPI para demo (ver docs/ai-evidence) */}
      <Grid variant="2-1" gap="base">
        <Surface className="flex flex-col gap-3 rounded-lg px-5 py-4">
          <Text variant="secondary">Cobranza de {monthName(period)}</Text>
          <div className="flex flex-wrap items-baseline gap-2">
            <div className="text-3xl font-semibold text-kumo-default">{formatMoney(collected)}</div>
            <Text variant="secondary">cobrado de {formatMoney(expected)}</Text>
          </div>
          <Meter label="Avance del mes" value={pct(collected, expected)} />
          <Text variant="secondary">
            {expected > collected
              ? <>Faltan <Text as="span" bold>{formatMoney(expected - collected)}</Text></>
              : 'Mes cobrado completo'}
            {` · ${paidInFull} de ${billed.length} ${billed.length === 1 ? 'contrato' : 'contratos'} ${paidInFull === 1 ? 'pagó' : 'pagaron'} completo`}
          </Text>
        </Surface>
        <Surface className={`flex flex-col gap-1.5 rounded-lg px-5 py-4 ${hasDebt ? 'ring-kumo-danger bg-kumo-danger/10' : ''}`}>
          <Text variant="secondary">Deuda vencida</Text>
          <div className={`text-3xl font-semibold ${hasDebt ? 'text-kumo-danger' : 'text-kumo-default'}`}>{formatMoney(data.totalOverdueDebt)}</div>
          {hasDebt ? (
            <Text variant="error">
              en {debtors.length} {debtors.length === 1 ? 'contrato' : 'contratos'} · la más antigua, {days(oldest)}
            </Text>
          ) : (
            <Text variant="success">Ningún contrato tiene pagos vencidos</Text>
          )}
        </Surface>
      </Grid>

      {data.properties.length === 0 && (
        <Empty title="Aún no hay propiedades" description="Cuando registres propiedades aparecerán aquí." />
      )}

      {toCollect.length > 0 && (
        <Group title="Por cobrar" count={toCollect.length}>
          {toCollect.map((p) => (
            <LeaseRow key={p.propertyId} p={p} period={period} upToDate={false} onPay={() => openPayment(p.leaseId!)} />
          ))}
        </Group>
      )}

      {upToDate.length > 0 && (
        <Group title="Al día" count={upToDate.length}>
          {upToDate.map((p) => <LeaseRow key={p.propertyId} p={p} period={period} upToDate />)}
        </Group>
      )}

      {available.length > 0 && (
        <Group title="Disponibles" count={available.length}>
          {available.map((p) => (
            <Row
              key={p.propertyId}
              main={
                <div className="flex flex-col gap-1">
                  <Text bold>{p.name}</Text>
                  <Text variant="secondary">{propertyTypeLabel[p.type] ?? p.type} · sin arrendatario</Text>
                </div>
              }
              action={
                <Button variant="secondary" icon={PlusIcon} aria-label={`Nuevo contrato: ${p.name}`}
                  onClick={() => navigate(`/properties/${p.propertyId}/new-lease`)}>
                  Nuevo contrato
                </Button>
              }
            />
          ))}
        </Group>
      )}

      {inactive.length > 0 && (
        <Group title="Inactivas" count={inactive.length} dimmed>
          {inactive.map((p) => (
            <Row
              key={p.propertyId}
              main={
                <div className="flex flex-col gap-1">
                  <span className="flex flex-wrap items-center gap-2">
                    <Text as="span" bold>{p.name}</Text>
                    <StatusBadge status="Inactive" />
                  </span>
                  <Text variant="secondary">{propertyTypeLabel[p.type] ?? p.type}</Text>
                </div>
              }
            />
          ))}
        </Group>
      )}

      <RegisterPaymentDialog leaseId={payLeaseId} open={payOpen} onOpenChange={setPayOpen} onRegistered={load} />
    </div>
  )
}
