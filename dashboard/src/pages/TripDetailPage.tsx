import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { Badge, TripStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { JsonView } from '../components/JsonView'
import { MapView } from '../components/MapView'
import { ReasonModal } from '../components/ReasonModal'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { trips } from '../lib/admin'
import { formatDateTime, formatKm, formatMinutes, formatMoney, formatNumber } from '../lib/format'
import { escapeHtml, L, pinIcon, routeLineStyle, stopIcon } from '../lib/leaflet'
import { isTerminalTripStatus, PAYMENT_METHOD_KEY, PRICING_MODE_KEY, TRIP_ACTOR_KEY } from '../lib/trips'
import type { TripDetail, TripEvent, TripTimeline } from '../lib/types'

const TIMELINE_STEPS: { key: keyof TripTimeline; label: TranslationKey }[] = [
  { key: 'requestedAt', label: 'tlRequested' },
  { key: 'assignedAt', label: 'tlAssigned' },
  { key: 'arrivedAt', label: 'tlArrived' },
  { key: 'startedAt', label: 'tlStarted' },
  { key: 'completedAt', label: 'tlCompleted' },
]

export function TripDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const query = useQuery(() => trips.get(id), `trip:${id}`)
  const [cancelOpen, setCancelOpen] = useState(false)

  const cancelTrip = async (reason: string) => {
    try {
      await trips.cancel(id, reason)
      toast.success(t('tripCancelled'))
      setCancelOpen(false)
      query.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={query.reload} />
      </Card>
    )
  }

  const trip = query.data
  const canCancel = !isTerminalTripStatus(trip.status)
  const isCancelled = trip.status === 'cancelled' || trip.status === 'no_drivers'
  const point = (name: string | null, address: string | null) => [name, address].filter(Boolean).join(' — ') || '—'

  return (
    <>
      <Link to="/trips" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('backToTrips')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="route" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="text-sm font-bold text-brand">{t('tripNumber')}</p>
              <h2 className="ltr-nums text-2xl font-bold leading-tight">{trip.tripNumber}</h2>
              <div className="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted">
                <TripStatusBadge status={trip.status} />
                <Badge tone="muted">{trip.bookingType === 'scheduled' ? t('bookingScheduled') : t('bookingNow')}</Badge>
                {trip.rideCategory && <Badge tone="ink">{trip.rideCategory.name}</Badge>}
                <span>
                  {t('requestedAt')}: {formatDateTime(trip.timeline.requestedAt, lang)}
                </span>
              </div>
            </div>
          </div>
          {canCancel && (
            <Button variant="danger-outline" icon="x" onClick={() => setCancelOpen(true)}>
              {t('cancelTrip')}
            </Button>
          )}
        </div>

        {isCancelled && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <p>
              {trip.cancelledBy && (
                <span className="font-bold">
                  {t('cancelledBy')}: {t(TRIP_ACTOR_KEY[trip.cancelledBy])}
                  {trip.cancellationReason ? ' — ' : ''}
                </span>
              )}
              {trip.cancellationReason ?? (trip.cancelledBy ? '' : t('tripStatusNoDrivers'))}
            </p>
          </div>
        )}
      </Card>

      <div className="mb-6 grid gap-6 lg:grid-cols-2">
        <Card title={t('passenger')}>
          <DefinitionList
            items={[
              { label: t('fullName'), value: trip.passenger?.fullName || t('unnamed') },
              { label: t('phoneNumber'), value: trip.passenger?.phoneNumber ?? '—', ltr: true },
              { label: t('paymentMethod'), value: t(PAYMENT_METHOD_KEY[trip.paymentMethod] ?? 'paymentCash') },
              { label: t('preferFemaleDriver'), value: trip.preferFemaleDriver ? t('yes') : t('no') },
              ...(trip.riderNote ? [{ label: t('riderNote'), value: trip.riderNote }] : []),
            ]}
          />
        </Card>

        <Card title={t('driverAndVehicle')}>
          {trip.driver ? (
            <DefinitionList
              items={[
                { label: t('fullName'), value: trip.driver.fullName || t('unnamed') },
                { label: t('phoneNumber'), value: trip.driver.phoneNumber ?? trip.driver.phoneMasked ?? '—', ltr: true },
                { label: t('rating'), value: trip.driver.ratingAvg !== null ? `${formatNumber(trip.driver.ratingAvg)} ★` : '—', ltr: true },
                {
                  label: t('vehicle'),
                  value: trip.vehicle ? `${trip.vehicle.make} ${trip.vehicle.model} · ${trip.vehicle.color}` : t('noVehicle'),
                },
                { label: t('plateNumber'), value: trip.vehicle?.plateNumber ?? '—', ltr: true },
                ...(trip.waitingSeconds ? [{ label: t('waitingTime'), value: `${formatMinutes(trip.waitingSeconds)} ${t('min')}`, ltr: true }] : []),
              ]}
            />
          ) : (
            <EmptyState icon="car" title={t('noDriverAssigned')} />
          )}
        </Card>
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('route')} className="lg:col-span-2">
          <ol className="mb-5 space-y-3">
            <RoutePoint tone="brand" label={t('pickup')} value={point(trip.pickup.name, trip.pickup.address)} />
            {trip.stops.map((stop, index) => (
              <RoutePoint key={`${stop.lat},${stop.lng},${index}`} tone="ink" label={`${t('stop')} ${index + 1}`} value={point(stop.name, stop.address)} />
            ))}
            <RoutePoint tone="ink" label={t('dropoff')} value={point(trip.dropoff.name, trip.dropoff.address)} />
          </ol>
          <DefinitionList
            items={[
              {
                label: `${t('distance')} (${t('estimated')} / ${t('final')})`,
                value: `${formatKm(trip.estimatedDistanceMeters)} / ${formatKm(trip.finalDistanceMeters)} ${t('km')}`,
                ltr: true,
              },
              {
                label: `${t('duration')} (${t('estimated')} / ${t('final')})`,
                value: `${formatMinutes(trip.estimatedDurationSeconds)} / ${formatMinutes(trip.finalDurationSeconds)} ${t('min')}`,
                ltr: true,
              },
              { label: t('estimatedFare'), value: `${formatMoney(trip.estimatedFare)} ${t('sar')}`, ltr: true },
              { label: t('finalFare'), value: `${formatMoney(trip.finalFare)} ${t('sar')}`, ltr: true },
              { label: t('pricingMode'), value: t(PRICING_MODE_KEY[trip.pricingMode] ?? 'pricingFixed') },
              { label: t('offeredPrice'), value: trip.offeredPrice !== null ? `${formatMoney(trip.offeredPrice)} ${t('sar')}` : '—', ltr: true },
              ...(trip.scheduledAt ? [{ label: t('scheduledAt'), value: formatDateTime(trip.scheduledAt, lang) }] : []),
            ]}
          />
        </Card>

        <Card title={t('routeMap')} flush>
          <div className="px-5 pb-5 sm:px-6 sm:pb-6">
            <RouteMap trip={trip} />
          </div>
        </Card>
      </div>

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('timeline')}>
          <Timeline trip={trip} />
        </Card>
        <Card title={t('events')} flush className="lg:col-span-2">
          <EventsTable events={trip.events} />
        </Card>
      </div>

      <ReasonModal
        open={cancelOpen}
        title={t('cancelTripTitle')}
        description={`${trip.tripNumber} — ${t('cancelTripCopy')}`}
        confirmLabel={t('cancelTrip')}
        onClose={() => setCancelOpen(false)}
        onConfirm={cancelTrip}
      />
    </>
  )
}

