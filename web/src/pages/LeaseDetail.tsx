import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Banner, Breadcrumbs, Button, Dialog, Empty, Grid, InputGroup, LayerCard, Loader, Select, Surface, Table, Text, useKumoToastManager } from '@cloudflare/kumo'
import { InfoIcon, MoneyIcon, WarningIcon, XCircleIcon } from '@phosphor-icons/react'
import { useParams } from 'react-router-dom'
import { api, type Lease, type Statement } from '../api'
import { defaultPeriod, docTypeLabel, formatDate, formatMoney, formatPeriod, periodLabel, periodsBetween, todayIso } from '../format'
import { DateField } from '../components/DateField'
import { StatusBadge } from '../components/StatusBadge'
import { useErrorHandler } from '../errors'

function Item({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <Text variant="secondary">{label}</Text>
      <Text bold>{value}</Text>
    </div>
  )
}

export default function LeaseDetail() {
  const { id } = useParams()
  const handle = useErrorHandler()
  const toasts = useKumoToastManager()
  const [lease, setLease] = useState<Lease | null>(null)
  const [st, setSt] = useState<Statement | null>(null)
  const [payOpen, setPayOpen] = useState(false)
  const [endOpen, setEndOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [pay, setPay] = useState({ period: '', amount: '', paidOn: todayIso(), method: 'Cash' })

  const load = useCallback(async () => {
    try {
      const [l, s] = await Promise.all([api<Lease>(`/leases/${id}`), api<Statement>(`/leases/${id}/statement`)])
      setLease(l)
      setSt(s)
    } catch (e) {
      handle(e)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  useEffect(() => { load() }, [load])

  // Solo los meses del contrato: la UI impide RN-13 (período fuera de la vigencia).
  const periodOptions = useMemo(() => (lease ? periodsBetween(lease.startDate, lease.endDate) : []), [lease])
  const periodItems = useMemo(() => Object.fromEntries(periodOptions.map((p) => [p, periodLabel(p)])), [periodOptions])
  const periodValue = pay.period && periodOptions.includes(pay.period) ? pay.period : defaultPeriod(periodOptions)

  async function submitPayment(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setErrors({})
    try {
      await api(`/leases/${id}/payments`, {
        method: 'POST',
        body: { period: periodValue, amount: pay.amount === '' ? 0 : Number(pay.amount), paidOn: pay.paidOn, method: pay.method },
      })
      toasts.add({ title: 'Pago registrado', description: `${formatMoney(Number(pay.amount))} del período ${formatPeriod(periodValue)}`, variant: 'success' })
      setPayOpen(false)
      setPay({ ...pay, amount: '' })
      await load()
    } catch (e2) {
      setErrors(handle(e2, ['period', 'amount', 'paidOn', 'method']))
    } finally {
      setBusy(false)
    }
  }

  async function terminate() {
    setBusy(true)
    try {
      await api(`/leases/${id}/terminate`, { method: 'POST' })
      toasts.add({ title: 'Contrato terminado', variant: 'success' })
      await load()
    } catch (e) {
      handle(e)
    } finally {
      setBusy(false)
      setEndOpen(false)
    }
  }

  if (!lease || !st) return <div className="flex justify-center p-12"><Loader size={32} /></div>
  const active = lease.status === 'Active'
  const overduePeriods = st.periods.filter((p) => p.status === 'Overdue').length

  return (
    <div className="flex flex-col gap-6">
      <Breadcrumbs>
        <Breadcrumbs.Link href="/">Resumen</Breadcrumbs.Link>
        <Breadcrumbs.Separator />
        <Breadcrumbs.Current>{lease.propertyName}</Breadcrumbs.Current>
      </Breadcrumbs>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Text as="h1" variant="heading" size="lg">
          {lease.propertyName} <Text as="span" variant="secondary" size="lg">· {lease.tenantFullName}</Text>
        </Text>
        {active ? (
          <div className="flex gap-2">
            <Button variant="primary" icon={MoneyIcon} onClick={() => { setErrors({}); setPayOpen(true) }}>Registrar pago</Button>
            <Button variant="secondary-destructive" icon={XCircleIcon} onClick={() => setEndOpen(true)}>Terminar contrato</Button>
          </div>
        ) : null}
      </div>

      {!active && (
        <Banner icon={<InfoIcon weight="fill" />}
          title={`Contrato terminado${lease.terminatedOn ? ` el ${formatDate(lease.terminatedOn)}` : ''}`} />
      )}

      <LayerCard>
        <LayerCard.Secondary>Contrato</LayerCard.Secondary>
        <LayerCard.Primary>
          <Grid variant="1-2-4up" gap="base">
            <Item label="Arrendatario" value={lease.tenantFullName} />
            <Item label="Documento" value={`${docTypeLabel[lease.tenantDocumentType] ?? lease.tenantDocumentType} ${lease.tenantDocumentNumber}`} />
            <Item label="Celular" value={lease.tenantPhone || '—'} />
            <Item label="Correo" value={lease.tenantEmail || '—'} />
            <Item label="Vigencia" value={`${formatDate(lease.startDate)} – ${formatDate(lease.endDate)}`} />
            <Item label="Alquiler mensual" value={formatMoney(lease.monthlyRent)} />
            <Item label="Garantía" value={formatMoney(lease.deposit)} />
            <Item label="Día de pago" value={String(lease.paymentDay)} />
          </Grid>
        </LayerCard.Primary>
      </LayerCard>

      <Grid variant="2up" gap="base">
        <Surface className={`rounded-lg px-5 py-4 ${st.overdueDebt > 0 ? 'ring-kumo-danger bg-kumo-danger/10' : ''}`}>
          <Text variant="secondary">Deuda vencida</Text>
          <div className={`mt-1.5 text-3xl font-semibold ${st.overdueDebt > 0 ? 'text-kumo-danger' : ''}`}>{formatMoney(st.overdueDebt)}</div>
        </Surface>
        <Surface className="rounded-lg px-5 py-4">
          <Text variant="secondary">Saldo pendiente</Text>
          <div className="mt-1.5 text-3xl font-semibold">{formatMoney(st.outstandingBalance)}</div>
        </Surface>
      </Grid>

      {active && st.overdueDebt > 0 && (
        <Banner variant="error" icon={<WarningIcon weight="fill" />}
          title={`Este contrato tiene ${formatMoney(st.overdueDebt)} vencidos en ${overduePeriods} ${overduePeriods === 1 ? 'período' : 'períodos'}`} />
      )}

      {st.periods.length === 0 ? (
        <Empty title="Sin períodos" description="Este contrato aún no tiene períodos en el estado de cuenta." />
      ) : (
      <LayerCard className="p-0">
        <Table>
          <Table.Header>
            <Table.Row>
              <Table.Head>Período</Table.Head>
              <Table.Head>Vence</Table.Head>
              <Table.Head>Pagado</Table.Head>
              <Table.Head>Saldo</Table.Head>
              <Table.Head>Estado</Table.Head>
              <Table.Head>Días de atraso</Table.Head>
            </Table.Row>
          </Table.Header>
          <Table.Body>
            {st.periods.map((p) => (
              <Table.Row key={p.period}>
                <Table.Cell>{formatPeriod(p.period)}</Table.Cell>
                <Table.Cell>{formatDate(p.dueDate)}</Table.Cell>
                <Table.Cell>{formatMoney(p.paid)}</Table.Cell>
                <Table.Cell>{formatMoney(p.balance)}</Table.Cell>
                <Table.Cell><StatusBadge status={p.status} /></Table.Cell>
                <Table.Cell>{p.daysOverdue > 0 ? p.daysOverdue : '—'}</Table.Cell>
              </Table.Row>
            ))}
          </Table.Body>
        </Table>
      </LayerCard>
      )}

      <Dialog.Root open={payOpen} onOpenChange={setPayOpen}>
        <Dialog size="lg" className="px-8 py-6">
          <Dialog.Title className="mb-6 text-xl font-semibold">Registrar pago</Dialog.Title>
          <form onSubmit={submitPayment} className="flex flex-col gap-4" noValidate>
            <Select className="w-full" label="Período" hideLabel={false} items={periodItems} value={periodValue} onValueChange={(v) => setPay({ ...pay, period: v as string })} error={errors.period}>
              {periodOptions.map((p) => <Select.Option key={p} value={p}>{periodLabel(p)}</Select.Option>)}
            </Select>
            <InputGroup label="Monto" error={errors.amount ? { message: errors.amount, match: true } : undefined}>
              <InputGroup.Addon>S/</InputGroup.Addon>
              <InputGroup.Input type="number" min="0" step="0.01" value={pay.amount} onChange={(e) => setPay({ ...pay, amount: e.target.value })} />
            </InputGroup>
            <DateField label="Fecha de pago" value={pay.paidOn} onChange={(v) => setPay({ ...pay, paidOn: v })} error={errors.paidon} />
            <Select className="w-full" label="Medio de pago" hideLabel={false} items={{ Cash: 'Efectivo', BankTransfer: 'Transferencia', Yape: 'Yape', Plin: 'Plin' }} value={pay.method} onValueChange={(v) => setPay({ ...pay, method: v as string })} error={errors.method}>
              <Select.Option value="Cash">Efectivo</Select.Option>
              <Select.Option value="BankTransfer">Transferencia</Select.Option>
              <Select.Option value="Yape">Yape</Select.Option>
              <Select.Option value="Plin">Plin</Select.Option>
            </Select>
            <div className="mt-2 flex justify-end gap-2">
              <Dialog.Close render={(p) => <Button {...p} type="button" variant="secondary">Cancelar</Button>} />
              <Button type="submit" variant="primary" loading={busy}>Registrar</Button>
            </div>
          </form>
        </Dialog>
      </Dialog.Root>

      <Dialog.Root open={endOpen} onOpenChange={setEndOpen} role="alertdialog">
        <Dialog className="px-8 py-6">
          <Dialog.Title className="mb-1.5 text-xl font-semibold">¿Terminar contrato?</Dialog.Title>
          <Dialog.Description className="text-kumo-subtle">
            Se terminará el contrato de {lease.tenantFullName} en {lease.propertyName}. La propiedad quedará disponible. Esta acción no se puede deshacer.
          </Dialog.Description>
          <div className="mt-8 flex justify-end gap-2">
            <Dialog.Close render={(p) => <Button {...p} variant="secondary">Cancelar</Button>} />
            <Button variant="destructive" loading={busy} onClick={terminate}>Terminar contrato</Button>
          </div>
        </Dialog>
      </Dialog.Root>
    </div>
  )
}
