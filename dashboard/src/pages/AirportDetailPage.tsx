import { useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { AirportFormModal } from '../components/AirportFormModal'
import { AirportMap } from '../components/AirportMap'
import { AirportQueuePanel } from '../components/AirportQueuePanel'
import { AirportZoneFormModal } from '../components/AirportZoneFormModal'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { DefinitionList } from '../components/DefinitionList'
import { Icon } from '../components/Icon'
import { PageSpinner } from '../components/Spinner'
import { PermissionError } from '../components/PermissionError'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { useUrlState } from '../hooks/useUrlState'
import { airports, catalog } from '../lib/admin'
import { AIRPORT_ZONE_KIND_KEY, AIRPORT_ZONE_KINDS, airportInputOf, airportZoneInputOf, effectiveWaiting } from '../lib/airports'
import { parseEnum } from '../lib/finance'
import { formatDateTime, formatMoney, formatNumber } from '../lib/format'
import type { AirportZone, AirportZoneKind } from '../lib/types'

const TABS = ['zones', 'queue'] as const

/** One airport (§F17.4 admin, `airport.manage`): geofence and zones on a map, waiting policy, zone CRUD and the live driver queue. */
export function AirportDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter } = useUrlState()
  const tab = parseEnum(params.get('tab'), TABS) || 'zones'
  const query = useQuery(() => airports.get(id), `airport:${id}`)
  const zonesQuery = useQuery(() => airports.zones(id), `airport-zones:${id}`)
  const citiesQuery = useQuery(() => catalog.cities(), 'catalog-cities')
  const allAirports = useQuery(() => airports.list(), 'airports')
  const [editingAirport, setEditingAirport] = useState(false)
  const [deletingAirport, setDeletingAirport] = useState(false)
  const [editingZone, setEditingZone] = useState<AirportZone | 'new' | null>(null)
  const [deletingZone, setDeletingZone] = useState<AirportZone | null>(null)
  const [selectedZoneId, setSelectedZoneId] = useState<string | null>(null)
  const [kindFilter, setKindFilter] = useState<AirportZoneKind | ''>('')
  const [busy, setBusy] = useState<string | null>(null)

  const airport = query.data
  const zones = useMemo(
    () => [...(zonesQuery.data ?? [])].sort((a, b) => AIRPORT_ZONE_KINDS.indexOf(a.kind) - AIRPORT_ZONE_KINDS.indexOf(b.kind) || a.sortOrder - b.sortOrder || a.code.localeCompare(b.code)),
    [zonesQuery.data],
  )
  const visibleZones = kindFilter ? zones.filter((zone) => zone.kind === kindFilter) : zones
  const airportList = useMemo(() => (airport ? [airport] : []), [airport])
  const cityName = airport ? (citiesQuery.data ?? []).find((city) => city.id === airport.cityId)?.name : undefined
  const nameOf = (item: { nameAr: string; nameEn: string }) => (lang === 'ar' ? item.nameAr : item.nameEn) || item.nameEn || item.nameAr

  if (query.loading && !airport) return <PageSpinner />
  if (query.error || !airport) {
    return (
      <Card>
        <PermissionError error={query.error} permission="airport.manage" onRetry={query.reload} />
      </Card>
    )
  }

  const setAirportActive = async (isActive: boolean) => {
    setBusy('airport')
    try {
      await airports.update(airport.id, airportInputOf(airport, { isActive }))
      toast.success(t(isActive ? 'apActivated' : 'apDeactivated'))
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusy(null)
    }
  }

  const deleteAirport = async () => {
    try {
      await airports.remove(airport.id)
      toast.success(t('apDeleted'))
      navigate('/airports', { replace: true })
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const setZoneActive = async (zone: AirportZone, isActive: boolean) => {
    setBusy(zone.id)
    try {
      await airports.updateZone(airport.id, zone.id, airportZoneInputOf(zone, { isActive }))
      toast.success(t(isActive ? 'apZoneActivated' : 'apZoneDeactivated'))
      zonesQuery.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    } finally {
      setBusy(null)
    }
  }

  const deleteZone = async () => {
    if (!deletingZone) return
    try {
      await airports.removeZone(airport.id, deletingZone.id)
      toast.success(t('apZoneDeleted'))
      if (selectedZoneId === deletingZone.id) setSelectedZoneId(null)
      setDeletingZone(null)
      zonesQuery.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const zoneColumns: Column<AirportZone>[] = [
    {
      key: 'kind',
      header: t('apZoneKind'),
      render: (row) => <Badge tone={row.kind === 'terminal' ? 'ink' : row.kind === 'pickup_zone' ? 'brand' : 'warning'}>{t(AIRPORT_ZONE_KIND_KEY[row.kind])}</Badge>,
    },
    { key: 'code', header: t('code'), render: (row) => <span className="ltr-nums font-bold">{row.code}</span> },
    {
      key: 'name',
      header: t('name'),
      render: (row) => (
        <span className="block min-w-40">
          <span className="block font-bold">{nameOf(row)}</span>
          {row.terminalCode && <span className="ltr-nums block text-xs text-muted">{row.terminalCode}</span>}
        </span>
      ),
    },
    {
      key: 'waiting',
      header: t('apSectionPolicy'),
      render: (row) => {
        if (row.kind !== 'pickup_zone') return <span className="text-muted">—</span>
        const policy = effectiveWaiting(row, airport)
        return (
          <span className="block whitespace-nowrap text-xs">
            <span className="block">
              {t('apFreeWaiting')}: <span className="ltr-nums font-bold">{policy.freeMinutes !== null ? `${formatNumber(policy.freeMinutes)} ${t('min')}` : t('apFromPricingRule')}</span>
              {policy.freeMinutes !== null && <span className="ms-1 text-muted">({t(policy.freeFromZone ? 'apSourceZone' : 'apSourceAirport')})</span>}
            </span>
            <span className="block text-muted">
              {t('apPerMinute')}: <span className="ltr-nums font-bold text-ink">{policy.perMinute !== null ? `${formatMoney(policy.perMinute)} ${t('sar')}` : t('apFromPricingRule')}</span>
              {policy.perMinute !== null && <span className="ms-1">({t(policy.perMinuteFromZone ? 'apSourceZone' : 'apSourceAirport')})</span>}
            </span>
          </span>
        )
      },
    },
    { key: 'status', header: t('status'), render: (row) => <Badge tone={row.isActive ? 'brand' : 'muted'}>{row.isActive ? t('active') : t('inactive')}</Badge> },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2" onClick={(event) => event.stopPropagation()}>
          <Button variant="secondary" size="sm" icon="edit" onClick={() => setEditingZone(row)}>
            {t('edit')}
          </Button>
          {row.isActive ? (
            <Button variant="danger-outline" size="sm" icon="pause" aria-label={t('prDeactivate')} loading={busy === row.id} onClick={() => setZoneActive(row, false)} />
          ) : (
            <Button variant="brand" size="sm" icon="play" aria-label={t('prActivate')} loading={busy === row.id} onClick={() => setZoneActive(row, true)} />
          )}
          <Button variant="danger-outline" size="sm" icon="trash" aria-label={t('delete')} onClick={() => setDeletingZone(row)} />
        </span>
      ),
    },
  ]

  return (
    <>
      <Link to="/airports" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('apBack')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="plane" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="ltr-nums text-sm font-bold text-brand">{airport.code}</p>
              <h2 className="text-2xl font-bold leading-tight">{nameOf(airport)}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <Badge tone={airport.isActive ? 'brand' : 'muted'}>{airport.isActive ? t('active') : t('inactive')}</Badge>
                <Badge tone={airport.queueEnabled ? 'brand' : 'muted'}>{airport.queueEnabled ? t('apQueueOn') : t('apQueueOff')}</Badge>
                {airport.requiresPickupZone && <Badge tone="ink">{t('apPickupZoneRequired')}</Badge>}
                {cityName && <span>{cityName}</span>}
              </div>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="edit" onClick={() => setEditingAirport(true)}>
              {t('edit')}
            </Button>
            {airport.isActive ? (
              <Button variant="danger-outline" icon="pause" loading={busy === 'airport'} onClick={() => setAirportActive(false)}>
                {t('prDeactivate')}
              </Button>
            ) : (
              <Button variant="brand" icon="play" loading={busy === 'airport'} onClick={() => setAirportActive(true)}>
                {t('prActivate')}
              </Button>
            )}
            <Button variant="danger-outline" icon="trash" onClick={() => setDeletingAirport(true)}>
              {t('delete')}
            </Button>
          </div>
        </div>
        <div className="mt-5">
          <DefinitionList
            items={[
              { label: t('apFreeWaiting'), value: typeof airport.defaultFreeWaitingMinutes === 'number' ? `${formatNumber(airport.defaultFreeWaitingMinutes)} ${t('min')}` : t('apFromPricingRule') },
              { label: t('apPerMinute'), value: typeof airport.defaultWaitingPerMinute === 'number' ? `${formatMoney(airport.defaultWaitingPerMinute)} ${t('sar')}` : t('apFromPricingRule') },
              { label: t('apRequiresPickupZone'), value: airport.requiresPickupZone ? t('yes') : t('no') },
              { label: t('apRefPoint'), value: `${airport.lat}, ${airport.lng}`, ltr: true },
              ...(airport.updatedAt ? [{ label: t('apUpdatedAt'), value: formatDateTime(airport.updatedAt, lang) }] : []),
            ]}
          />
        </div>
      </Card>

      <Tabs
        className="mb-4"
        value={tab}
        onChange={(value) => setFilter('tab', value === 'zones' ? '' : value)}
        options={[
          { value: 'zones', label: t('apTabZones'), count: zonesQuery.data ? zonesQuery.data.length : null },
          { value: 'queue', label: t('apTabQueue') },
        ]}
      />

      {tab === 'queue' ? (
        <AirportQueuePanel airportId={airport.id} queueEnabled={airport.queueEnabled} />
      ) : (
        <div className="grid gap-6 xl:grid-cols-2">
          <Card
            title={t('apZonesTitle')}
            description={t('apZonesCopy')}
            flush
            action={
              <Button icon="plus" size="sm" onClick={() => setEditingZone('new')}>
                {t('apZoneNew')}
              </Button>
            }
          >
            <div className="px-5 pb-3 sm:px-6">
              <Tabs
                value={kindFilter}
                onChange={setKindFilter}
                options={[{ value: '', label: t('statusAll') }, ...AIRPORT_ZONE_KINDS.map((kind) => ({ value: kind, label: t(AIRPORT_ZONE_KIND_KEY[kind]), count: zones.filter((zone) => zone.kind === kind).length }))]}
              />
            </div>
            {zonesQuery.error ? (
              <PermissionError error={zonesQuery.error} permission="airport.manage" onRetry={zonesQuery.reload} />
            ) : (
              <Table
                columns={zoneColumns}
                rows={visibleZones}
                rowKey={(row) => row.id}
                loading={zonesQuery.loading && !zonesQuery.data}
                onRowClick={(row) => setSelectedZoneId(row.id)}
                emptyTitle={t('apZonesEmpty')}
                emptyDescription={t('apZonesEmptyCopy')}
              />
            )}
          </Card>

          <Card title={t('apMapTitle')} description={t('apDetailMapCopy')} flush>
            <div className="px-5 pb-5 sm:px-6 sm:pb-6">
              <AirportMap airports={airportList} selectedAirportId={airport.id} zones={visibleZones} selectedZoneId={selectedZoneId} onSelectZone={(zone) => setSelectedZoneId(zone.id)} className="h-96 w-full xl:h-[32rem]" />
              <ul className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted">
                <li>
                  <span className="me-1.5 inline-block size-2.5 rounded-full bg-brand" />
                  {t('apLegendPickup')}
                </li>
                <li>
                  <span className="me-1.5 inline-block size-2.5 rounded-full bg-ink" />
                  {t('apLegendTerminal')}
                </li>
                <li>
                  <span className="me-1.5 inline-block size-2.5 rounded-full bg-amber-600" />
                  {t('apLegendWaiting')}
                </li>
              </ul>
            </div>
          </Card>
        </div>
      )}

      <AirportFormModal
        open={editingAirport}
        airport={airport}
        airports={allAirports.data ?? [airport]}
        onClose={() => setEditingAirport(false)}
        onSaved={() => {
          setEditingAirport(false)
          toast.success(t('apSaved'))
          query.reload()
        }}
      />

      <AirportZoneFormModal
        open={editingZone !== null}
        airport={airport}
        zone={editingZone === 'new' ? null : editingZone}
        zones={zones}
        onClose={() => setEditingZone(null)}
        onSaved={() => {
          setEditingZone(null)
          toast.success(t('apZoneSaved'))
          zonesQuery.reload()
        }}
      />

      <ConfirmModal
        open={deletingAirport}
        title={t('apDeleteTitle')}
        description={`${airport.code} · ${nameOf(airport)} — ${t('apDeleteCopy')}`}
        confirmLabel={t('delete')}
        onClose={() => setDeletingAirport(false)}
        onConfirm={deleteAirport}
      />

      <ConfirmModal
        open={deletingZone !== null}
        title={t('apZoneDeleteTitle')}
        description={deletingZone ? `${deletingZone.code} · ${nameOf(deletingZone)} — ${t('apZoneDeleteCopy')}` : undefined}
        confirmLabel={t('delete')}
        onClose={() => setDeletingZone(null)}
        onConfirm={deleteZone}
      />
    </>
  )
}