function RoutePoint({ tone, label, value }: { tone: 'brand' | 'ink'; label: string; value: string }) {
  return (
    <li className="flex items-start gap-3">
      <span className={`mt-1 grid size-6 shrink-0 place-items-center rounded-full ${tone === 'brand' ? 'bg-brand-soft text-brand' : 'bg-cloud text-ink'}`}>
        <Icon name="pin" className="size-3.5" />
      </span>
      <span className="min-w-0">
        <span className="block text-xs font-bold text-muted">{label}</span>
        <span className="block break-words font-bold">{value}</span>
      </span>
    </li>
  )
}

function RouteMap({ trip }: { trip: TripDetail }) {
  const { t } = useLang()
  const [map, setMap] = useState<L.Map | null>(null)

  useEffect(() => {
    if (!map) return
    const group = L.layerGroup().addTo(map)
    const path: L.LatLngExpression[] = [
      [trip.pickup.lat, trip.pickup.lng],
      ...trip.stops.map((stop): L.LatLngExpression => [stop.lat, stop.lng]),
      [trip.dropoff.lat, trip.dropoff.lng],
    ]
    L.polyline(path, routeLineStyle).addTo(group)
    L.marker([trip.pickup.lat, trip.pickup.lng], { icon: pinIcon('brand') })
      .bindPopup(`<strong>${escapeHtml(t('pickup'))}</strong><br>${escapeHtml(trip.pickup.name ?? trip.pickup.address)}`)
      .addTo(group)
    trip.stops.forEach((stop, index) => {
      L.marker([stop.lat, stop.lng], { icon: stopIcon(index + 1) })
        .bindPopup(`<strong>${escapeHtml(t('stop'))} ${index + 1}</strong><br>${escapeHtml(stop.name ?? stop.address)}`)
        .addTo(group)
    })
    L.marker([trip.dropoff.lat, trip.dropoff.lng], { icon: pinIcon('ink') })
      .bindPopup(`<strong>${escapeHtml(t('dropoff'))}</strong><br>${escapeHtml(trip.dropoff.name ?? trip.dropoff.address)}`)
      .addTo(group)
    map.fitBounds(L.latLngBounds(path), { padding: [32, 32], maxZoom: 15 })
    return () => {
      group.remove()
    }
  }, [map, trip, t])

  return <MapView className="h-72 w-full lg:h-full lg:min-h-80" onReady={setMap} onDispose={() => setMap(null)} />
}

