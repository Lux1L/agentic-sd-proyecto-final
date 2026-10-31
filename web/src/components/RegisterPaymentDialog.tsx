import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Button, Dialog, InputGroup, Loader, Select, useKumoToastManager } from '@cloudflare/kumo'
import { api, type Lease } from '../api'
import { defaultPeriod, formatMoney, formatPeriod, periodLabel, periodsBetween, todayIso } from '../format'
import { DateField } from './DateField'
import { useErrorHandler } from '../errors'

type Props = {
  /** Contrato al que se le registra el pago (null mientras no se elige uno). */
  leaseId: string | null
  /** Contrato ya cargado (detalle). Si no viene, se carga al abrir (inicio). */
  lease?: Lease | null
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Se llama tras registrar el pago, para recargar la pantalla que abrió el dialog. */
  onRegistered: () => void | Promise<void>
}

const methodItems = { Cash: 'Efectivo', BankTransfer: 'Transferencia', Yape: 'Yape', Plin: 'Plin' }

/** Dialog "Registrar pago" (RF-04), compartido por el detalle del contrato y el inicio. */
export function RegisterPaymentDialog({ leaseId, lease: given, open, onOpenChange, onRegistered }: Props) {
  const handle = useErrorHandler()
  const toasts = useKumoToastManager()
  const [loaded, setLoaded] = useState<Lease | null>(null)
  const [busy, setBusy] = useState(false)
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [pay, setPay] = useState({ period: '', amount: '', paidOn: todayIso(), method: 'Cash' })
  const lease = given ?? (loaded && loaded.id === leaseId ? loaded : null)

  // Al abrir se limpian los errores previos; al cambiar de contrato, también el período y el monto.
  const [prev, setPrev] = useState({ open, leaseId })
  if (prev.open !== open || prev.leaseId !== leaseId) {
    setPrev({ open, leaseId })
    if (open && !prev.open) setErrors({})
    if (leaseId !== prev.leaseId) setPay({ ...pay, period: '', amount: '' })
  }

  // Desde el inicio solo se conoce el id: el contrato (fechas para los períodos) se carga al abrir.
  useEffect(() => {
    if (!open || given || !leaseId || loaded?.id === leaseId) return
    let cancelled = false
    api<Lease>(`/leases/${leaseId}`)
      .then((l) => { if (!cancelled) setLoaded(l) })
      .catch((e) => {
        if (cancelled) return
        handle(e)
        onOpenChange(false)
      })
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, leaseId, given])

  // Solo los meses del contrato: la UI impide RN-13 (período fuera de la vigencia).
  const periodOptions = useMemo(() => (lease ? periodsBetween(lease.startDate, lease.endDate) : []), [lease])
  const periodItems = useMemo(() => Object.fromEntries(periodOptions.map((p) => [p, periodLabel(p)])), [periodOptions])
  const periodValue = pay.period && periodOptions.includes(pay.period) ? pay.period : defaultPeriod(periodOptions)

  async function submitPayment(e: FormEvent) {
    e.preventDefault()
    if (!lease) return
    setBusy(true)
    setErrors({})
    try {
      await api(`/leases/${lease.id}/payments`, {
        method: 'POST',
        body: { period: periodValue, amount: pay.amount === '' ? 0 : Number(pay.amount), paidOn: pay.paidOn, method: pay.method },
      })
      toasts.add({ title: 'Pago registrado', description: `${formatMoney(Number(pay.amount))} del período ${formatPeriod(periodValue)}`, variant: 'success' })
      onOpenChange(false)
      setPay({ ...pay, amount: '' })
      await onRegistered()
    } catch (e2) {
      setErrors(handle(e2, ['period', 'amount', 'paidOn', 'method']))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog size="lg" className="px-8 py-6">
        <Dialog.Title className={`${lease ? 'mb-1.5' : 'mb-6'} text-xl font-semibold`}>Registrar pago</Dialog.Title>
        {lease && (
          <Dialog.Description className="mb-6 text-kumo-subtle">
            {lease.propertyName} · {lease.tenantFullName}
          </Dialog.Description>
        )}
        {!lease ? (
          <div className="flex justify-center py-8"><Loader size={32} /></div>
        ) : (
          <form onSubmit={submitPayment} className="flex flex-col gap-4" noValidate>
            <Select className="w-full" label="Período" hideLabel={false} items={periodItems} value={periodValue} onValueChange={(v) => setPay({ ...pay, period: v as string })} error={errors.period}>
              {periodOptions.map((p) => <Select.Option key={p} value={p}>{periodLabel(p)}</Select.Option>)}
            </Select>
            <InputGroup label="Monto" error={errors.amount ? { message: errors.amount, match: true } : undefined}>
              <InputGroup.Addon>S/</InputGroup.Addon>
              <InputGroup.Input type="number" min="0" step="0.01" value={pay.amount} onChange={(e) => setPay({ ...pay, amount: e.target.value })} />
            </InputGroup>
            <DateField label="Fecha de pago" value={pay.paidOn} onChange={(v) => setPay({ ...pay, paidOn: v })} error={errors.paidon} />
            <Select className="w-full" label="Medio de pago" hideLabel={false} items={methodItems} value={pay.method} onValueChange={(v) => setPay({ ...pay, method: v as string })} error={errors.method}>
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
        )}
      </Dialog>
    </Dialog.Root>
  )
}
