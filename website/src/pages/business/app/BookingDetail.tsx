import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { TripStatusPill } from '../../../components/business/StatusPills'
import { Modal, PageHeader } from '../../../components/business/ui'
import { Button } from '../../../components/Button'
import { Card } from '../../../components/Card'
import { Field, Select } from '../../../components/Field'
import { Icon } from '../../../components/Icon'
import { Notice } from '../../../components/Notice'
import { ErrorState, LoadingState } from '../../../components/States'
import { TrackingMap } from '../../../components/TrackingMap'
import { useI18n, type TranslationKey } from '../../../i18n'
import { useFlash } from '../../../lib/useFlash'
import { catalogApi, corporateApi } from '../../../lib/api'
import { CANCELLABLE_TRIP_STATUSES, TERMINAL_TRIP_STATUSES } from '../../../lib/corporate'
import { describeError } from '../../../lib/errors'
import { formatDateTime, formatMoney, formatNumber } from '../../../lib/format'
import type { CancellationPreview, Trip } from '../../../lib/types'
import { useResource } from '../../../lib/useResource'

const POLL_MS = 10_000

function CancelDialog({ trip, open, onClose, onCancelled }: { trip: Trip; open: boolean; onClose: () => void; onCancelled: () => void }) {
  const { t, lang } = useI18n()
  const reasons = useResource(() => catalogApi.cancellationReasons(), [lang])
  const [reasonCode, setReasonCode] = useState('')
  const [note, setNote] = useState('')
  const [preview, setPreview] = useState<CancellationPreview | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const reason = reasons.data?.find((item) => item.code === reasonCode) ?? null

  useEffect(() => {
    if (!open) return
    let cancelled = false
    corporateApi
      .cancelPreview(trip.id, reasonCode || undefined)
      .then((result) => {
        if (!cancelled) setPreview(result)
      })
      .catch(() => {
        if (!cancelled) setPreview(null)
      })
    return () => {
      cancelled = true
    }
  }, [open, reasonCode, trip.id])

  const submit = async () => {
    if (!reasonCode) return
    setBusy(true)
    setError(null)
    try {
      await corporateApi.cancelBooking(trip.id, { reasonCode, note: note.trim() || undefined })
      onCancelled()
    } catch (caught) {
      setError(describeError(caught, t))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal open={open} title={t('biz.cancel.title')} onClose={onClose}>
      <div className="space-y-4">
        <Field label={t('biz.cancel.reason')} htmlFor="cancel-reason">
          <Select id="cancel-reason" value={reasonCode} onChange={(event) => setReasonCode(event.target.value)}>
            <option value="">{t('biz.cancel.chooseReason')}</option>
            {(reasons.data ?? []).map((item) => (
              <option key={item.code} value={item.code}>
                {item.name}
              </option>
            ))}
          </Select>
        </Field>
        {reason?.requiresNote && (
          <Field label={t('biz.cancel.note')} htmlFor="cancel-note">
            <textarea id="cancel-note" rows={3} value={note} onChange={(event) => setNote(event.target.value)} className="field-control h-auto py-3" />
          </Field>
        )}
        {preview && (
          <Notice tone={preview.isFree ? 'success' : 'info'}>
            {preview.message ?? (preview.isFree ? t('biz.cancel.free') : t('biz.cancel.fee', { fee: formatMoney(preview.fee, lang) }))}
          </Notice>
        )}
        {error && <Notice tone="error">{error}</Notice>}
        <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {t('biz.cancel.keep')}
          </Button>
          <Button variant="danger" onClick={() => void submit()} disabled={busy || !reasonCode || (reason?.requiresNote === true && !note.trim())}>
            {busy ? t('action.saving') : t('biz.cancel.confirm')}
          </Button>
        </div>
      </div>
    </Modal>
  )
}

const timelineSteps: { key: keyof Trip['timeline']; label: TranslationKey }[] = [
  { key: 'requestedAt', label: 'biz.timeline.requested' },
  { key: 'assignedAt', label: 'biz.timeline.assigned' },
  { key: 'arrivedAt', label: 'biz.timeline.arrived' },
  { key: 'startedAt', label: 'biz.timeline.started' },
  { key: 'completedAt', label: 'biz.timeline.completed' },
  { key: 'cancelledAt', label: 'biz.timeline.cancelled' },
]

export function BookingDetail() {
  const { tripId = '' } = useParams()
  const { t, lang } = useI18n()
  const trip = useResource(() => corporateApi.booking(tripId), [tripId, lang])
  const [cancelOpen, setCancelOpen] = useState(false)
  const [flash, setFlash] = useFlash()
  const data = trip.data
  const status = data?.status
  const reload = trip.reload

  useEffect(() => {
    if (!status || TERMINAL_TRIP_STATUSES.includes(status)) return
    const timer = window.setInterval(() => reload(true), POLL_MS)
    return () => window.clearInterval(timer)
  }, [status, reload])

  const back = (
    <Link to="/business/app/bookings" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-muted hover:text-ink">
      <Icon name="arrow" className="size-4 ltr:rotate-180" />
      {t('biz.bookings.title')}
    </Link>
  )

  if (trip.loading && !data) return <LoadingState />
  if (trip.error && !data) {
    return (
      <>
        {back}
        <ErrorState error={trip.error} onRetry={() => trip.reload()} />
      </>
    )
  }
  if (!data) return null

  const rider = data.corporate?.isGuest ? data.corporate.guestName : (data.corporate?.employeeName ?? null)
  const fare = data.finalFare ?? data.estimatedFare

  return (
    <>
      <title>{`${data.tripNumber} · ATA`}</title>
      {back}
      <PageHeader
        title={data.tripNumber}
        subtitle={data.scheduledAt ? t('biz.trip.scheduledFor', { time: formatDateTime(data.scheduledAt, lang) }) : undefined}
        actions={
          <>
            <TripStatusPill status={data.status} />
            {CANCELLABLE_TRIP_STATUSES.includes(data.status) && (
              <Button variant="danger" size="sm" onClick={() => setCancelOpen(true)}>
                {t('biz.cancel.cta')}
              </Button>
            )}
          </>
        }
      />
      {flash && (
        <Notice tone="success" className="mb-4">
          {flash}
        </Notice>
      )}

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="space-y-4">
          <TrackingMap
            className="h-72 sm:h-96"
            pickup={data.pickup}
            dropoff={data.dropoff}
            stops={data.stops}
            planned={[]}
            travelled={[]}
            driver={null}
          />
          <Card>
            <h2 className="mb-4 font-bold">{t('share.route')}</h2>
            <dl className="space-y-3 text-sm">
              <div>
                <dt className="text-xs font-bold text-muted">{t('share.pickup')}</dt>
                <dd className="font-bold">{data.pickup.name}</dd>
              </div>
              {data.stops.map((stop, index) => (
                <div key={`${stop.lat}-${stop.lng}-${index}`}>
                  <dt className="text-xs font-bold text-muted">{t('share.stop', { n: index + 1 })}</dt>
                  <dd className="font-bold">{stop.name}</dd>
                </div>
              ))}
              <div>
                <dt className="text-xs font-bold text-muted">{t('share.dropoff')}</dt>
                <dd className="font-bold">{data.dropoff.name}</dd>
              </div>
            </dl>
          </Card>
        </div>

        <div className="space-y-4">
          <Card className="space-y-3 text-sm">
            <h2 className="font-bold">{t('biz.trip.details')}</h2>
            <Row label={t('biz.trip.rider')} value={rider ?? '—'} extra={data.corporate?.isGuest ? t('biz.trip.guest') : undefined} />
            <Row label={t('biz.book.category')} value={data.rideCategory?.name ?? '—'} />
            <Row label={t('biz.book.purpose')} value={data.corporate?.purpose ?? '—'} />
            <Row label={t('biz.employee.costCenter')} value={data.corporate?.costCenter ?? '—'} />
            <Row
              label={data.finalFare !== null ? t('biz.trip.finalFare') : t('biz.trip.estimatedFare')}
              value={formatMoney(fare, lang)}
            />
            {data.estimatedDistanceMeters !== null && (
              <Row label={t('biz.trip.distance')} value={`${formatNumber(data.estimatedDistanceMeters / 1000, 1)} km`} />
            )}
            {data.cancellation && (
              <Row
                label={t('biz.trip.cancellation')}
                value={data.cancellation.reasonName ?? data.cancellationReason ?? '—'}
                extra={data.cancellation.feeCharged ? formatMoney(data.cancellation.feeCharged, lang) : undefined}
              />
            )}
          </Card>

          <Card className="space-y-3 text-sm">
            <h2 className="font-bold">{t('share.driver')}</h2>
            {data.driver ? (
              <>
                <Row label={t('biz.trip.driverName')} value={data.driver.fullName ?? '—'} />
                {data.driver.ratingAvg !== null && <Row label={t('biz.trip.rating')} value={formatNumber(data.driver.ratingAvg)} />}
                {data.vehicle && (
                  <>
                    <Row label={t('share.vehicle')} value={`${data.vehicle.make} ${data.vehicle.model} · ${data.vehicle.color}`} />
                    <div className="flex items-center justify-between gap-3">
                      <span className="text-muted">{t('share.plate')}</span>
                      <span dir="ltr" className="rounded-lg border-2 border-ink px-2 py-0.5 font-bold tracking-wider">
                        {data.vehicle.plateNumber}
                      </span>
                    </div>
                  </>
                )}
              </>
            ) : (
              <p className="text-muted">{t('biz.trip.noDriver')}</p>
            )}
          </Card>

          <Card>
            <h2 className="mb-4 font-bold">{t('biz.trip.timeline')}</h2>
            <ol className="space-y-3 text-sm">
              {timelineSteps
                .filter((step) => data.timeline[step.key])
                .map((step) => (
                  <li key={step.key} className="flex items-center justify-between gap-3">
                    <span className="flex items-center gap-2 font-bold">
                      <span className={`size-2.5 rounded-full ${step.key === 'cancelledAt' ? 'bg-danger' : 'bg-brand'}`} />
                      {t(step.label)}
                    </span>
                    <span className="text-muted">{formatDateTime(data.timeline[step.key] ?? '', lang)}</span>
                  </li>
                ))}
            </ol>
          </Card>
        </div>
      </div>

      <CancelDialog
        trip={data}
        open={cancelOpen}
        onClose={() => setCancelOpen(false)}
        onCancelled={() => {
          setCancelOpen(false)
          setFlash(t('biz.cancel.done'))
          trip.reload(true)
        }}
      />
    </>
  )
}

function Row({ label, value, extra }: { label: string; value: string; extra?: string }) {
  return (
    <div className="flex items-start justify-between gap-3">
      <span className="shrink-0 text-muted">{label}</span>
      <span className="min-w-0 text-end font-bold">
        {value}
        {extra && <span className="ms-2 rounded-full bg-cloud px-2 py-0.5 text-xs text-muted">{extra}</span>}
      </span>
    </div>
  )
}