function Timeline({ trip }: { trip: TripDetail }) {
  const { t, lang } = useLang()
  const cancelled = trip.timeline.cancelledAt !== null
  const steps = TIMELINE_STEPS.map((step) => ({ ...step, at: trip.timeline[step.key] }))
  const rows = cancelled
    ? [...steps.filter((step) => step.at !== null), { key: 'cancelledAt' as const, label: 'tlCancelled' as const, at: trip.timeline.cancelledAt }]
    : steps

  return (
    <ol className="relative space-y-5 border-s-2 border-line ps-6">
      {rows.map((step) => {
        const done = step.at !== null
        const failed = step.key === 'cancelledAt'
        const ring = failed ? 'bg-danger-soft text-danger' : done ? 'bg-brand-soft text-brand' : 'bg-cloud text-muted'
        return (
          <li key={step.key} className="relative">
            <span className={`absolute -start-[31px] top-1 grid size-5 place-items-center rounded-full ring-4 ring-white ${ring}`}>
              <Icon name={failed ? 'x' : done ? 'check' : 'clock'} className="size-3" />
            </span>
            <p className={`font-bold ${done ? '' : 'text-muted'}`}>{t(step.label)}</p>
            <p className="mt-0.5 text-xs text-muted">{done ? formatDateTime(step.at, lang) : t('notYet')}</p>
          </li>
        )
      })}
    </ol>
  )
}

function EventsTable({ events }: { events: TripEvent[] }) {
  const { t, lang } = useLang()
  const [expanded, setExpanded] = useState<number | null>(null)
  const keyOf = (event: TripEvent, index: number) => event.id ?? `${event.type}-${event.createdAt}-${index}`
  const rows = events.map((event, index) => ({ ...event, rowKey: keyOf(event, index), index }))

  const columns: Column<(typeof rows)[number]>[] = [
    { key: 'type', header: t('eventType'), render: (row) => <Badge tone="ink">{row.type}</Badge> },
    {
      key: 'actor',
      header: t('actor'),
      render: (row) => (
        <span>
          <span className="block font-bold">{t(TRIP_ACTOR_KEY[row.actor] ?? 'actorSystem')}</span>
          {row.actorName && <span className="block text-xs text-muted">{row.actorName}</span>}
        </span>
      ),
    },
    { key: 'createdAt', header: t('createdAt'), render: (row) => <span className="whitespace-nowrap">{formatDateTime(row.createdAt, lang)}</span> },
    {
      key: 'details',
      header: t('details'),
      className: 'text-end',
      render: (row) =>
        row.data !== undefined && row.data !== null ? (
          <Button
            variant="secondary"
            size="sm"
            icon={expanded === row.index ? 'x' : 'eye'}
            onClick={() => setExpanded((current) => (current === row.index ? null : row.index))}
          >
            {expanded === row.index ? t('hideDetails') : t('showDetails')}
          </Button>
        ) : null,
    },
  ]

  return (
    <Table
      columns={columns}
      rows={rows}
      rowKey={(row) => row.rowKey}
      emptyTitle={t('noEvents')}
      emptyDescription=""
      renderExpanded={(row) => (expanded === row.index ? <JsonView label={t('details')} value={row.data} /> : null)}
    />
  )
}
