import { Link, useNavigate } from 'react-router'
import { Badge, DriverStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
import { CancellationKpis } from '../components/CancellationKpis'
import { Card, DarkCard } from '../components/Card'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { Icon, type IconName } from '../components/Icon'
import { PageHeader } from '../components/PageHeader'
import { PageSpinner } from '../components/Spinner'
import { StatCard } from '../components/StatCard'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { cancellations, dashboard, drivers, live, payments, payouts, refunds, safety } from '../lib/admin'
import { formatDate, formatMoney, formatNumber } from '../lib/format'
import { daysAgoIso, todayIso } from '../lib/pricing'
import { formatDuration } from '../lib/safety'
import type { DashboardSummary, SafetySummary } from '../lib/types'

const STATS: { key: Exclude<keyof DashboardSummary, 'today'>; label: TranslationKey; icon: IconName; tone?: 'brand' | 'danger' }[] = [
  { key: 'pendingDriverApplications', label: 'statPendingApplications', icon: 'document', tone: 'danger' },
  { key: 'approvedDrivers', label: 'statApprovedDrivers', icon: 'shield' },
  { key: 'onlineDrivers', label: 'statOnlineDrivers', icon: 'car' },
  { key: 'passengers', label: 'statPassengers', icon: 'users' },
  { key: 'tripsToday', label: 'statTripsToday', icon: 'pin' },
  { key: 'usersToday', label: 'statUsersToday', icon: 'user' },
]

const QUICK_LINKS: { key: TranslationKey; icon: IconName; to: string }[] = [
  { key: 'quickReviewApplications', icon: 'document', to: '/drivers?status=submitted' },
  { key: 'quickManagePassengers', icon: 'users', to: '/passengers' },
  { key: 'quickTrips', icon: 'route', to: '/trips' },
  { key: 'quickLiveMap', icon: 'map', to: '/live' },
  { key: 'quickRideCategories', icon: 'layers', to: '/ride-categories' },
  { key: 'quickAuditLogs', icon: 'list', to: '/audit-logs' },
]

export function DashboardPage() {
  const { t, lang } = useLang()
  const { user } = useAuth()
  const navigate = useNavigate()
  const summary = useQuery(() => dashboard.summary(), 'dashboard-summary')
  const pending = useQuery(() => drivers.list({ status: 'submitted', page: 1, pageSize: 5 }), 'dashboard-pending')
  const liveNow = useQuery(() => live.snapshot(), 'dashboard-live')
  const today = todayIso()
  // Finance counters come from the list endpoints' totals (pageSize=1) — docs/08 has no finance summary.
  const captured = useQuery(() => payments.list({ status: 'captured', from: today, to: today, page: 1, pageSize: 1 }), `dashboard-captured:${today}`)
  const pendingPayouts = useQuery(() => payouts.list({ status: 'requested', page: 1, pageSize: 1 }), 'dashboard-payouts')
  const pendingRefunds = useQuery(() => refunds.list({ status: 'pending_approval', page: 1, pageSize: 1 }), 'dashboard-refunds')
  // F12 / F14 — hidden for admins without `safety.manage` / `reports.view` (the calls answer 403).
  const safetySummary = useQuery(() => safety.summary(), 'dashboard-safety')
  const kpiFrom = daysAgoIso(6)
  const cancellationStats = useQuery(() => cancellations.stats({ from: kpiFrom, to: today }), `dashboard-cancellations:${kpiFrom}:${today}`)
  const gmv = summary.data?.today?.gmv
  const financeStats: { key: TranslationKey; value: string; icon: IconName; to: string; tone?: 'brand' | 'danger' }[] = [
    ...(typeof gmv === 'number' ? [{ key: 'statGmvToday' as const, value: `${formatMoney(gmv)} ${t('sar')}`, icon: 'activity' as const, to: '/payments' }] : []),
    { key: 'statCapturedToday', value: countOf(captured), icon: 'wallet', to: `/payments?status=captured&from=${today}&to=${today}` },
    { key: 'statPendingPayouts', value: countOf(pendingPayouts), icon: 'upload', to: '/payouts', tone: 'danger' },
    { key: 'statPendingRefunds', value: countOf(pendingRefunds), icon: 'refresh', to: '/refunds', tone: 'danger' },
  ]
  const financeUnavailable = Boolean(captured.error && pendingPayouts.error && pendingRefunds.error)
  const liveStats = liveNow.data
    ? [
        { key: 'searchingTrips' as const, value: liveNow.data.searchingTrips.length, tone: 'warning' as const },
        { key: 'activeTrips' as const, value: liveNow.data.activeTrips.length, tone: 'brand' as const },
        { key: 'driversOnline' as const, value: liveNow.data.drivers.filter((driver) => driver.isOnline).length, tone: 'ink' as const },
      ]
    : null

  return (
    <>
      <PageHeader
        eyebrow={t('dashboardTitle')}
        title={`${t('dashboardWelcome')} ${user?.fullName?.trim() || t('admin')}`}
        description={t('dashboardCopy')}
      />

      {summary.error ? (
        <Card className="mb-6">
          <ErrorState error={summary.error} onRetry={summary.reload} />
        </Card>
      ) : (
        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {STATS.map((stat) => (
            <StatCard
              key={stat.key}
              title={t(stat.label)}
              icon={stat.icon}
              tone={stat.tone}
              value={summary.loading ? '…' : formatNumber(summary.data?.[stat.key])}
            />
          ))}
        </div>
      )}

      {!financeUnavailable && (
        <div className={`mb-6 grid gap-4 sm:grid-cols-2 ${financeStats.length > 3 ? 'xl:grid-cols-4' : 'xl:grid-cols-3'}`}>
          {financeStats.map((stat) => (
            <Link key={stat.key} to={stat.to} className="block rounded-3xl transition hover:-translate-y-0.5 hover:shadow-brand">
              <StatCard title={t(stat.key)} icon={stat.icon} tone={stat.tone} value={stat.value} meta={t('finance')} />
            </Link>
          ))}
        </div>
      )}

      {(!safetySummary.error || !cancellationStats.error) && (
        <div className="mb-6 grid gap-6 lg:grid-cols-2">
          {!safetySummary.error && <SafetyOverview summary={safetySummary.data} loading={safetySummary.loading} />}
          {!cancellationStats.error && (
            <Card
              title={t('cxKpisTitle')}
              description={t('cxKpisLast7Days')}
              action={
                <Link to={`/cancellation/events?from=${kpiFrom}&to=${today}`} className="inline-flex items-center gap-1 text-sm font-bold text-brand">
                  {t('viewAll')}
                  <Icon name="chevron" className="size-4 rtl:rotate-180" />
                </Link>
              }
            >
              <CancellationKpis stats={cancellationStats.data} loading={cancellationStats.loading} compact />
            </Card>
          )}
        </div>
      )}

      {!liveNow.error && (
        <Card
          className="mb-6"
          title={t('liveNow')}
          description={t('liveNowCopy')}
          action={
            <Link to="/live" className="inline-flex items-center gap-1 text-sm font-bold text-brand">
              {t('openLiveMap')}
              <Icon name="chevron" className="size-4 rtl:rotate-180" />
            </Link>
          }
        >
          <div className="grid gap-3 sm:grid-cols-3">
            {(liveStats ?? [{ key: 'searchingTrips' as const, value: null, tone: 'warning' as const }, { key: 'activeTrips' as const, value: null, tone: 'brand' as const }, { key: 'driversOnline' as const, value: null, tone: 'ink' as const }]).map((stat) => (
              <div key={stat.key} className="flex items-center justify-between rounded-2xl bg-cloud px-4 py-3">
                <span className="text-sm font-bold text-muted">{t(stat.key)}</span>
                <Badge tone={stat.tone}>{stat.value === null ? '…' : formatNumber(stat.value)}</Badge>
              </div>
            ))}
          </div>
        </Card>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <Card
          className="lg:col-span-2"
          title={t('pendingApplications')}
          action={
            <Link to="/drivers?status=submitted" className="text-sm font-bold text-brand">
              {t('viewAll')}
            </Link>
          }
        >
          {pending.loading ? (
            <PageSpinner />
          ) : pending.error ? (
            <ErrorState error={pending.error} onRetry={pending.reload} />
          ) : pending.data && pending.data.items.length > 0 ? (
            <ul className="space-y-3">
              {pending.data.items.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    onClick={() => navigate(`/drivers/${item.id}`)}
                    className="flex w-full items-center gap-4 rounded-2xl border border-line p-4 text-start transition hover:border-brand hover:bg-brand-soft/30"
                  >
                    <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-cloud text-ink">
                      <Icon name="user" />
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block truncate font-bold">{item.fullName || t('unnamed')}</span>
                      <span className="mt-0.5 block truncate text-xs text-muted">
                        <span className="ltr-nums">{item.applicationNumber}</span>
                        {item.cityName ? ` · ${item.cityName}` : ''} · {formatDate(item.submittedAt, lang)}
                      </span>
                    </span>
                    <DriverStatusBadge status={item.status} />
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <EmptyState icon="check" title={t('pendingApplicationsEmpty')} />
          )}
        </Card>

        <DarkCard>
          <div className="mb-6 flex items-center justify-between">
            <span className="grid size-12 place-items-center rounded-2xl bg-brand text-white">
              <Icon name="shield" />
            </span>
            <Badge tone="white">ATA</Badge>
          </div>
          <p className="text-xl font-bold">{t('quickLinks')}</p>
          <p className="mt-1 text-sm text-white/60">{t('quickLinksCopy')}</p>
          <div className="mt-6 space-y-2">
            {QUICK_LINKS.map((link) => (
              <Link
                key={link.key}
                to={link.to}
                className="flex items-center gap-3 rounded-2xl bg-white/5 px-4 py-3 text-sm font-bold transition hover:bg-white/10"
              >
                <Icon name={link.icon} className="size-5 text-brand" />
                <span className="flex-1">{t(link.key)}</span>
                <Icon name="chevron" className="size-4 text-white/50 rtl:rotate-180" />
              </Link>
            ))}
          </div>
          <Button variant="secondary" className="mt-6 w-full" onClick={() => navigate('/drivers?status=submitted')}>
            {t('quickReviewApplications')}
          </Button>
        </DarkCard>
      </div>
    </>
  )
}

function SafetyOverview({ summary, loading }: { summary: SafetySummary | null; loading: boolean }) {
  const { t } = useLang()
  const openTotal = summary ? summary.open.critical + summary.open.high + summary.open.medium + summary.open.low : null
  const critical = summary?.open.critical ?? 0
  const value = (count: number | null | undefined) => (loading && !summary ? '…' : formatNumber(count))

  return (
    <Card
      title={t('sfDashboardTitle')}
      action={
        <Link to="/safety" className="inline-flex items-center gap-1 text-sm font-bold text-brand">
          {t('sfOpenCenter')}
          <Icon name="chevron" className="size-4 rtl:rotate-180" />
        </Link>
      }
    >
      <Link
        to="/safety?status=open"
        className={`mb-3 flex items-center justify-between gap-3 rounded-2xl px-4 py-3 transition ${critical > 0 ? 'bg-danger text-white' : 'bg-cloud hover:bg-line'}`}
      >
        <span className="flex items-center gap-3">
          {critical > 0 && (
            <span className="relative flex size-3" aria-hidden="true">
              <span className="absolute inline-flex size-full rounded-full bg-white opacity-75 motion-safe:animate-ping" />
              <span className="relative inline-flex size-3 rounded-full bg-white" />
            </span>
          )}
          <span className="text-sm font-bold">{t('sfOpenCases')}</span>
        </span>
        <span className="ltr-nums text-2xl font-bold">{value(openTotal)}</span>
      </Link>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[
          { key: 'sfOpenCritical' as const, count: summary?.open.critical, tone: 'danger' as const },
          { key: 'sfOpenHigh' as const, count: summary?.open.high, tone: 'warning' as const },
          { key: 'sfUnassigned' as const, count: summary?.unassigned, tone: 'warning' as const },
          { key: 'sfPendingAlerts' as const, count: summary?.pendingAlerts, tone: 'ink' as const },
        ].map((stat) => (
          <div key={stat.key} className="rounded-2xl bg-cloud px-3 py-2.5">
            <p className="truncate text-xs font-bold text-muted">{t(stat.key)}</p>
            <p className={`ltr-nums mt-1 text-lg font-bold ${stat.tone === 'danger' && (stat.count ?? 0) > 0 ? 'text-danger' : ''}`}>{value(stat.count)}</p>
          </div>
        ))}
      </div>
      <p className="mt-3 text-xs text-muted">
        {t('sfAvgFirstResponse')}: <span className="ltr-nums font-bold text-ink">{summary ? formatDuration(summary.avgFirstResponseSeconds) : '…'}</span> · {t('sfOnDutyAgents')}:{' '}
        <span className="ltr-nums font-bold text-ink">{value(summary?.onDutyAgents)}</span>
      </p>
      <Link to="/safety/alerts?status=pending_rider" className="mt-3 inline-flex items-center gap-1 text-xs font-bold text-brand">
        <Icon name="bell" className="size-3.5" />
        {t('sfAlertsTitle')}
      </Link>
    </Card>
  )
}

function countOf(query: { data: { total: number } | null; loading: boolean; error: unknown }) {
  if (query.loading && !query.data) return '…'
  if (query.error || !query.data) return '—'
  return formatNumber(query.data.total)
}
