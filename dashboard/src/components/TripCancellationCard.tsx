import { useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { cancellations } from '../lib/admin'
import { ACTOR_KEY, AT_FAULT_KEY, STAGE_KEY } from '../lib/cancellation'
import { formatDateTime, formatNumber } from '../lib/format'
import { excuseStatusMeta, feeStatusMeta } from '../lib/status'
import type { CancellationEvent, TripDetail } from '../lib/types'
import { MetaBadge } from './Badge'
import { Button } from './Button'
import { Card } from './Card'
import { ExcuseReviewModal } from './ExcuseReviewModal'
import { Money } from './Money'

/**
 * "الإلغاء" section of the trip page (docs/09 "وفي /trips/:id"): stage, actor, reason, fee, compensation and
 * excuse status. `Trip.cancellation` gives the summary; the matching `/admin/cancellations` row adds the event
 * id (for the excuse review), the user and the compensation.
 */
export function TripCancellationCard({ trip, onChanged }: { trip: TripDetail; onChanged: () => void }) {
  const { t, lang } = useLang()
  const summary = trip.cancellation ?? null
  const eventQuery = useQuery(async () => {
    const page = await cancellations.list({ search: trip.tripNumber, page: 1, pageSize: 10 })
    return page.items.find((item) => item.tripId === trip.id) ?? null
  }, `trip-cancellation:${trip.id}:${trip.status}`)
  const [review, setReview] = useState<'approve' | 'reject' | null>(null)

  const event: CancellationEvent | null = eventQuery.data ?? null
  if (!summary && !event) return null

  const stage = event?.stage ?? summary?.stage ?? 'before_accept'
  const actor = event?.actor ?? summary?.actor ?? trip.cancelledBy ?? null
  const atFault = event?.atFault ?? summary?.atFault ?? 'none'
  const fee = event?.feeAmount ?? summary?.fee ?? null
  const feeCharged = event?.feeCharged ?? summary?.feeCharged ?? null
  const feeStatus = event?.feeStatus ?? summary?.feeStatus ?? 'none'
  const compensation = event?.compensationAmount ?? summary?.compensation ?? null
  const points = event?.penaltyPoints ?? summary?.penaltyPoints ?? null
  const excuseStatus = event?.excuseStatus ?? summary?.excuseStatus ?? 'not_applicable'
  const eventId = event?.id ?? summary?.eventId ?? summary?.id ?? null
  const note = event?.note ?? summary?.note ?? null
  const reviewNote = event?.reviewNote ?? summary?.reviewNote ?? null

  const cell = (label: string, value: ReactNode) => (
    <div className="min-w-0 rounded-2xl bg-cloud px-4 py-3">
      <p className="text-xs font-bold text-muted">{label}</p>
      <div className="mt-1 font-bold">{value}</div>
    </div>
  )

  return (
    <Card
      title={t('cxTripSection')}
      className="mb-6"
      action={
        <Link to={`/cancellation/events?search=${encodeURIComponent(trip.tripNumber)}`} className="text-sm font-bold text-brand">
          {t('cxEventsTitle')}
        </Link>
      }
    >
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {cell(t('cxStage'), t(STAGE_KEY[stage] ?? 'cxStageBeforeAccept'))}
        {cell(
          t('actor'),
          <>
            {actor ? t(ACTOR_KEY[actor] ?? 'actorSystem') : '—'}
            {event?.userName && <span className="block text-xs font-normal text-muted">{event.userName}</span>}
          </>,
        )}
        {cell(t('cxAtFault'), t(AT_FAULT_KEY[atFault] ?? 'cxFaultNone'))}
        {cell(
          t('reason'),
          <>
            <span className="block truncate">{event?.reasonName ?? summary?.reasonName ?? event?.reasonCode ?? summary?.reasonCode ?? trip.cancellationReason ?? '—'}</span>
            {note && <span className="block truncate text-xs font-normal text-muted">{note}</span>}
          </>,
        )}
        {cell(
          t('cxFee'),
          <span className="flex flex-wrap items-center gap-2">
            <Money value={feeCharged ?? fee} strong />
            <MetaBadge record={feeStatusMeta} value={feeStatus} />
          </span>,
        )}
        {cell(t('cxCompensation'), compensation ? <Money value={compensation} /> : <span className="text-muted">—</span>)}
        {cell(t('cxPoints'), <span className="ltr-nums">{points === null ? '—' : formatNumber(points)}</span>)}
        {cell(
          t('cxExcuse'),
          <>
            <MetaBadge record={excuseStatusMeta} value={excuseStatus} />
            {event?.reviewedAt && <span className="mt-1 block text-xs font-normal text-muted">{`${event.reviewedByName ?? ''} · ${formatDateTime(event.reviewedAt, lang)}`}</span>}
            {reviewNote && <span className="mt-1 block truncate text-xs font-normal text-muted">{reviewNote}</span>}
          </>,
        )}
      </div>

      {excuseStatus === 'pending' && eventId && (
        <div className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-amber-50 px-4 py-3 text-sm text-amber-700">
          <p className="font-bold">{t('cxExcuseAwaitingReview')}</p>
          <span className="inline-flex gap-2">
            <Button variant="brand" size="sm" icon="check" onClick={() => setReview('approve')}>
              {t('approve')}
            </Button>
            <Button variant="danger-outline" size="sm" icon="x" onClick={() => setReview('reject')}>
              {t('reject')}
            </Button>
          </span>
        </div>
      )}

      <ExcuseReviewModal
        target={
          review && eventId
            ? {
                id: eventId,
                tripNumber: trip.tripNumber,
                actor,
                userName: event?.userName,
                reasonName: event?.reasonName ?? summary?.reasonName ?? null,
                reasonCode: event?.reasonCode ?? summary?.reasonCode ?? '',
                note,
                stage,
                feeAmount: fee,
                feeCharged,
                penaltyPoints: points,
              }
            : null
        }
        decision={review ?? 'approve'}
        onClose={() => setReview(null)}
        onReviewed={() => {
          setReview(null)
          eventQuery.reload()
          onChanged()
        }}
      />
    </Card>
  )
}
