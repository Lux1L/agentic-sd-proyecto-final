import { useState, type FormEvent } from 'react'
import { Button, Grid, Input, InputGroup, LayerCard, Select, Text } from '@cloudflare/kumo'
import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { DateField } from '../components/DateField'
import { useErrorHandler } from '../errors'

// RN-08: el día de pago va de 1 a 28; el Select no ofrece otro valor.
const DAYS = Array.from({ length: 28 }, (_, i) => String(i + 1))
const dayItems = Object.fromEntries(DAYS.map((d) => [d, d]))

export default function NewLease() {
  const { id } = useParams()
  const navigate = useNavigate()
  const handle = useErrorHandler()
  const [f, setF] = useState({
    tenantFullName: '', tenantDocumentType: 'Dni', tenantDocumentNumber: '', tenantPhone: '', tenantEmail: '',
    startDate: '', endDate: '', monthlyRent: '', deposit: '', paymentDay: '5',
  })
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [busy, setBusy] = useState(false)
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value })
  const err = (k: string) => ({ error: errors[k.toLowerCase()], variant: errors[k.toLowerCase()] ? ('error' as const) : ('default' as const) })

  const groupError = (k: string) => (errors[k] ? { message: errors[k], match: true as const } : undefined)

  async function submit(e: FormEvent) {
    e.preventDefault()
    // Las fechas vacías no se pueden enviar como JSON válido: se marcan en el formulario.
    const missing: Record<string, string> = {}
    if (!f.startDate) missing.startdate = 'Ingresa la fecha de inicio.'
    if (!f.endDate) missing.enddate = 'Ingresa la fecha de fin.'
    if (Object.keys(missing).length > 0) {
      setErrors(missing)
      return
    }
    setBusy(true)
    setErrors({})
    try {
      const leaseId = await api<string>('/leases', {
        method: 'POST',
        body: {
          propertyId: id,
          tenantFullName: f.tenantFullName,
          tenantDocumentType: f.tenantDocumentType,
          tenantDocumentNumber: f.tenantDocumentNumber,
          tenantPhone: f.tenantPhone,
          tenantEmail: f.tenantEmail || null,
          startDate: f.startDate,
          endDate: f.endDate,
          monthlyRent: f.monthlyRent === '' ? 0 : Number(f.monthlyRent),
          deposit: f.deposit === '' ? 0 : Number(f.deposit),
          paymentDay: f.paymentDay === '' ? 0 : Number(f.paymentDay),
        },
      })
      navigate(`/leases/${leaseId}`)
    } catch (e2) {
      setErrors(handle(e2, Object.keys(f)))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <div className="mb-4"><Text as="h1" variant="heading" size="lg">Nuevo contrato</Text></div>
      <LayerCard>
        <LayerCard.Secondary>Arrendatario y condiciones</LayerCard.Secondary>
        <LayerCard.Primary>
          <form onSubmit={submit} noValidate>
          <Grid variant="2up" gap="base" className="items-start">
            <div className="md:col-span-2">
              <Input label="Nombre completo" value={f.tenantFullName} onChange={set('tenantFullName')} {...err('tenantFullName')} />
            </div>
            <Select className="w-full" label="Tipo de documento" hideLabel={false} items={{ Dni: 'DNI', Ce: 'CE', Ruc: 'RUC' }} value={f.tenantDocumentType}
              onValueChange={(v) => setF({ ...f, tenantDocumentType: v as string })} error={errors.tenantdocumenttype}>
              <Select.Option value="Dni">DNI</Select.Option>
              <Select.Option value="Ce">CE</Select.Option>
              <Select.Option value="Ruc">RUC</Select.Option>
            </Select>
            <Input label="Número de documento" value={f.tenantDocumentNumber} onChange={set('tenantDocumentNumber')} {...err('tenantDocumentNumber')} />
            <Input label="Celular" value={f.tenantPhone} onChange={set('tenantPhone')} {...err('tenantPhone')} />
            <Input label="Correo (opcional)" type="email" value={f.tenantEmail} onChange={set('tenantEmail')} {...err('tenantEmail')} />
            <DateField label="Inicio" value={f.startDate} onChange={(v) => setF({ ...f, startDate: v })} error={errors.startdate} />
            <DateField label="Fin" value={f.endDate} onChange={(v) => setF({ ...f, endDate: v })} error={errors.enddate} />
            <InputGroup label="Alquiler mensual" error={groupError('monthlyrent')}>
              <InputGroup.Addon>S/</InputGroup.Addon>
              <InputGroup.Input type="number" min="0" step="0.01" value={f.monthlyRent} onChange={set('monthlyRent')} />
            </InputGroup>
            <InputGroup label="Garantía" error={groupError('deposit')}>
              <InputGroup.Addon>S/</InputGroup.Addon>
              <InputGroup.Input type="number" min="0" step="0.01" value={f.deposit} onChange={set('deposit')} />
            </InputGroup>
            <Select className="w-full" label="Día de pago" hideLabel={false} items={dayItems} value={f.paymentDay}
              onValueChange={(v) => setF({ ...f, paymentDay: v as string })} error={errors.paymentday}>
              {DAYS.map((d) => <Select.Option key={d} value={d}>{d}</Select.Option>)}
            </Select>
            <div className="flex justify-end gap-2 md:col-span-2">
              <Button type="button" variant="secondary" onClick={() => navigate('/')}>Cancelar</Button>
              <Button type="submit" variant="primary" loading={busy}>Guardar contrato</Button>
            </div>
          </Grid>
          </form>
        </LayerCard.Primary>
      </LayerCard>
    </div>
  )
}
