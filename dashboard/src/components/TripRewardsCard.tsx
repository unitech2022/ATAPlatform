import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useRatingTags } from '../hooks/useRatingTags'
import { ratings } from '../lib/admin'
import { RATING_DIRECTION_KEY } from '../lib/rewards'
import { redemptionStatusMeta } from '../lib/status'
import type { TripDetail, TripRatingInfo } from '../lib/types'
import { Badge, MetaBadge } from './Badge'
import { Card } from './Card'
import { Money } from './Money'
import { Stars } from './Stars'

/**
 * F15 on the trip page: the promo reservation (`Trip.promotion`), the stored discount lines
 * (`fare_breakdown.discounts[]`, docs/08 §F11.6) and both ratings. Ratings come from the assumed admin
 * `Trip.ratings[]`; when the trip does not embed them, completed trips look them up in `GET /admin/ratings`
 * by trip number. Renders nothing for trips with no promotion, no discounts and no chance of ratings.
 */
export function TripRewardsCard({ trip }: { trip: TripDetail }) {
  const { t } = useLang()
  const discounts = trip.fareBreakdown?.discounts ?? trip.discounts ?? []
  const discountTotal = trip.fareBreakdown?.discount ?? null
  const completed = trip.status === 'completed'
  if (!trip.promotion && discounts.length === 0 && !trip.ratings?.length && !completed) return null

  return (
    <Card title={t('tripRewardsTitle')} className="mb-6">
      <div className="grid gap-4 md:grid-cols-2">
        <div className="space-y-3">
          {trip.promotion ? (
            <div className="flex flex-wrap items-center justify-between gap-2 rounded-2xl bg-cloud px-4 py-3">
              <span className="min-w-0">
                <span className="block text-xs font-bold text-muted">{t('tripPromotion')}</span>
                <Link
                  to={trip.promotion.promotionId ? `/promotions/${trip.promotion.promotionId}` : `/promotions?q=${encodeURIComponent(trip.promotion.code)}`}
                  className="ltr-nums font-bold tracking-wider text-brand hover:underline"
                >
                  {trip.promotion.code}
                </Link>
              </span>
              <span className="flex items-center gap-2">
                <MetaBadge record={redemptionStatusMeta} value={trip.promotion.status} />
                {trip.promotion.discountAmount !== null && <Money value={trip.promotion.discountAmount} strong />}
              </span>
            </div>
          ) : (
            <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('tripNoPromotion')}</p>
          )}
          {discounts.length > 0 && (
            <div className="rounded-2xl border border-line px-4 py-3">
              <p className="mb-2 text-xs font-bold text-muted">{t('tripDiscounts')}</p>
              <ul className="space-y-1.5 text-sm">
                {discounts.map((line, index) => (
                  <li key={`${line.source}-${index}`} className="flex items-center justify-between gap-2">
                    <span className="min-w-0 truncate">{line.label || line.reference || line.source}</span>
                    <Money value={-Math.abs(line.amount)} signed />
                  </li>
                ))}
              </ul>
              {typeof discountTotal === 'number' && discounts.length > 1 && (
                <p className="mt-2 flex items-center justify-between gap-2 border-t border-line pt-2 text-sm font-bold">
                  <span>{t('total')}</span>
                  <Money value={-Math.abs(discountTotal)} signed strong />
                </p>
              )}
            </div>
          )}
        </div>
        <div className="space-y-3">
          {trip.ratings !== undefined && trip.ratings !== null ? <RatingList items={trip.ratings} /> : completed ? <LookedUpRatings tripNumber={trip.tripNumber} /> : <RatingList items={[]} />}
        </div>
      </div>
    </Card>
  )
}

/** Fallback when the admin trip payload has no `ratings[]`: search the ratings list by trip number (hidden on 403). */
function LookedUpRatings({ tripNumber }: { tripNumber: string }) {
  const { t } = useLang()
  const query = useQuery(() => ratings.list({ search: tripNumber, page: 1, pageSize: 10 }), `trip-ratings:${tripNumber}`)
  if (query.error?.status === 403) return null
  if (query.loading && !query.data) return <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('loading')}</p>
  if (query.error) return <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('tripNoRatings')}</p>
  const items: TripRatingInfo[] = (query.data?.items ?? [])
    .filter((row) => row.tripNumber === tripNumber)
    .map((row) => ({ id: row.id, raterRole: row.raterRole, stars: row.stars, tags: row.tags, comment: row.comment, status: row.status, createdAt: row.createdAt }))
  return <RatingList items={items} />
}

function RatingList({ items }: { items: TripRatingInfo[] }) {
  const { t } = useLang()
  const tags = useRatingTags()
  if (items.length === 0) return <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('tripNoRatings')}</p>
  return (
    <>
      {items.map((rating, index) => (
        <div key={rating.id ?? index} className="rounded-2xl border border-line px-4 py-3">
          <div className="mb-1 flex flex-wrap items-center justify-between gap-2">
            <span className="text-xs font-bold text-muted">{t(RATING_DIRECTION_KEY[rating.raterRole] ?? 'rtDirPassengerToDriver')}</span>
            <span className="flex items-center gap-2">
              {rating.status === 'hidden' && <Badge tone="muted">{t('rtStatusHidden')}</Badge>}
              <Stars value={rating.stars} low={rating.stars <= 2} />
            </span>
          </div>
          {rating.tags.length > 0 && (
            <div className="mb-1 flex flex-wrap gap-1">
              {rating.tags.map((code) => (
                <Badge key={code} tone={rating.stars >= 4 ? 'brand' : 'danger'}>
                  {tags.label(code)}
                </Badge>
              ))}
            </div>
          )}
          {rating.comment && <p className="break-words text-sm">{rating.comment}</p>}
        </div>
      ))}
    </>
  )
}
