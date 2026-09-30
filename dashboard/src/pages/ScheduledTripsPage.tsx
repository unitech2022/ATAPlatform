import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router'
import { AssignDriverModal } from '../components/AssignDriverModal'
import { Badge, MetaBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { CityField } from '../components/CityField'
import { Input, Select } from '../components/Field'
import { Icon } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { Pagination } from '../components/Pagination'
import { PermissionError } from '../components/PermissionError'
import { ReasonModal } from '../components/ReasonModal'
import { SchedulingKpis } from '../components/SchedulingKpis'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { rideCategories, scheduledTrips, zones } from '../lib/admin'
import { formatDateTime } from '../lib/format'
import { daysAgoIso, localName, todayIso } from '../lib/pricing'
import { applyClientFilters, formatCountdown, type ScheduledClientFilters, needsClientFilter, parseDriverFilter, parseReservationFilter, RESERVATION_FILTER_KEY, SCHEDULED_RESERVATION_FILTERS } from '../lib/scheduling'
import { reservationStateMeta } from '../lib/status'
import type { ScheduledTripRow } from '../lib/types'

const PAGE_SIZE = 20
/** Page size used when a filter the endpoint may not support is active: everything is fetched once and paged locally. */
const CLIENT_FETCH_SIZE = 200
const KPI_DAYS = [7, 30, 90] as const

/** Scheduled rides (§F17.4): upcoming trips with reservation state and risk, KPI cards, manual assignment (`scheduling.manage`). */
export function ScheduledTripsPage() {
  const { t, lang } = useLang()
  const navigate = useNavigate()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { params, setFilter, setPage, page } = useUrlState()

  const reservation = parseReservationFilter(params.get('reservation'))
  const from = parseIsoDate(params.get('from'))
  const to = parseIsoDate(params.get('to'))
  const cityId = params.get('cityId') ?? ''
  const rideCategoryId = params.get('categoryId') ?? ''
  const zoneId = params.get('zoneId') ?? ''
  const driver = parseDriverFilter(params.get('driver'))
  const atRisk = params.get('atRisk') === 'true'
  const kpiDays = KPI_DAYS.find((days) => String(days) === params.get('kpi')) ?? 30

  const categoriesQuery = useQuery(() => rideCategories.list(), 'ride-categories')
  const zonesQuery = useQuery(() => zones.list(), 'zones')
  const categories = useMemo(() => [...(categoriesQuery.data ?? [])].sort((a, b) => a.sortOrder - b.sortOrder), [categoriesQuery.data])
  const zoneList = useMemo(() => [...(zonesQuery.data ?? [])].sort((a, b) => a.code.localeCompare(b.code)), [zonesQuery.data])

  const selectedCategory = categories.find((category) => category.id === rideCategoryId)
  const filters = useMemo<ScheduledClientFilters>(
    () => ({
      driver,
      atRisk,
      rideCategoryId,
      categoryNames: selectedCategory ? [selectedCategory.nameAr, selectedCategory.nameEn, selectedCategory.code] : [],
      zoneId,
    }),
    [driver, atRisk, rideCategoryId, zoneId, selectedCategory],
  )
  const clientMode = needsClientFilter(filters)
  // "Without a driver" maps to the endpoint's `reservation=none`; it only combines with the "all" and "none" tabs.
  const reservationParam = driver === 'without' && (reservation === '' || reservation === 'none') ? 'none' : reservation

  const query = useQuery(
    () =>
      scheduledTrips.list({
        from,
        to,
        reservation: reservationParam,
        cityId: cityId || undefined,
        rideCategoryId: rideCategoryId || undefined,
        zoneId: zoneId || undefined,
        page: clientMode ? 1 : page,
        pageSize: clientMode ? CLIENT_FETCH_SIZE : PAGE_SIZE,
      }),
    `scheduled-trips:${from}:${to}:${reservationParam}:${cityId}:${rideCategoryId}:${zoneId}:${clientMode ? 'all' : page}`,
  )

  const kpiFrom = daysAgoIso(kpiDays - 1)
  const kpiTo = todayIso()
  const stats = useQuery(() => scheduledTrips.stats({ from: kpiFrom, to: kpiTo }), `scheduling-stats:${kpiFrom}:${kpiTo}`)

  const filtered = useMemo(() => {
    const items = [...(query.data?.items ?? [])].sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime())
    return clientMode ? applyClientFilters(items, filters) : items
  }, [query.data, clientMode, filters])
  const total = clientMode ? filtered.length : (query.data?.total ?? 0)
  const rows = clientMode ? filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE) : filtered
  const truncated = clientMode && (query.data?.total ?? 0) > CLIENT_FETCH_SIZE

  const [assigning, setAssigning] = useState<ScheduledTripRow | null>(null)
  const [releasing, setReleasing] = useState<ScheduledTripRow | null>(null)

  const hasActiveReservation = (row: ScheduledTripRow) => row.reservationStatus === 'reserved' || row.reservationStatus === 'confirmed' || row.reservationStatus === 'assigned'

  const release = async (reason: string) => {
    if (!releasing) return
    try {
      await scheduledTrips.releaseReservation(releasing.tripId, reason)
      toast.success(t('sdReleased'))
      setReleasing(null)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const columns: Column<ScheduledTripRow>[] = [
    { key: 'tripNumber', header: t('tripNumber'), render: (row) => <span className="ltr-nums font-bold">{row.tripNumber}</span> },
    {
      key: 'scheduledAt',
      header: t('scheduledAt'),
      render: (row) => (
        <span className="block whitespace-nowrap">
          <span className="block font-bold">{formatDateTime(row.scheduledAt, lang)}</span>
          <span className="ltr-nums block text-xs text-muted">
            {t('sdInPrefix')} {formatCountdown(row.minutesToPickup)}
          </span>
        </span>
      ),
    },
    { key: 'passenger', header: t('passenger'), render: (row) => <span className="font-bold">{row.passengerName || t('unnamed')}</span> },
    { key: 'category', header: t('category'), render: (row) => row.categoryName ?? '—' },
    {
      key: 'route',
      header: t('route'),
      render: (row) => (
        <span className="flex max-w-64 items-center gap-2 text-sm">
          <span className="truncate">{row.pickupName || '—'}</span>
          <Icon name="arrow" className="size-4 shrink-0 text-muted ltr:rotate-180" />
          <span className="truncate">{row.dropoffName || '—'}</span>
        </span>
      ),
    },
    {
      key: 'reservation',
      header: t('sdReservation'),
      render: (row) => (
        <span className="flex flex-col items-start gap-1">
          <MetaBadge record={reservationStateMeta} value={row.reservationStatus ?? 'none'} />
          {row.atRisk && (
            <Badge tone="danger">
              <Icon name="alert" className="me-1 size-3" />
              {t('sdAtRisk')}
            </Badge>
          )}
        </span>
      ),
    },
    { key: 'driver', header: t('driver'), render: (row) => (row.driverName ? <span className="font-bold">{row.driverName}</span> : <span className="text-muted">—</span>) },
    {
      key: 'actions',
      header: t('actions'),
      className: 'text-end',
      render: (row) => (
        <span className="inline-flex gap-2" onClick={(event) => event.stopPropagation()}>
          <Button variant="secondary" size="sm" icon="user" onClick={() => setAssigning(row)}>
            {hasActiveReservation(row) ? t('sdReassign') : t('sdAssign')}
          </Button>
          {hasActiveReservation(row) && (
            <Button variant="danger-outline" size="sm" icon="x" onClick={() => setReleasing(row)}>
              {t('sdRelease')}
            </Button>
          )}
        </span>
      ),
    },
  ]

  return (
    <>
      <PageHeader title={t('sdTitle')} description={t('sdCopy')} />

      {!stats.error && (
        <section className="mb-6" aria-label={t('sdKpisTitle')}>
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <h2 className="text-lg font-bold">{t('sdKpisTitle')}</h2>
            <div className="flex gap-1.5">
              {KPI_DAYS.map((days) => (
                <Button key={days} size="sm" variant={days === kpiDays ? 'primary' : 'secondary'} aria-pressed={days === kpiDays} onClick={() => setFilter('kpi', days === 30 ? '' : String(days))}>
                  {t(days === 7 ? 'fvLast7Days' : days === 30 ? 'fvLast30Days' : 'fvLast90Days')}
                </Button>
              ))}
            </div>
          </div>
          <SchedulingKpis stats={stats.data} loading={stats.loading} />
        </section>
      )}

      <Tabs
        className="mb-4"
        value={reservation}
        onChange={(value) => setFilter('reservation', value)}
        options={[{ value: '', label: t('statusAll') }, ...SCHEDULED_RESERVATION_FILTERS.map((value) => ({ value, label: t(RESERVATION_FILTER_KEY[value]) }))]}
      />

      <div className="mb-4 grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-4">
        <Input id="sd-from" type="date" label={t('fromDate')} dir="ltr" value={from} max={to || undefined} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="sd-to" type="date" label={t('toDate')} dir="ltr" value={to} min={from || undefined} onChange={(event) => setFilter('to', event.target.value)} />
        <CityField id="sd-city-filter" label={t('city')} value={cityId} onChange={(value) => setFilter('cityId', value)} />
        <Select id="sd-category-filter" label={t('rideCategory')} value={rideCategoryId} onChange={(event) => setFilter('categoryId', event.target.value)}>
          <option value="">{t('allCategories')}</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {localName(category, lang)}
            </option>
          ))}
        </Select>
        <Select id="sd-zone-filter" label={t('pickupZone')} value={zoneId} onChange={(event) => setFilter('zoneId', event.target.value)}>
          <option value="">{t('allZones')}</option>
          {zoneList.map((zone) => (
            <option key={zone.id} value={zone.id}>
              {localName(zone, lang)}
            </option>
          ))}
        </Select>
        <Select id="sd-driver-filter" label={t('driver')} value={driver} onChange={(event) => setFilter('driver', event.target.value)}>
          <option value="">{t('sdDriverAny')}</option>
          <option value="with">{t('sdDriverWith')}</option>
          <option value="without">{t('sdDriverWithout')}</option>
        </Select>
        <div className="flex items-end sm:col-span-2 lg:col-span-2">
          <Button
            variant={atRisk ? 'danger' : 'secondary'}
            icon="alert"
            aria-pressed={atRisk}
            className="w-full sm:w-auto"
            onClick={() => setFilter('atRisk', !atRisk)}
          >
            {t('sdAtRiskOnly')}
          </Button>
        </div>
      </div>
      {atRisk && <p className="mb-4 text-xs text-muted">{t('sdAtRiskCopy')}</p>}
      {truncated && (
        <p className="mb-4 flex items-start gap-2 rounded-2xl bg-amber-50 px-4 py-3 text-xs font-bold text-amber-800">
          <Icon name="info" className="mt-0.5 size-3.5 shrink-0" />
          {t('sdTruncated')}
        </p>
      )}

      <Card flush>
        {query.error ? (
          <PermissionError error={query.error} permission="scheduling.manage" onRetry={query.reload} />
        ) : (
          <>
            <Table
              columns={columns}
              rows={rows}
              rowKey={(row) => row.tripId}
              loading={query.loading}
              onRowClick={(row) => navigate(`/trips/${row.tripId}`)}
              emptyTitle={t('sdEmpty')}
              emptyDescription={t('sdEmptyCopy')}
            />
            {query.data && total > 0 && <Pagination page={page} pageSize={PAGE_SIZE} total={total} onChange={setPage} />}
          </>
        )}
      </Card>

      <AssignDriverModal
        open={assigning !== null}
        tripId={assigning?.tripId ?? ''}
        tripNumber={assigning?.tripNumber ?? ''}
        reassign={assigning ? hasActiveReservation(assigning) : false}
        onClose={() => setAssigning(null)}
        onDone={() => {
          setAssigning(null)
          query.reload()
        }}
      />

      <ReasonModal
        open={releasing !== null}
        title={t('sdReleaseTitle')}
        description={releasing ? `${releasing.tripNumber} — ${t('sdReleaseCopy')}` : undefined}
        confirmLabel={t('sdRelease')}
        label={t('sdReleaseReason')}
        onClose={() => setReleasing(null)}
        onConfirm={release}
      />
    </>
  )
}
