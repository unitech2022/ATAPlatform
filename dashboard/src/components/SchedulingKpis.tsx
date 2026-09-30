import { useLang } from '../context/lang'
import type { TranslationKey } from '../i18n'
import { driverNoShowRate, formatShare } from '../lib/scheduling'
import { formatNumber } from '../lib/format'
import type { SchedulingStats } from '../lib/types'
import { Icon, type IconName } from './Icon'
import { StatCard } from './StatCard'

/** Scheduled-ride KPI cards from `GET /admin/scheduling/stats` (§F17.5): completion, cancellation and driver no-show rates plus counters. */
export function SchedulingKpis({ stats, loading }: { stats: SchedulingStats | null; loading: boolean }) {
  const { t } = useLang()
  const pending = loading && !stats
  const value = (text: string) => (pending ? '…' : text)
  const counters: { key: TranslationKey; value: number | null | undefined; icon: IconName }[] = [
    { key: 'sdKpiBooked', value: stats?.booked, icon: 'calendar' },
    { key: 'sdKpiCompleted', value: stats?.completed, icon: 'check' },
    { key: 'sdKpiCancelledPassenger', value: stats?.cancelledByPassenger, icon: 'x' },
    { key: 'sdKpiCancelledLate', value: stats?.cancelledLate, icon: 'clock' },
    { key: 'sdKpiReleases', value: stats?.driverReleases, icon: 'refresh' },
    { key: 'sdKpiConfirmMissed', value: stats?.confirmationMissed, icon: 'alert' },
    { key: 'sdKpiRematched', value: stats?.rematched, icon: 'route' },
  ]
  const noShows = stats?.driverNoShows
  return (
    <div className="space-y-3">
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title={t('sdKpiCompletionRate')} icon="check" value={value(formatShare(stats?.scheduledCompletionRate))} meta={t('sdKpiCompletionMeta')} />
        <StatCard title={t('sdKpiCancellationRate')} icon="x" tone="danger" value={value(formatShare(stats?.scheduledCancellationRate))} meta={t('sdKpiCancellationMeta')} />
        <StatCard
          title={t('sdKpiNoShowRate')}
          icon="car"
          tone="danger"
          value={value(formatShare(driverNoShowRate(stats)))}
          meta={pending ? undefined : `${formatNumber(noShows)} ${t('sdKpiNoShowUnit')}`}
        />
      </div>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {counters.map((counter) => (
          <div key={counter.key} className="rounded-2xl bg-white px-4 py-3 shadow-soft">
            <div className="flex items-center gap-2">
              <Icon name={counter.icon} className="size-3.5 shrink-0 text-muted" />
              <span className="text-xs font-bold leading-tight text-muted">{t(counter.key)}</span>
            </div>
            <p className="ltr-nums mt-1.5 text-xl font-bold">{value(formatNumber(counter.value))}</p>
          </div>
        ))}
        <div className="rounded-2xl bg-white px-4 py-3 shadow-soft">
          <div className="flex items-center gap-2">
            <Icon name="clock" className="size-3.5 shrink-0 text-muted" />
            <span className="text-xs font-bold leading-tight text-muted">{t('sdKpiAvgLead')}</span>
          </div>
          <p className="ltr-nums mt-1.5 text-xl font-bold">
            {value(typeof stats?.avgReservationLeadHours === 'number' ? `${stats.avgReservationLeadHours.toFixed(1)} h` : '—')}
          </p>
        </div>
      </div>
    </div>
  )
}
