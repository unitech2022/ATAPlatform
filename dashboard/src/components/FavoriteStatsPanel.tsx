import { Link } from 'react-router'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { favorites } from '../lib/admin'
import { favoriteRateShare } from '../lib/favorites'
import { formatMoney, formatNumber } from '../lib/format'
import { daysAgoIso, todayIso } from '../lib/pricing'
import { formatRatio } from '../lib/rewards'
import type { FavoriteTopDriver } from '../lib/types'
import { Card } from './Card'
import { CityField } from './CityField'
import { ErrorState } from './ErrorState'
import { Input } from './Field'
import { Button } from './Button'
import { StatCard } from './StatCard'
import { Table, type Column } from './Table'
import { UsageBar } from './UsageBar'

/** Default window of the stats view (last 30 days including today). */
const DEFAULT_DAYS = 29

/** Favorite-driver KPIs and top favorited drivers (`GET /admin/favorites/stats`, §F16.3) with a date range and city filter in the URL. */
export function FavoriteStatsPanel() {
  const { t } = useLang()
  const { params, setFilter, update } = useUrlState()
  const from = parseIsoDate(params.get('from')) || daysAgoIso(DEFAULT_DAYS)
  const to = parseIsoDate(params.get('to')) || todayIso()
  const cityId = params.get('cityId') ?? ''
  const query = useQuery(() => favorites.stats({ from, to, cityId: cityId || undefined }), `favorite-stats:${from}:${to}:${cityId}`)
  const stats = query.data
  const pending = query.loading && !stats
  const value = (text: string) => (pending ? '…' : text)
  const requests = stats?.favoriteRequests ?? 0
  const acceptShare = stats && requests > 0 ? Math.min(1, stats.accepted / requests) : null

  const preset = (days: number) =>
    update((next) => {
      next.set('from', daysAgoIso(days - 1))
      next.set('to', todayIso())
    })

  const topDrivers = [...(stats?.topDrivers ?? [])].sort((a, b) => b.favoritesCount - a.favoritesCount || b.favoriteTrips - a.favoriteTrips)
  const rankOf = new Map(topDrivers.map((row, index) => [row.driverId, index + 1]))

  const columns: Column<FavoriteTopDriver>[] = [
    { key: 'rank', header: '#', className: 'w-10', render: (row) => <span className="ltr-nums text-muted">{rankOf.get(row.driverId)}</span> },
    {
      key: 'name',
      header: t('driver'),
      render: (row) => (
        <Link to={`/drivers/${row.driverId}`} className="font-bold text-brand hover:underline">
          {row.name || t('unnamed')}
        </Link>
      ),
    },
    { key: 'favoritesCount', header: t('fvFavoritesCount'), className: 'text-center', render: (row) => <span className="ltr-nums font-bold">{formatNumber(row.favoritesCount)}</span> },
    { key: 'favoriteTrips', header: t('fvFavoriteTrips'), className: 'text-center', render: (row) => <span className="ltr-nums">{formatNumber(row.favoriteTrips)}</span> },
  ]

  return (
    <div className="space-y-6">
      <div className="grid gap-3 rounded-3xl bg-white p-4 shadow-soft sm:grid-cols-2 lg:grid-cols-5">
        <Input id="fv-stats-from" type="date" label={t('fromDate')} dir="ltr" value={from} max={to} onChange={(event) => setFilter('from', event.target.value)} />
        <Input id="fv-stats-to" type="date" label={t('toDate')} dir="ltr" value={to} min={from} onChange={(event) => setFilter('to', event.target.value)} />
        <CityField id="fv-stats-city" label={t('city')} value={cityId} onChange={(value) => setFilter('cityId', value)} />
        <div className="flex items-end gap-2 sm:col-span-2 lg:col-span-2">
          <Button variant="secondary" size="sm" onClick={() => preset(7)}>
            {t('fvLast7Days')}
          </Button>
          <Button variant="secondary" size="sm" onClick={() => preset(30)}>
            {t('fvLast30Days')}
          </Button>
          <Button variant="secondary" size="sm" onClick={() => preset(90)}>
            {t('fvLast90Days')}
          </Button>
        </div>
      </div>

      {query.error ? (
        <Card>
          <ErrorState error={query.error} onRetry={query.reload} />
        </Card>
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            <StatCard title={t('fvKpiBookingRate')} icon="heart" value={value(formatRatio(favoriteRateShare(stats?.favoriteBookingRate)))} meta={t('fvKpiBookingRateMeta')} />
            <StatCard title={t('fvKpiRequests')} icon="send" value={value(formatNumber(stats?.favoriteRequests))} />
            <StatCard title={t('fvKpiAccepted')} icon="check" value={value(formatNumber(stats?.accepted))} meta={acceptShare === null ? undefined : formatRatio(acceptShare)} />
            <StatCard title={t('fvKpiFallback')} icon="refresh" tone="danger" value={value(formatNumber(stats?.fallback))} meta={t('fvKpiFallbackMeta')} />
            <StatCard title={t('fvKpiUsage')} icon="tag" value={value(formatNumber(stats?.discountUsageCount))} />
            <StatCard title={t('fvKpiDiscountTotal')} icon="wallet" value={value(`${formatMoney(stats?.discountTotal)} ${t('sar')}`)} />
          </div>

          {stats && requests > 0 && (
            <Card title={t('fvOutcomeSplit')} description={t('fvOutcomeSplitCopy')}>
              <UsageBar share={acceptShare} />
              <p className="mt-2 flex flex-wrap items-center justify-between gap-2 text-xs text-muted">
                <span>
                  {t('fvKpiAccepted')}: <span className="ltr-nums font-bold text-ink">{formatNumber(stats.accepted)}</span>
                </span>
                <span>
                  {t('fvKpiFallback')}: <span className="ltr-nums font-bold text-ink">{formatNumber(stats.fallback)}</span>
                </span>
                <span>
                  {t('fvKpiRequests')}: <span className="ltr-nums font-bold text-ink">{formatNumber(requests)}</span>
                </span>
              </p>
            </Card>
          )}

          <Card title={t('fvTopDrivers')} description={t('fvTopDriversCopy')} flush>
            <Table columns={columns} rows={topDrivers} rowKey={(row) => row.driverId} loading={query.loading && !stats} emptyTitle={t('fvTopDriversEmpty')} emptyDescription="" />
          </Card>
        </>
      )}
    </div>
  )
}
