import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { reliabilityProfiles } from '../lib/admin'
import { formatRate, ROLE_KEY } from '../lib/cancellation'
import { formatDateTime, formatNumber } from '../lib/format'
import { restrictionLevelMeta } from '../lib/status'
import type { ReliabilityRole } from '../lib/types'
import { MetaBadge } from './Badge'
import { Card } from './Card'
import { ErrorState } from './ErrorState'
import { Icon } from './Icon'
import { PageSpinner } from './Spinner'

/**
 * Reliability summary for a driver / passenger (rate, penalty points, restriction) from
 * `GET /admin/reliability-profiles/{userId}?role=`. A 404 means no profile has been computed yet;
 * a 403 (no `reliability.manage`) hides the card entirely.
 */
export function ReliabilityCard({ userId, role, bare = false }: { userId: string; role: ReliabilityRole; bare?: boolean }) {
  const { t, lang } = useLang()
  const query = useQuery(() => reliabilityProfiles.get(userId, role), `reliability-card:${userId}:${role}`)

  if (query.error?.status === 403) return null

  const profile = query.data
  const body =
    query.loading && !profile ? (
      <PageSpinner />
    ) : query.error && query.error.status !== 404 ? (
      <ErrorState error={query.error} onRetry={query.reload} />
    ) : !profile ? (
      <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('rlNoProfileYet')}</p>
    ) : (
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <div className="rounded-2xl bg-cloud px-4 py-3">
          <p className="text-xs font-bold text-muted">{t('rlLevel')}</p>
          <div className="mt-1">
            <MetaBadge record={restrictionLevelMeta} value={profile.level} />
          </div>
          {profile.restrictedUntil && (
            <p className="mt-1 text-xs text-danger">
              {t('rlUntil')} {formatDateTime(profile.restrictedUntil, lang)}
            </p>
          )}
        </div>
        <div className="rounded-2xl bg-cloud px-4 py-3">
          <p className="text-xs font-bold text-muted">{t('rlCancellationRate')}</p>
          <p className="ltr-nums mt-1 text-xl font-bold">{formatRate(profile.cancellationRate)}</p>
        </div>
        <div className="rounded-2xl bg-cloud px-4 py-3">
          <p className="text-xs font-bold text-muted">{t('rlReliabilityRate')}</p>
          <p className="ltr-nums mt-1 text-xl font-bold">{formatRate(profile.reliabilityRate)}</p>
        </div>
        <div className="rounded-2xl bg-cloud px-4 py-3">
          <p className="text-xs font-bold text-muted">{t('rlPenaltyPoints')}</p>
          <p className={`ltr-nums mt-1 text-xl font-bold ${profile.penaltyPoints > 0 ? 'text-danger' : ''}`}>{formatNumber(profile.penaltyPoints)}</p>
          <p className="text-xs text-muted">
            {t('rlNoShows')}: <span className="ltr-nums">{formatNumber(profile.noShowCount)}</span>
          </p>
        </div>
      </div>
    )

  const link = (
    <Link to={`/reliability/${userId}?role=${role}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
      {t('rlOpenProfile')}
      <Icon name="chevron" className="size-4 rtl:rotate-180" />
    </Link>
  )

  if (bare) {
    return (
      <div>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm font-bold">
            {t('rlCardTitle')} · {t(ROLE_KEY[role])}
          </p>
          {link}
        </div>
        {body}
      </div>
    )
  }

  return (
    <Card title={t('rlCardTitle')} className="mb-6" action={link}>
      {body}
    </Card>
  )
}
