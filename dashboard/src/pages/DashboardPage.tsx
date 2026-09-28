import { Link, useNavigate } from 'react-router'
import { Badge, DriverStatusBadge } from '../components/Badge'
import { Button } from '../components/Button'
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
import { dashboard, drivers, live } from '../lib/admin'
import { formatDate, formatNumber } from '../lib/format'
import type { DashboardSummary } from '../lib/types'

const STATS: { key: keyof DashboardSummary; label: TranslationKey; icon: IconName; tone?: 'brand' | 'danger' }[] = [
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
