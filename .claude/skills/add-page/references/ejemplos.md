# Ejemplos reales (copiados de `web/src/pages/`)

## Estado de formulario, errores por campo y envío (`NewLease.tsx`)

```tsx
const handle = useErrorHandler()
const [f, setF] = useState({ tenantFullName: '', startDate: '', monthlyRent: '', paymentDay: '5' /* … */ })
const [errors, setErrors] = useState<Record<string, string>>({})
const [busy, setBusy] = useState(false)
const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value })
const err = (k: string) => ({ error: errors[k.toLowerCase()], variant: errors[k.toLowerCase()] ? ('error' as const) : ('default' as const) })
const groupError = (k: string) => (errors[k] ? { message: errors[k], match: true as const } : undefined)

async function submit(e: FormEvent) {
  e.preventDefault()
  setBusy(true); setErrors({})
  try {
    const id = await api<string>('/leases', { method: 'POST', body: { /* … */ } })
    navigate(`/leases/${id}`)
  } catch (e2) {
    setErrors(handle(e2, Object.keys(f)))   // validación -> por campo; negocio -> toast
  } finally { setBusy(false) }
}
```

Campos: `<Input label="Celular" value={f.tenantPhone} onChange={set('tenantPhone')} {...err('tenantPhone')} />`.
Los números se envían con `Number(...)` (vacío -> `0`) y los opcionales vacíos como `null`.

## Select con `items` (muestra la etiqueta, no el valor)

```tsx
<Select className="w-full" label="Medio de pago" hideLabel={false}
  items={{ Cash: 'Efectivo', BankTransfer: 'Transferencia', Yape: 'Yape', Plin: 'Plin' }}
  value={pay.method} onValueChange={(v) => setPay({ ...pay, method: v as string })} error={errors.method}>
  <Select.Option value="Cash">Efectivo</Select.Option>
  …
</Select>
```

Período limitado al contrato (la UI impide RN-13):

```tsx
const periodOptions = useMemo(() => periodsBetween(lease.startDate, lease.endDate), [lease])
const periodItems = useMemo(() => Object.fromEntries(periodOptions.map((p) => [p, periodLabel(p)])), [periodOptions])
const periodValue = pay.period && periodOptions.includes(pay.period) ? pay.period : defaultPeriod(periodOptions)
```

## InputGroup con prefijo S/ (el error va con `match: true`)

```tsx
<InputGroup label="Monto" error={errors.amount ? { message: errors.amount, match: true } : undefined}>
  <InputGroup.Addon>S/</InputGroup.Addon>
  <InputGroup.Input type="number" min="0" step="0.01" value={pay.amount} onChange={(e) => setPay({ ...pay, amount: e.target.value })} />
</InputGroup>
```

## Dialog con formulario y alertdialog de confirmación (`LeaseDetail.tsx`)

```tsx
<Dialog.Root open={payOpen} onOpenChange={setPayOpen}>
  <Dialog size="lg" className="px-8 py-6">
    <Dialog.Title className="mb-6 text-xl font-semibold">Registrar pago</Dialog.Title>
    <form onSubmit={submitPayment} className="flex flex-col gap-4" noValidate>
      {/* campos */}
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
    <Dialog.Description className="text-kumo-subtle">… Esta acción no se puede deshacer.</Dialog.Description>
    <div className="mt-8 flex justify-end gap-2">
      <Dialog.Close render={(p) => <Button {...p} variant="secondary">Cancelar</Button>} />
      <Button variant="destructive" loading={busy} onClick={terminate}>Terminar contrato</Button>
    </div>
  </Dialog>
</Dialog.Root>
```

Al abrir el dialog limpia errores previos: `onClick={() => { setErrors({}); setPayOpen(true) }}`.

## Tabla

```tsx
<LayerCard className="p-0">
  <Table>
    <Table.Header><Table.Row><Table.Head>Período</Table.Head>…</Table.Row></Table.Header>
    <Table.Body>
      {rows.map((p) => (
        <Table.Row key={p.period}><Table.Cell>{formatPeriod(p.period)}</Table.Cell>…</Table.Row>
      ))}
    </Table.Body>
  </Table>
</LayerCard>
```
