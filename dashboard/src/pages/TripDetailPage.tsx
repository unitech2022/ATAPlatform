import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { Badge, TripStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { DefinitionList } from '../components/DefinitionList'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { JsonView } from '../components/JsonView'
import { MapView } from '../components/MapView'
import { Select, Textarea, Toggle } from '../components/Field'
import { Modal } from '../components/Modal'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { TripCancellationCard } from '../components/TripCancellationCard'
import { TripMessagesPanel } from '../components/TripMessagesPanel'
import { TripPaymentCard } from '../components/TripPaymentCard'
import { TripRewardsCard } from '../components/TripRewardsCard'
import { TripSafetyCard } from '../components/TripSafetyCard'
import { TripSchedulingCard } from '../components/TripSchedulingCard'
import { TripSupportCard } from '../components/TripSupportCard'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { matching, trips } from '../lib/admin'
import { formatDateTime, formatKm, formatMinutes, formatMoney, formatNumber } from '../lib/format'
import { escapeHtml, L, pinIcon, routeLineStyle, stopIcon } from '../lib/leaflet'
import { AT_FAULT_KEY, AT_FAULTS } from '../lib/cancellation'
import { MATCHING_OUTCOME_KEY } from '../lib/pricing'
import { isTerminalTripStatus, PAYMENT_METHOD_KEY, PRICING_MODE_KEY, TRIP_ACTOR_KEY } from '../lib/trips'
import type { AtFault, CandidateResponse, MatchingAttempt, MatchingCandidate, TripDetail, TripEvent, TripTimeline } from '../lib/types'

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

  const cancelTrip = async (reason: string, atFault: AtFault, chargeFee: boolean) => {
    try {
      await trips.cancel(id, reason, { atFault, chargeFee })
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
  const isScheduledTrip = trip.bookingType === 'scheduled' || Boolean(trip.scheduling)
  // Scheduled trips carry their own cancel action inside the scheduling section.
  const canCancel = !isTerminalTripStatus(trip.status) && !isScheduledTrip
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
              ...(trip.airport
                ? [
                    { label: t('apTripAirport'), value: `${trip.airport.code} · ${trip.airport.direction === 'pickup' ? t('apDirPickup') : t('apDirDropoff')}` },
                    ...(trip.airport.zoneName ? [{ label: t('apTripZone'), value: trip.airport.zoneName }] : []),
                    ...(trip.airport.terminalCode ? [{ label: t('apTripTerminal'), value: trip.airport.terminalCode, ltr: true }] : []),
                    ...(trip.airport.flightNumber ? [{ label: t('apTripFlight'), value: trip.airport.flightNumber, ltr: true }] : []),
                    ...(typeof trip.airport.freeWaitingMinutes === 'number' ? [{ label: t('apTripFreeWaiting'), value: `${formatNumber(trip.airport.freeWaitingMinutes)} ${t('min')}`, ltr: true }] : []),
                  ]
                : []),
            ]}
          />
        </Card>

        <Card title={t('routeMap')} flush>
          <div className="px-5 pb-5 sm:px-6 sm:pb-6">
            <RouteMap trip={trip} />
          </div>
        </Card>
      </div>

      {isScheduledTrip && <TripSchedulingCard trip={trip} onChanged={query.reload} onCancel={() => setCancelOpen(true)} />}

      <TripPaymentCard trip={trip} />

      <TripRewardsCard trip={trip} />

      {(isCancelled || trip.cancellation) && <TripCancellationCard trip={trip} onChanged={query.reload} />}

      <TripSafetyCard trip={trip} />

      <TripSupportCard trip={trip} />

      <div className="mb-6 grid gap-6 lg:grid-cols-3">
        <Card title={t('timeline')}>
          <Timeline trip={trip} />
        </Card>
        <Card title={t('events')} flush className="lg:col-span-2">
          <EventsTable events={trip.events} />
        </Card>
      </div>

      <Card title={t('matchingSection')} description={t('matchingSectionCopy')} flush className="mb-6">
        <MatchingSection tripId={id} />
      </Card>

      {trip.driver && (
        <Card title={t('sfTripChat')} flush className="mb-6">
          <TripMessagesPanel tripId={id} />
        </Card>
      )}

      <CancelTripModal open={cancelOpen} tripNumber={trip.tripNumber} hasDriver={Boolean(trip.driver)} onClose={() => setCancelOpen(false)} onConfirm={cancelTrip} />
    </>
  )
}

