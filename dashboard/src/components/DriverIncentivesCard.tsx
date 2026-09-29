import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { incentives } from '../lib/admin'
import { formatDate, formatNumber } from '../lib/format'
import { INCENTIVE_TYPE_KEY, usageShare } from '../lib/rewards'
import { incentiveProgressStatusMeta } from '../lib/status'
import { Badge, MetaBadge } from './Badge'
import { Card } from './Card'
import { Money } from './Money'
import { UsageBar } from './UsageBar'

/**
 * A driver's incentive progress. Uses the assumed `GET /admin/drivers/{id}/incentives` (the contract only
 * lists progress per incentive); the card disappears when the endpoint answers 404/403.
 */
export function DriverIncentivesCard({ driverId }: { driverId: string }) {
  const { t, lang } = useLang()
  const query = useQuery(() => incentives.forDriver(driverId), `driver-incentives:${driverId}`)
  if (query.error || (query.loading && !query.data)) return null
  const rows = [...(query.data ?? [])].sort((a, b) => new Date(b.periodStart).getTime() - new Date(a.periodStart).getTime())

  return (
    <Card title={t('icDriverCardTitle')} className="mb-6">
      {rows.length === 0 ? (
        <p className="rounded-2xl bg-cloud px-4 py-3 text-sm text-muted">{t('icNoProgress')}</p>
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2">
          {rows.slice(0, 6).map((row) => (
            <li key={row.id} className="rounded-2xl border border-line p-4">
              <div className="mb-2 flex items-start justify-between gap-2">
                <Link to={`/incentives/${row.incentiveId}`} className="min-w-0 font-bold hover:text-brand">
                  <span className="block truncate">{row.name ?? row.incentiveName ?? t('icTitle')}</span>
                  {row.type && <span className="block text-xs font-normal text-muted">{t(INCENTIVE_TYPE_KEY[row.type] ?? 'icTypeOneTime')}</span>}
                </Link>
                <MetaBadge record={incentiveProgressStatusMeta} value={row.status} />
              </div>
              <p className="ltr-nums text-sm font-bold">
                {formatNumber(row.completedTrips)} / {formatNumber(row.targetTrips)} {t('icTrips')}
              </p>
              <UsageBar share={usageShare(row.completedTrips, row.targetTrips)} className="mt-1.5" />
              <div className="mt-2 flex flex-wrap items-center justify-between gap-2 text-xs text-muted">
                <span>
                  {formatDate(row.periodStart, lang)}
                  {row.periodEnd ? ` – ${formatDate(row.periodEnd, lang)}` : ''}
                </span>
                <span className="flex items-center gap-1.5">
                  {typeof row.incentiveMultiplier === 'number' && row.incentiveMultiplier < 1 && <Badge tone="danger">{t('icReduced')}</Badge>}
                  {row.rewardAmount !== null && <Money value={row.rewardAmount} />}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}
