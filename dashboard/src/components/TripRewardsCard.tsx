import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useRatingTags } from '../hooks/useRatingTags'
import { ratings } from '../lib/admin'
import { DISCOUNT_SOURCE_KEY, FAVORITE_FALLBACK_STATUSES, FAVORITE_OUTCOME_KEY } from '../lib/favorites'
import { RATING_DIRECTION_KEY } from '../lib/rewards'
import { favoriteStatusMeta, redemptionStatusMeta } from '../lib/status'
import type { TripDetail, TripFavoriteInfo, TripRatingInfo } from '../lib/types'
import { Badge, MetaBadge } from './Badge'
import { Card } from './Card'
import { Money } from './Money'
import { Stars } from './Stars'

/**
 * F15/F16 on the trip page: the promo reservation (`Trip.promotion`), the favorite driver request (`Trip.favorite`,
 * docs/10 §F16.3), the stored discount lines (`fare_breakdown.discounts[]`, docs/08 §F11.6, each with its source) and both ratings. Ratings come from the assumed admin
 * `Trip.ratings[]`; when the trip does not embed them, completed trips look them up in `GET /admin/ratings`
 * by trip number. Renders nothing for trips with no promotion, no discounts and no chance of ratings.
 */
export function TripRewardsCard({ trip }: { trip: TripDetail }) {
  const { t } = useLang()
  const discounts = trip.fareBreakdown?.discounts ?? trip.discounts ?? []
  const discountTotal = trip.fareBreakdown?.discount ?? null
  const completed = trip.status === 'completed'
  if (!trip.promotion && !trip.favorite && discounts.length === 0 && !trip.ratings?.length && !completed) return null

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
          {trip.favorite && <FavoriteInfo favorite={trip.favorite} discounts={discounts} />}
          {discounts.length > 0 && (
            <div className="rounded-2xl border border-line px-4 py-3">
              <p className="mb-2 text-xs font-bold text-muted">{t('tripDiscounts')}</p>
              <ul className="space-y-1.5 text-sm">
                {discounts.map((line, index) => (
                  <li key={`${line.source}-${index}`} className="flex items-center justify-between gap-2">
                    <span className="flex min-w-0 items-center gap-2">
                      <span className="truncate">{line.label || line.reference || line.source}</span>
                      {DISCOUNT_SOURCE_KEY[line.source] && <Badge tone={line.source === 'favorite_driver' ? 'brand' : 'ink'}>{t(DISCOUNT_SOURCE_KEY[line.source])}</Badge>}
                    </span>
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

/** F16: requested favorite driver, exclusive-offer outcome, fallback to normal matching and the favorite discount line. */
function FavoriteInfo({ favorite, discounts }: { favorite: TripFavoriteInfo; discounts: NonNullable<TripDetail['discounts']> }) {
  const { t } = useLang()
  const fallback = FAVORITE_FALLBACK_STATUSES.includes(favorite.status)
  const line = discounts.find((item) => item.source === 'favorite_driver')
  const applied = favorite.discountApplied === true || Boolean(line)
  return (
    <div className="rounded-2xl border border-line px-4 py-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="min-w-0">
          <span className="block text-xs font-bold text-muted">{t('tripFavorite')}</span>
          {favorite.driverId ? (
            <Link to={`/drivers/${favorite.driverId}`} className="font-bold text-brand hover:underline">
              {favorite.driverName || t('unnamed')}
            </Link>
          ) : (
            <span className="font-bold">{favorite.driverName || t('unnamed')}</span>
          )}
        </span>
        <MetaBadge record={favoriteStatusMeta} value={favorite.status} />
      </div>
      <p className="mt-2 text-sm text-muted">{t(FAVORITE_OUTCOME_KEY[favorite.status] ?? 'fvOutcomeRequested')}</p>
      <div className="mt-2 flex flex-wrap items-center gap-2 text-xs">
        {fallback && <Badge tone="warning">{t('tripFavoriteFallback')}</Badge>}
        <Badge tone={applied ? 'brand' : 'muted'}>{applied ? t('tripFavoriteDiscountApplied') : t('tripFavoriteNoDiscount')}</Badge>
        {line && <Money value={-Math.abs(line.amount)} signed strong />}
        {favorite.discountRuleId && (
          <Link to="/favorites" className="font-bold text-brand hover:underline">
            {t('tripFavoriteRule')}: {favorite.discountRuleName || favorite.discountRuleId}
          </Link>
        )}
      </div>
    </div>
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
