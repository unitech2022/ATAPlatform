import { useLang } from '../context/lang'
import { changeOf, deltaTone, formatChange, formatKpi, KPI_META, unitOf, type UnitLabels } from '../lib/reports'
import type { KpiMetric } from '../lib/types'
import { Icon } from './Icon'

/**
 * KPI tile (§F20.6/§F20.9): value per unit, change vs the comparison period coloured by whether "up" is good for
 * that metric (always with an arrow and a sign, never colour alone). Clicking selects the metric for the chart.
 */
export function KpiCard({ metric, selected, showDelta, onSelect }: { metric: KpiMetric; selected: boolean; showDelta: boolean; onSelect: () => void }) {
  const { t } = useLang()
  const labels: UnitLabels = { sar: t('sar'), minutes: t('rpMinutes'), hours: t('rpHours') }
  const meta = KPI_META[metric.code]
  const unit = unitOf(metric)
  const name = meta ? t(meta.key) : metric.name || metric.code
  const change = showDelta ? changeOf(metric) : null
  const tone = deltaTone(metric.code, change)
  const toneClass = tone === 'good' ? 'bg-brand-soft text-brand' : tone === 'bad' ? 'bg-danger-soft text-danger' : 'bg-cloud text-muted'
  const value = formatKpi(unit, metric.value, labels, metric.numerator, metric.denominator)

  return (
    <button
      type="button"
      onClick={onSelect}
      aria-pressed={selected}
      data-kpi={metric.code}
      className={`flex min-w-0 flex-col rounded-3xl bg-white p-4 text-start shadow-soft transition hover:shadow-brand sm:p-5 ${selected ? 'ring-2 ring-brand' : ''}`}
    >
      <span className="line-clamp-2 min-h-10 text-sm font-bold text-muted">{name}</span>
      <span className="ltr-nums mt-2 truncate text-2xl font-bold leading-tight">{value}</span>
      {showDelta && (
        <span className="mt-2 flex flex-wrap items-center gap-2 text-xs">
          {change === null ? (
            <span className="text-muted">{t('rpNoComparison')}</span>
          ) : (
            <span className={`ltr-nums inline-flex items-center gap-1 rounded-full px-2 py-0.5 font-bold ${toneClass}`}>
              <Icon name="arrow" className={`size-3 ${change > 0 ? 'rotate-90' : change < 0 ? '-rotate-90' : 'rotate-180'}`} />
              {formatChange(change)}
              <span className="sr-only">{tone === 'good' ? t('rpBetter') : tone === 'bad' ? t('rpWorse') : ''}</span>
            </span>
          )}
          {typeof metric.previousValue === 'number' && (
            <span className="truncate text-muted">
              {t('rpVs')} <span className="ltr-nums">{formatKpi(unit, metric.previousValue, labels)}</span>
            </span>
          )}
        </span>
      )}
    </button>
  )
}
