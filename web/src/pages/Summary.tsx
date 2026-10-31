import { useEffect, useState } from 'react'
import { Button, Empty, Grid, LayerCard, Loader, Meter, Surface, Text } from '@cloudflare/kumo'
import { PlusIcon } from '@phosphor-icons/react'
import { useNavigate } from 'react-router-dom'
import { api, type PortfolioSummary } from '../api'
import { formatMoney, propertyTypeLabel } from '../format'
import { StatusBadge } from '../components/StatusBadge'
import { useErrorHandler } from '../errors'

function Stat({ label, value, danger }: { label: string; value: string; danger?: boolean }) {
  return (
    <Surface className={`rounded-lg px-5 py-4 ${danger ? 'ring-kumo-danger bg-kumo-danger/10' : ''}`}>
      <Text variant="secondary">{label}</Text>
      {/* Excepción consciente a kumo-design content-text-size: KPI para demo (ver docs/ai-evidence) */}
      <div className={`mt-1.5 text-3xl font-semibold ${danger ? 'text-kumo-danger' : 'text-kumo-default'}`}>{value}</div>
    </Surface>
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
      <Text as="h1" variant="heading" size="lg">Resumen</Text>
      <Grid variant="3up" gap="base">
        <Stat label="Propiedades" value={String(data.totalProperties)} />
        <Stat label="Alquiladas" value={`${data.leasedProperties} de ${data.totalProperties}`} />
        <Stat label="Deuda vencida total" value={formatMoney(data.totalOverdueDebt)} danger={data.totalOverdueDebt > 0} />
      </Grid>

      {data.properties.length === 0 ? (
        <Empty title="Aún no hay propiedades" description="Cuando registres propiedades aparecerán aquí." />
      ) : (
      <Grid variant="2up" gap="base">
        {data.properties.map((p) => {
          const leased = !!p.leaseId
          const open = () => leased && navigate(`/leases/${p.leaseId}`)
          return (
            <div
              key={p.propertyId}
              className={leased ? 'cursor-pointer rounded-lg focus-visible:outline-2 focus-visible:outline-kumo-brand' : undefined}
              onClick={open}
              onKeyDown={(e) => { if (leased && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); open() } }}
              role={leased ? 'link' : undefined}
              tabIndex={leased ? 0 : undefined}
              aria-label={leased ? `Ver contrato de ${p.name}` : undefined}
            >
            <LayerCard>
              <LayerCard.Secondary>
                <div className="flex items-center justify-between gap-2">
                  <Text bold>{p.name}</Text>
                  <Text variant="secondary">{propertyTypeLabel[p.type] ?? p.type}</Text>
                </div>
              </LayerCard.Secondary>
              <LayerCard.Primary>
                <div className="flex flex-col gap-4">
                  <div className="flex items-center justify-between gap-2">
                    <Text>{leased ? p.tenantFullName : 'Sin arrendatario'}</Text>
                    <StatusBadge status={leased ? p.currentPeriodStatus : 'Available'} />
                  </div>
                  {leased && (
                    <div className="flex justify-between text-sm">
                      <Text variant="secondary">Alquiler: <Text as="span" bold>{formatMoney(p.monthlyRent)}</Text></Text>
                      <Text variant={p.overdueDebt > 0 ? 'error' : 'secondary'} bold={p.overdueDebt > 0}>
                        Deuda: {formatMoney(p.overdueDebt)}
                      </Text>
                    </div>
                  )}
                  {leased && p.monthlyRent != null && p.monthlyRent > 0 && p.currentPeriodPaid != null && (
                    <Meter
                      label={`Mes actual: ${formatMoney(p.currentPeriodPaid)} de ${formatMoney(p.monthlyRent)}`}
                      value={Math.min(100, Math.round((p.currentPeriodPaid / p.monthlyRent) * 100))}
                      showValue={false}
                    />
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
            </div>
          )
        })}
      </Grid>
      )}
    </div>
  )
}
