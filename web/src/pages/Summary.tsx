import { useEffect, useState } from 'react'
import { Button, LayerCard, Loader, Text } from '@cloudflare/kumo'
import { PlusIcon } from '@phosphor-icons/react'
import { useNavigate } from 'react-router-dom'
import { api, type PortfolioSummary } from '../api'
import { formatMoney, propertyTypeLabel } from '../format'
import { StatusBadge } from '../components/StatusBadge'
import { useErrorHandler } from '../errors'

function Stat({ label, value, danger }: { label: string; value: string; danger?: boolean }) {
  return (
    <div className={`rounded-lg px-5 py-4 ring ${danger ? 'ring-kumo-danger bg-kumo-danger/10' : 'ring-kumo-line bg-kumo-base'}`}>
      <div className="text-sm text-kumo-subtle">{label}</div>
      <div className={`mt-1.5 text-3xl font-semibold ${danger ? 'text-kumo-danger' : 'text-kumo-default'}`}>{value}</div>
    </div>
  )
}

export default function Summary() {
  const navigate = useNavigate()
  const handle = useErrorHandler()
  const [data, setData] = useState<PortfolioSummary | null>(null)

  useEffect(() => {
    api<PortfolioSummary>('/portfolio/summary').then(setData).catch((e) => handle(e))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (!data) return <div className="flex justify-center p-12"><Loader size={32} /></div>

  return (
    <div className="flex flex-col gap-6">
      <Text as="h1" variant="heading1">Resumen</Text>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <Stat label="Propiedades" value={String(data.totalProperties)} />
        <Stat label="Alquiladas" value={`${data.leasedProperties} de ${data.totalProperties}`} />
        <Stat label="Deuda vencida total" value={formatMoney(data.totalOverdueDebt)} danger={data.totalOverdueDebt > 0} />
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        {data.properties.map((p) => {
          const leased = !!p.leaseId
          const open = () => leased && navigate(`/leases/${p.leaseId}`)
          return (
            <LayerCard key={p.propertyId}>
              <LayerCard.Secondary>
                <div className="flex items-center justify-between gap-2">
                  <span className="font-medium">{p.name}</span>
                  <span className="text-sm text-kumo-subtle">{propertyTypeLabel[p.type] ?? p.type}</span>
                </div>
              </LayerCard.Secondary>
              <LayerCard.Primary>
                <div
                  className={`flex flex-col gap-4 ${leased ? 'cursor-pointer' : ''}`}
                  onClick={open}
                  role={leased ? 'link' : undefined}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-kumo-strong">{leased ? p.tenantFullName : 'Sin arrendatario'}</span>
                    <StatusBadge status={leased ? p.currentPeriodStatus : 'Available'} />
                  </div>
                  {leased && (
                    <div className="flex justify-between text-sm">
                      <span className="text-kumo-subtle">Alquiler: <span className="font-medium text-kumo-default">{formatMoney(p.monthlyRent)}</span></span>
                      <span className={p.overdueDebt > 0 ? 'font-semibold text-kumo-danger' : 'text-kumo-subtle'}>
                        Deuda: {formatMoney(p.overdueDebt)}
                      </span>
                    </div>
                  )}
                  {p.status === 'Available' && (
                    <div>
                      <Button variant="primary" icon={PlusIcon}
                        onClick={(e) => { e.stopPropagation(); navigate(`/properties/${p.propertyId}/new-lease`) }}>
                        Nuevo contrato
                      </Button>
                    </div>
                  )}
                </div>
              </LayerCard.Primary>
            </LayerCard>
          )
        })}
      </div>
    </div>
  )
}
