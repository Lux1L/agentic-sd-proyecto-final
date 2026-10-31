import { useEffect, useState } from 'react'
import { Badge, Empty, Input, LayerCard, Loader, Pagination, Select, Table, Text } from '@cloudflare/kumo'
import { MagnifyingGlassIcon } from '@phosphor-icons/react'
import { useNavigate } from 'react-router-dom'
import { api, type Lease } from '../api'
import { docTypeLabel, formatDate, formatMoney } from '../format'
import { useErrorHandler } from '../errors'

interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

const PAGE_SIZE = 10
const paginationLabels = {
  navigation: 'Paginación',
  firstPage: 'Primera página',
  previousPage: 'Página anterior',
  nextPage: 'Página siguiente',
  lastPage: 'Última página',
  pageNumber: 'Número de página',
}
const statusItems = { All: 'Todos', Active: 'Activos', Terminated: 'Terminados' }

export default function Leases() {
  const navigate = useNavigate()
  const handle = useErrorHandler()
  const [status, setStatus] = useState('All')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<Paged<Lease> | null>(null)

  // Debounce de la búsqueda (~300 ms); al cambiar el término se vuelve a la página 1.
  useEffect(() => {
    const t = setTimeout(() => {
      setSearch((prev) => {
        const next = searchInput.trim()
        if (next !== prev) setPage(1)
        return next
      })
    }, 300)
    return () => clearTimeout(t)
  }, [searchInput])

  useEffect(() => {
    let cancelled = false
    const qs = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) })
    if (status !== 'All') qs.set('status', status)
    if (search) qs.set('search', search)
    api<Paged<Lease>>(`/leases?${qs}`)
      .then((d) => { if (!cancelled) setData(d) })
      .catch((e) => { if (!cancelled) handle(e) })
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, status, search])

  return (
    <div className="flex flex-col gap-6">
      <Text as="h1" variant="heading" size="lg">Contratos</Text>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
        <Select
          className="w-full sm:w-48"
          label="Estado"
          hideLabel={false}
          items={statusItems}
          value={status}
          onValueChange={(v) => { setStatus(v as string); setPage(1) }}
        >
          <Select.Option value="All">Todos</Select.Option>
          <Select.Option value="Active">Activos</Select.Option>
          <Select.Option value="Terminated">Terminados</Select.Option>
        </Select>
        <div className="w-full sm:max-w-sm">
          <Input
            label="Buscar"
            placeholder="Arrendatario o documento"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
        </div>
      </div>

      {!data ? (
        <div className="flex justify-center p-12"><Loader size={32} /></div>
      ) : data.items.length === 0 ? (
        <Empty
          icon={<MagnifyingGlassIcon size={48} className="text-kumo-inactive" />}
          title="No hay contratos"
          description="Ningún contrato coincide con los filtros."
        />
      ) : (
        <>
          <LayerCard className="p-0">
            <Table>
              <Table.Header>
                <Table.Row>
                  <Table.Head>Propiedad</Table.Head>
                  <Table.Head>Arrendatario</Table.Head>
                  <Table.Head>Documento</Table.Head>
                  <Table.Head>Vigencia</Table.Head>
                  <Table.Head>Alquiler</Table.Head>
                  <Table.Head>Estado</Table.Head>
                </Table.Row>
              </Table.Header>
              <Table.Body>
                {data.items.map((l) => {
                  const open = () => navigate(`/leases/${l.id}`)
                  return (
                    <Table.Row
                      key={l.id}
                      className="cursor-pointer focus-visible:outline-2 focus-visible:outline-kumo-brand"
                      tabIndex={0}
                      aria-label={`Ver contrato de ${l.tenantFullName} en ${l.propertyName}`}
                      onClick={open}
                      onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); open() } }}
                    >
                      <Table.Cell>{l.propertyName}</Table.Cell>
                      <Table.Cell>{l.tenantFullName}</Table.Cell>
                      <Table.Cell>{docTypeLabel[l.tenantDocumentType] ?? l.tenantDocumentType} {l.tenantDocumentNumber}</Table.Cell>
                      <Table.Cell>{formatDate(l.startDate)} – {formatDate(l.endDate)}</Table.Cell>
                      <Table.Cell>{formatMoney(l.monthlyRent)}</Table.Cell>
                      <Table.Cell>
                        {l.status === 'Active'
                          ? <Badge variant="success">Activo</Badge>
                          : <Badge variant="neutral">Terminado</Badge>}
                      </Table.Cell>
                    </Table.Row>
                  )
                })}
              </Table.Body>
            </Table>
          </LayerCard>
          <Pagination
            page={page}
            setPage={setPage}
            perPage={PAGE_SIZE}
            totalCount={data.totalCount}
            labels={paginationLabels}
          >
            <Pagination.Info>
              {({ pageShowingRange, totalCount }) => `Mostrando ${pageShowingRange} de ${totalCount}`}
            </Pagination.Info>
            <Pagination.Separator />
            <Pagination.Controls />
          </Pagination>
        </>
      )}
    </div>
  )
}
