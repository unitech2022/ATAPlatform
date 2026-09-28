import { useLang } from '../context/lang'
import type { TranslationKey } from '../i18n'
import { feeRevenueOf, formatRate } from '../lib/cancellation'
import { formatMoney, formatNumber } from '../lib/format'
import type { CancellationStats } from '../lib/types'
import { Icon, type IconName } from './Icon'

type Kpi = { key: TranslationKey; value: string; icon: IconName; tone: 'brand' | 'danger' | 'ink' }

/** KPI tiles from `GET /admin/cancellations/stats`; `compact` shows the three headline numbers only. */
export function CancellationKpis({ stats, loading, compact = false }: { stats: CancellationStats | null; loading: boolean; compact?: boolean }) {
  const { t } = useLang()
  const pending = loading && !stats
  const rate = (value: number | null | undefined) => (pending ? '…' : formatRate(value))
  const revenue = feeRevenueOf(stats)

  const headline: Kpi[] = [
    { key: 'cxKpiPassengerRate', value: rate(stats?.passengerCancellationRate), icon: 'user', tone: 'danger' },
    { key: 'cxKpiDriverRate', value: rate(stats?.driverCancellationRate), icon: 'car', tone: 'danger' },
    { key: 'cxKpiFeeRevenue', value: pending ? '…' : revenue === null ? '—' : `${formatMoney(revenue)} ${t('sar')}`, icon: 'wallet', tone: 'brand' },
  ]
  const extra: Kpi[] = [
    { key: 'cxKpiRepeatRate', value: rate(stats?.repeatCancellationRate), icon: 'refresh', tone: 'ink' },
    { key: 'cxKpiNoShowRate', value: rate(stats?.noShowRate), icon: 'clock', tone: 'ink' },
    { key: 'cxKpiExcuseApproval', value: rate(stats?.excuseApprovalRate), icon: 'check', tone: 'ink' },
    { key: 'cxKpiDriverReliability', value: rate(stats?.driverReliabilityRate), icon: 'gauge', tone: 'brand' },
    { key: 'cxKpiPassengerReliability', value: rate(stats?.passengerReliabilityRate), icon: 'gauge', tone: 'brand' },
  ]
  if (typeof stats?.totalCancellations === 'number') extra.unshift({ key: 'cxKpiTotal', value: formatNumber(stats.totalCancellations), icon: 'x', tone: 'ink' })
  const tiles = compact ? headline : [...headline, ...extra]

  return (
    <div className={`grid gap-3 ${compact ? 'sm:grid-cols-3' : 'grid-cols-2 sm:grid-cols-3 lg:grid-cols-4'}`}>
      {tiles.map((kpi) => (
        <div key={kpi.key} className={`rounded-2xl ${compact ? 'bg-cloud' : 'bg-white shadow-soft'} px-4 py-3`}>
          <div className="flex items-center gap-2">
            <span className={`grid size-7 shrink-0 place-items-center rounded-lg ${kpi.tone === 'danger' ? 'bg-danger-soft text-danger' : kpi.tone === 'brand' ? 'bg-brand-soft text-brand' : 'bg-white text-ink'}`}>
              <Icon name={kpi.icon} className="size-3.5" />
            </span>
            <span className="truncate text-xs font-bold text-muted">{t(kpi.key)}</span>
          </div>
          <p className="ltr-nums mt-2 text-xl font-bold">{kpi.value}</p>
        </div>
      ))}
    </div>
  )
}
