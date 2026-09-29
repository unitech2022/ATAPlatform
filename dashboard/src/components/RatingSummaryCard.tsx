import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { useRatingTags } from '../hooks/useRatingTags'
import { ratings } from '../lib/admin'
import { formatDate, formatNumber } from '../lib/format'
import { formatAvg, summarizeRatings } from '../lib/rewards'
import type { RaterRole } from '../lib/types'
import { Card } from './Card'
import { ErrorState } from './ErrorState'
import { Icon } from './Icon'
import { PageSpinner } from './Spinner'
import { Stars } from './Stars'

const SAMPLE_SIZE = 100

/**
 * Ratings a user received, broken down by stars and tag (§F15.2 meaning: ≥ 4 liked, ≤ 3 disliked).
 * Built from `GET /admin/ratings?userId=&raterRole=<counterpart>` (latest 100) because the admin API
 * exposes no per-user summary; the stored average (`ratingAvg`) is the weighted one when provided.
 * A 403 (no `ratings.manage`) hides the card.
 */
export function RatingSummaryCard({
  userId,
  role,
  ratingAvg,
  ratingCount,
  bare = false,
}: {
  userId: string
  /** Role of the user being rated. */
  role: RaterRole
  ratingAvg?: number | null
  ratingCount?: number | null
  bare?: boolean
}) {
  const { t, lang } = useLang()
  const tags = useRatingTags()
  const raterRole: RaterRole = role === 'driver' ? 'passenger' : 'driver'
  const query = useQuery(() => ratings.list({ userId, raterRole, page: 1, pageSize: SAMPLE_SIZE }), `rating-summary:${userId}:${raterRole}`)
  if (query.error?.status === 403) return null

  const summary = query.data ? summarizeRatings(query.data.items) : null
  const average = typeof ratingAvg === 'number' ? ratingAvg : (summary?.average ?? null)
  const total = typeof ratingCount === 'number' ? ratingCount : (query.data?.total ?? summary?.count ?? 0)
  const maxBucket = summary ? Math.max(1, ...Object.values(summary.distribution)) : 1

  const body =
    query.loading && !query.data ? (
      <PageSpinner />
    ) : query.error ? (
      <ErrorState error={query.error} onRetry={query.reload} />
    ) : !summary || summary.count === 0 ? (
      <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('rtNoRatingsYet')}</p>
    ) : (
      <div className="grid gap-5 lg:grid-cols-3">
        <div className="rounded-2xl bg-cloud p-4">
          <p className="text-xs font-bold text-muted">{t('rtAverage')}</p>
          <p className="ltr-nums mt-1 flex items-center gap-2 text-3xl font-bold">
            <Icon name="star" className="size-6 text-amber-500" />
            {formatAvg(average)}
          </p>
          <p className="ltr-nums mt-1 text-xs text-muted">
            {formatNumber(total)} {t('rtRatingsCount')}
          </p>
          <div className="mt-4 space-y-1.5">
            {([5, 4, 3, 2, 1] as const).map((stars) => (
              <div key={stars} className="flex items-center gap-2 text-xs">
                <span className="ltr-nums w-4 font-bold">{stars}</span>
                <span className="block h-1.5 flex-1 overflow-hidden rounded-full bg-line">
                  <span className={`block h-full rounded-full ${stars <= 2 ? 'bg-danger' : stars === 3 ? 'bg-amber-500' : 'bg-brand'}`} style={{ width: `${(summary.distribution[stars] / maxBucket) * 100}%` }} />
                </span>
                <span className="ltr-nums w-8 text-end text-muted">{formatNumber(summary.distribution[stars])}</span>
              </div>
            ))}
          </div>
        </div>
        <div>
          <p className="mb-2 text-xs font-bold text-muted">{t('rtByTag')}</p>
          {summary.tags.length === 0 ? (
            <p className="text-sm text-muted">—</p>
          ) : (
            <ul className="space-y-2">
              {summary.tags.slice(0, 8).map((tag) => (
                <li key={tag.code} className="flex items-center justify-between gap-2 rounded-xl border border-line px-3 py-2 text-sm">
                  <span className="truncate font-bold">{tags.label(tag.code)}</span>
                  <span className="ltr-nums flex shrink-0 gap-2 text-xs">
                    <span className="text-brand" title={t('rtTagsLiked')}>
                      +{formatNumber(tag.positive)}
                    </span>
                    <span className="text-danger" title={t('rtTagsDisliked')}>
                      −{formatNumber(tag.negative)}
                    </span>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
        <div>
          <p className="mb-2 text-xs font-bold text-muted">{t('rtRecentComments')}</p>
          {summary.recentComments.length === 0 ? (
            <p className="text-sm text-muted">—</p>
          ) : (
            <ul className="space-y-2">
              {summary.recentComments.map((row) => (
                <li key={row.id} className="rounded-xl bg-cloud px-3 py-2 text-sm">
                  <span className="mb-1 flex items-center justify-between gap-2 text-xs text-muted">
                    <Stars value={row.stars} low={row.stars <= 2} />
                    {formatDate(row.createdAt, lang)}
                  </span>
                  <span className="line-clamp-2 break-words">{row.comment}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    )

  const link = (
    <Link to={`/ratings?userId=${userId}&raterRole=${raterRole}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
      {t('rtAllRatings')}
      <Icon name="chevron" className="size-4 rtl:rotate-180" />
    </Link>
  )

  if (bare) {
    return (
      <div>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm font-bold">{t('rtSummaryTitle')}</p>
          {link}
        </div>
        {body}
      </div>
    )
  }

  return (
    <Card title={t('rtSummaryTitle')} description={t('rtSummaryCopy')} className="mb-6" action={link}>
      {body}
    </Card>
  )
}