/** Admin cancel (F8) extended by F14: who is at fault and whether the matching rule's fee is charged. */
function CancelTripModal({
  open,
  ...props
}: {
  open: boolean
  tripNumber: string
  hasDriver: boolean
  onClose: () => void
  onConfirm: (reason: string, atFault: AtFault, chargeFee: boolean) => Promise<void>
}) {
  return open ? <CancelTripDialog {...props} /> : null
}

function CancelTripDialog({
  tripNumber,
  hasDriver,
  onClose,
  onConfirm,
}: {
  tripNumber: string
  hasDriver: boolean
  onClose: () => void
  onConfirm: (reason: string, atFault: AtFault, chargeFee: boolean) => Promise<void>
}) {
  const { t } = useLang()
  const [reason, setReason] = useState('')
  const [atFault, setAtFault] = useState<AtFault>('none')
  const [chargeFee, setChargeFee] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    if (!reason.trim()) {
      setError(t('reasonRequired'))
      return
    }
    setSubmitting(true)
    try {
      await onConfirm(reason.trim(), atFault, atFault === 'passenger' && chargeFee)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={t('cancelTripTitle')}
      description={`${tripNumber} — ${t('cancelTripCopy')}`}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button variant="danger" onClick={submit} loading={submitting}>
            {t('cancelTrip')}
          </Button>
        </>
      }
    >
      <div className="grid gap-4">
        <Textarea
          id="cancel-reason"
          label={t('reason')}
          placeholder={t('reasonPlaceholder')}
          value={reason}
          error={error}
          autoFocus
          onChange={(event) => {
            setReason(event.target.value)
            setError(null)
          }}
        />
        <Select id="cancel-fault" label={t('cxAtFault')} hint={t('cxAdminFaultHint')} value={atFault} onChange={(event) => setAtFault(event.target.value as AtFault)}>
          {AT_FAULTS.filter((value) => value !== 'driver' || hasDriver).map((value) => (
            <option key={value} value={value}>
              {t(AT_FAULT_KEY[value])}
            </option>
          ))}
        </Select>
        {atFault === 'passenger' && <Toggle checked={chargeFee} onChange={setChargeFee} label={t('cxChargeFee')} description={t('cxChargeFeeCopy')} />}
      </div>
    </Modal>
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

const RESPONSE_META: Record<Exclude<CandidateResponse, null>, { key: TranslationKey; tone: 'brand' | 'danger' | 'muted' }> = {
  accepted: { key: 'respAccepted', tone: 'brand' },
  rejected: { key: 'respRejected', tone: 'danger' },
  expired: { key: 'respExpired', tone: 'muted' },
}

function MatchingSection({ tripId }: { tripId: string }) {
  const { t, lang } = useLang()
  const query = useQuery(() => matching.forTrip(tripId), `trip-matching:${tripId}`)

  if (query.loading && !query.data) return <PageSpinner />
  // Older backends answer 404 for trips created before F9; that is simply "no matching data".
  if (query.error && query.error.status !== 404) return <ErrorState error={query.error} onRetry={query.reload} />

  const attempts = [...(query.data?.attempts ?? [])].sort((a, b) => a.round - b.round)
  if (attempts.length === 0) return <EmptyState icon="target" title={t('noMatching')} />

  return (
    <div className="divide-y divide-line">
      {attempts.map((attempt) => (
        <MatchingRound key={attempt.id} attempt={attempt} lang={lang} />
      ))}
    </div>
  )
}

function MatchingRound({ attempt, lang }: { attempt: MatchingAttempt; lang: 'ar' | 'en' }) {
  const { t } = useLang()
  const navigate = useNavigate()
  const outcome = attempt.outcome ?? 'in_progress'
  const outcomeTone = outcome === 'assigned' ? 'brand' : outcome === 'in_progress' ? 'warning' : outcome === 'cancelled' ? 'muted' : 'danger'
  const candidates = [...attempt.candidates].sort((a, b) => a.rank - b.rank)

  const columns: Column<MatchingCandidate>[] = [
    { key: 'rank', header: t('rank'), className: 'text-center', render: (row) => <span className="ltr-nums font-bold">{formatNumber(row.rank)}</span> },
    {
      key: 'driver',
      header: t('driver'),
      render: (row) => (
        <button type="button" onClick={() => navigate(`/drivers/${row.driverId}`)} className="text-start font-bold text-brand hover:underline">
          {row.driverName || t('unnamed')}
        </button>
      ),
    },
    { key: 'distance', header: t('distance'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatKm(row.distanceMeters)} {t('km')}</span> },
    { key: 'eta', header: t('eta'), className: 'text-end', render: (row) => <span className="ltr-nums">{formatMinutes(row.etaSeconds)} {t('min')}</span> },
    {
      key: 'score',
      header: t('score'),
      render: (row) => (
        <span className="flex min-w-36 items-center gap-2">
          <span className="h-2 flex-1 overflow-hidden rounded-full bg-cloud" dir="ltr">
            <span className="block h-full rounded-full bg-brand" style={{ width: `${Math.round(Math.min(1, Math.max(0, row.score)) * 100)}%` }} />
          </span>
          <span className="ltr-nums w-12 text-end text-xs font-bold">{(Math.round(row.score * 1000) / 1000).toFixed(3)}</span>
        </span>
      ),
    },
    { key: 'offered', header: t('offered'), className: 'text-center', render: (row) => <Badge tone={row.offered ? 'ink' : 'muted'}>{row.offered ? t('yes') : t('no')}</Badge> },
    {
      key: 'response',
      header: t('response'),
      render: (row) => {
        const meta = row.response ? RESPONSE_META[row.response] : null
        return meta ? <Badge tone={meta.tone}>{t(meta.key)}</Badge> : <span className="text-xs text-muted">{row.offered ? '—' : t('respNone')}</span>
      },
    },
  ]

  return (
    <section>
      <header className="flex flex-wrap items-center gap-x-4 gap-y-2 px-5 py-4 text-sm sm:px-6">
        <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand">
          <Icon name="target" className="size-4" />
        </span>
        <span className="font-bold">
          {t('round')} <span className="ltr-nums">{formatNumber(attempt.round)}</span>
        </span>
        <Badge tone={outcomeTone}>{t(MATCHING_OUTCOME_KEY[outcome] ?? 'outcomeInProgress')}</Badge>
        <span className="text-muted">
          {t('searchRadius')}: <span className="ltr-nums font-bold text-ink">{formatKm(attempt.radiusMeters)} {t('km')}</span>
        </span>
        <span className="text-muted">
          {t('candidates')}: <span className="ltr-nums font-bold text-ink">{formatNumber(attempt.candidatesCount)}</span>
        </span>
        <span className="text-xs text-muted">
          {t('startedAt')}: {formatDateTime(attempt.startedAt, lang)}
          {attempt.finishedAt ? ` · ${t('finishedAt')}: ${formatDateTime(attempt.finishedAt, lang)}` : ''}
        </span>
      </header>
      {candidates.length === 0 ? (
        <p className="px-5 pb-5 text-sm text-muted sm:px-6">{t('noCandidates')}</p>
      ) : (
        <Table columns={columns} rows={candidates} rowKey={(row) => `${attempt.id}:${row.driverId}:${row.rank}`} emptyTitle={t('noCandidates')} emptyDescription="" />
      )}
    </section>
  )
}
