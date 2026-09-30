import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router'
import { AirportFormModal } from '../components/AirportFormModal'
import { AirportMap } from '../components/AirportMap'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { PageHeader } from '../components/PageHeader'
import { PermissionError } from '../components/PermissionError'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { airports, catalog } from '../lib/admin'
import { airportInputOf } from '../lib/airports'
import { formatMoney, formatNumber } from '../lib/format'
import type { Airport } from '../lib/types'

/** Airports (§F17.6–§F17.8, permission `airport.manage`): list with geofences on a map, create / edit / activate / delete. */
export function AirportsPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => airports.list(), 'airports')
  const citiesQuery = useQuery(() => catalog.cities(), 'catalog-cities')
  const [editing, setEditing] = useState<Airport | 'new' | null>(null)
  const [deleting, setDeleting] = useState<Airport | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  const rows = useMemo(() => [...(query.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [query.data])
  const cityNames = useMemo(() => new Map((citiesQuery.data ?? []).map((city) => [city.id, city.name])), [citiesQuery.data])
  const nameOf = (airport: Airport) => (lang === 'ar' ? airport.nameAr : airport.nameEn) || airport.nameEn || airport.nameAr

  const setActive = async (airport: Airport, isActive: boolean) => {
    setBusyId(airport.id)
    try {
      await airports.update(airport.id, airportInputOf(airport, { isActive }))
      toast.success(t(isActive ? 'apActivated' : 'apDeactivated'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusyId(null)
    }
  }

  const remove = async () => {
    if (!deleting) return
    try {
      await airports.remove(deleting.id)
      toast.success(t('apDeleted'))
      if (selectedId === deleting.id) setSelectedId(null)
      setDeleting(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<Airport>[] = [
    { key: 'code', header: t('apCode'), render: (row) => <span className="ltr-nums font-bold">{row.code}</span> },
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block min-w-40">
          <span className="block font-bold">{nameOf(row)}</span>
          <span className="block text-xs text-muted">{lang === 'ar' ? row.nameEn : row.nameAr}</span>
        </span>
      ),
    },
    { key: 'city', header: t('city'), render: (row) => cityNames.get(row.cityId) ?? <span className="ltr-nums text-xs text-muted">{row.cityId}</span> },
    {
      key: 'policy',
      header: t('apSectionPolicy'),
      render: (row) => (
        <span className="block whitespace-nowrap text-xs">
          <span className="block">
            {t('apFreeWaiting')}: <span className="ltr-nums font-bold">{typeof row.defaultFreeWaitingMinutes === 'number' ? `${formatNumber(row.defaultFreeWaitingMinutes)} ${t('min')}` : '—'}</span>
          </span>
          <span className="block text-muted">
            {t('apPerMinute')}: <span className="ltr-nums font-bold text-ink">{typeof row.defaultWaitingPerMinute === 'number' ? `${formatMoney(row.defaultWaitingPerMinute)} ${t('sar')}` : '—'}</span>
          </span>
        </span>
      ),
    },
    {
      key: 'flags',
      header: t('apFlags'),
      render: (row) => (
        <span className="flex flex-col items-start gap-1">
          <Badge tone={row.queueEnabled ? 'brand' : 'muted'}>{row.queueEnabled ? t('apQueueOn') : t('apQueueOff')}</Badge>
          {row.requiresPickupZone && <Badge tone="ink">{t('apPickupZoneRequired')}</Badge>}
        </span>
      ),
    },
    {
      key: 'zones',
      header: t('apZones'),
      className: 'text-center',
      render: (row) => <span className="ltr-nums">{typeof row.zonesCount === 'number' ? formatNumber(row.zonesCount) : '—'}</span>,
    },
    { key: 'status', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2" onClick={(event) => event.stopPropagation()}>
          <Button variant="secondary" size="sm" icon="edit" onClick={() => setEditing(row)}>
            {t('edit')}
          </Button>
          {row.isActive ? (
            <Button variant="danger-outline" size="sm" icon="pause" loading={busyId === row.id} onClick={() => setActive(row, false)}>
              {t('prDeactivate')}
            </Button>
          ) : (
            <Button variant="brand" size="sm" icon="play" loading={busyId === row.id} onClick={() => setActive(row, true)}>
              {t('prActivate')}
            </Button>
          )}
          <Button variant="danger-outline" size="sm" icon="trash" aria-label={t('delete')} onClick={() => setDeleting(row)}>
            {t('delete')}
          </Button>
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader
        title={t('apTitle')}
        description={t('apCopy')}
        actions={
          <Button icon="plus" onClick={() => setEditing('new')}>
            {t('apNew')}
          </Button>
        }
      />

      <div className="grid gap-6 xl:grid-cols-2">
        <Card flush>
          {query.error ? (
            <PermissionError error={query.error} permission="airport.manage" onRetry={query.reload} />
          ) : (
            <Table
              columns={columns}
              rows={rows}
              rowKey={(row) => row.id}
              loading={query.loading && !query.data}
              onRowClick={(row) => navigate(`/airports/${row.id}`)}
              emptyTitle={t('apEmpty')}
              emptyDescription={t('apEmptyCopy')}
            />
          )}
        </Card>

        <Card title={t('apMapTitle')} description={t('apMapCopy')} flush>
          <div className="px-5 pb-5 sm:px-6 sm:pb-6">
            <AirportMap airports={rows} selectedAirportId={selectedId} onSelectAirport={(airport) => setSelectedId(airport.id)} className="h-80 w-full xl:h-[28rem]" />
          </div>
        </Card>
      </div>

      <AirportFormModal
        open={editing !== null}
        airport={editing === 'new' ? null : editing}
        airports={rows}
        onClose={() => setEditing(null)}
        onSaved={(saved) => {
          const wasNew = editing === 'new'
          setEditing(null)
          toast.success(t('apSaved'))
          query.reload()
          if (wasNew && saved?.id) navigate(`/airports/${saved.id}`)
        }}
      />

      <ConfirmModal
        open={deleting !== null}
        title={t('apDeleteTitle')}
        description={deleting ? `${deleting.code} · ${nameOf(deleting)} — ${t('apDeleteCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeleting(null)}
        onConfirm={remove}
      />
    </>
  )
}
