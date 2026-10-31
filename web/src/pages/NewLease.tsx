import { useState, type FormEvent } from 'react'
import { Button, Input, LayerCard, Select } from '@cloudflare/kumo'
import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api'
import { useErrorHandler } from '../errors'

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

  async function submit(e: FormEvent) {
    e.preventDefault()
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
          startDate: f.startDate || null,
          endDate: f.endDate || null,
          monthlyRent: f.monthlyRent === '' ? 0 : Number(f.monthlyRent),
          deposit: f.deposit === '' ? 0 : Number(f.deposit),
          paymentDay: f.paymentDay === '' ? 0 : Number(f.paymentDay),
        },
      })
      navigate(`/leases/${leaseId}`)
    } catch (e2) {
      setErrors(handle(e2))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <h1 className="mb-4 text-2xl font-semibold">Nuevo contrato</h1>
      <LayerCard>
        <LayerCard.Secondary>Arrendatario y condiciones</LayerCard.Secondary>
        <LayerCard.Primary>
          <form onSubmit={submit} className="grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
            <div className="sm:col-span-2">
              <Input label="Nombre completo" value={f.tenantFullName} onChange={set('tenantFullName')} {...err('tenantFullName')} />
            </div>
            <Select label="Tipo de documento" hideLabel={false} value={f.tenantDocumentType}
              onValueChange={(v) => setF({ ...f, tenantDocumentType: v as string })} error={errors.tenantdocumenttype}>
              <Select.Option value="Dni">DNI</Select.Option>
              <Select.Option value="Ce">CE</Select.Option>
              <Select.Option value="Ruc">RUC</Select.Option>
            </Select>
            <Input label="Número de documento" value={f.tenantDocumentNumber} onChange={set('tenantDocumentNumber')} {...err('tenantDocumentNumber')} />
            <Input label="Celular" value={f.tenantPhone} onChange={set('tenantPhone')} {...err('tenantPhone')} />
            <Input label="Correo (opcional)" type="email" value={f.tenantEmail} onChange={set('tenantEmail')} {...err('tenantEmail')} />
            <Input label="Inicio" type="date" value={f.startDate} onChange={set('startDate')} {...err('startDate')} />
            <Input label="Fin" type="date" value={f.endDate} onChange={set('endDate')} {...err('endDate')} />
            <Input label="Alquiler mensual (S/)" type="number" min="0" step="0.01" value={f.monthlyRent} onChange={set('monthlyRent')} {...err('monthlyRent')} />
            <Input label="Garantía (S/)" type="number" min="0" step="0.01" value={f.deposit} onChange={set('deposit')} {...err('deposit')} />
            <Input label="Día de pago (1-28)" type="number" min="1" max="28" value={f.paymentDay} onChange={set('paymentDay')} {...err('paymentDay')} />
            <div className="flex justify-end gap-2 sm:col-span-2">
              <Button type="button" variant="secondary" onClick={() => navigate('/')}>Cancelar</Button>
              <Button type="submit" variant="primary" loading={busy}>Guardar contrato</Button>
            </div>
          </form>
        </LayerCard.Primary>
      </LayerCard>
    </div>
  )
}
