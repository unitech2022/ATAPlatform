import { Link } from 'react-router'
import { Can } from '../components/Can'
import { Card } from '../components/Card'
import { Toggle } from '../components/Field'
import { KpiCard } from '../components/KpiCard'
import { KpiSeriesChart } from '../components/KpiSeriesChart'
import { PageHeader } from '../components/PageHeader'
import { PermissionError } from '../components/PermissionError'
import { ReportFilters, type ReportFilterValue } from '../components/ReportFilters'
import { PageSpinner } from '../components/Spinner'
import { Table, type Column } from '../components/Table'
import { Tabs } from '../components/Tabs'
import { UsageBar } from '../components/UsageBar'
import { Icon } from '../components/Icon'
import { useLang } from '../context/lang'
import { useQuery } from '../hooks/useQuery'
import { parseIsoDate, useUrlState } from '../hooks/useUrlState'
import { reports } from '../lib/admin'
import { isApiError } from '../lib/api'
import { parseEnum } from '../lib/finance'
import { formatNumber } from '../lib/format'
import {
  defaultRange,
  displayValue,
  formatKpi,
  KPI_META,
  KPI_SECTIONS,
  metricsOfSection,
  rangeDays,
  rangeError,
  REPORT_MAX_RANGE_DAYS,
  unitOf,
  type UnitLabels,
} from '../lib/reports'
import type { BreakdownGroup, KpiBreakdownRow, KpiGranularity, KpiMetric } from '../lib/types'

const GRANULARITIES: KpiGranularity[] = ['day', 'week', 'month']
const GROUPS: BreakdownGroup[] = ['zone', 'category', 'city']

function autoGranularity(days: number): KpiGranularity {
  if (days <= 45) return 'day'
  if (days <= 200) return 'week'
  return 'month'
}

/**
 * KPI dashboard (`/reports`, docs/12 §F20.6/§F20.7/§F20.9, `reports.view`): one filter row (range, city, zone,
 * category, comparison) scoping KPI cards with deltas, the selected metric's time series, a breakdown by
 * zone/category/city, and the v1.1 metrics in their own section. All filters live in the URL.
 */
export function ReportsPage() {
  const { t } = useLang()
  const { params, update } = useUrlState()
  const fallback = defaultRange()
  const filters: ReportFilterValue = {
    from: parseIsoDate(params.get('from')) || fallback.from,
    to: parseIsoDate(params.get('to')) || fallback.to,
    cityId: params.get('cityId') ?? '',
    zoneId: params.get('zoneId') ?? '',
    rideCategoryId: params.get('rideCategoryId') ?? '',
  }
  const compare = params.get('compare') !== 'off'
  const metricCode = params.get('metric') || 'completed_trips'
  const days = rangeDays(filters.from, filters.to)
  const granularity = parseEnum(params.get('granularity'), GRANULARITIES) || autoGranularity(days)
  const groupBy = parseEnum(params.get('groupBy'), GROUPS) || 'zone'
  const invalid = rangeError(filters.from, filters.to)
  const scope = { cityId: filters.cityId, zoneId: filters.zoneId, rideCategoryId: filters.rideCategoryId }
  const scopeKey = `${filters.from}:${filters.to}:${filters.cityId}:${filters.zoneId}:${filters.rideCategoryId}`
  const labels: UnitLabels = { sar: t('sar'), minutes: t('rpMinutes'), hours: t('rpHours') }

  const setParam = (key: string, value: string | null) =>
    update((next) => {
      if (value) next.set(key, value)
      else next.delete(key)
    }, true)

  const kpis = useQuery(
    () => (invalid ? Promise.resolve(null) : reports.kpis({ from: filters.from, to: filters.to, ...scope, compare: compare ? 'previous_period' : '' })),
    `kpis:${scopeKey}:${compare}:${invalid ?? ''}`,
  )
  const series = useQuery(
    () => (invalid ? Promise.resolve(null) : reports.series(metricCode, { from: filters.from, to: filters.to, ...scope, granularity })),
    `kpi-series:${metricCode}:${scopeKey}:${granularity}:${invalid ?? ''}`,
  )
  const breakdown = useQuery(
    () => (invalid ? Promise.resolve(null) : reports.breakdown({ metric: metricCode, from: filters.from, to: filters.to, ...scope, groupBy })),
    `kpi-breakdown:${metricCode}:${scopeKey}:${groupBy}:${invalid ?? ''}`,
  )

  const rangeMessage = (error: unknown) => {
    if (isApiError(error) && error.code === 'report_range_too_large') {
      const max = typeof error.details?.maxDays === 'number' ? error.details.maxDays : REPORT_MAX_RANGE_DAYS
      return `${t('rpErrRangeTooLarge')} (${t('rpMaxDays')}: ${max})`
    }
    return null
  }

  const metrics = kpis.data?.metrics ?? []
  const selected: KpiMetric | undefined = metrics.find((metric) => metric.code === metricCode)
  const selectedUnit = selected ? unitOf(selected) : (KPI_META[metricCode]?.unit ?? 'count')
  const selectedName = KPI_META[metricCode] ? t(KPI_META[metricCode].key) : selected?.name || metricCode

  const renderCards = (list: KpiMetric[]) => (
    <div className="grid grid-cols-1 gap-3 min-[420px]:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4">
      {list.map((metric) => (
        <KpiCard key={metric.code} metric={metric} selected={metric.code === metricCode} showDelta={compare} onSelect={() => setParam('metric', metric.code)} />
      ))}
    </div>
  )

  const breakdownRows = breakdown.data?.rows ?? []
  const maxValue = Math.max(0, ...breakdownRows.map((row) => displayValue(selectedUnit, row.value, row.numerator, row.denominator) ?? 0))
  const breakdownColumns: Column<KpiBreakdownRow>[] = [
    { key: 'label', header: t(`rpGroup_${groupBy}` as const), render: (row) => <span className="block max-w-56 truncate font-bold">{row.label || row.key}</span> },
    {
      key: 'value',
      header: selectedName,
      className: 'text-end',
      render: (row) => <span className="ltr-nums whitespace-nowrap font-bold">{formatKpi(selectedUnit, row.value, labels, row.numerator, row.denominator)}</span>,
    },
    {
      key: 'share',
      header: <span className="sr-only">{t('rpShare')}</span>,
      className: 'w-40 min-w-28',
      render: (row) => {
        const shown = displayValue(selectedUnit, row.value, row.numerator, row.denominator)
        return <UsageBar share={shown !== null && maxValue > 0 ? Math.max(0, shown) / maxValue : null} />
      },
    },
    {
      key: 'ratio',
      header: t('rpNumDen'),
      className: 'hidden md:table-cell text-end',
      render: (row) =>
        typeof row.numerator === 'number' && typeof row.denominator === 'number' ? (
          <span className="ltr-nums text-xs text-muted">
            {formatNumber(row.numerator)} / {formatNumber(row.denominator)}
          </span>
        ) : (
          <span className="text-muted">—</span>
        ),
    },
  ]

  const v11 = metricsOfSection(metrics, 'v11')

  return (
    <>
      <PageHeader
        title={t('rpTitle')}
        description={t('rpCopy')}
        actions={
          <Can permission="reports.export">
            <Link to="/reports/exports" className="inline-flex h-11 items-center gap-2 rounded-2xl border border-line bg-white px-4 text-sm font-bold text-ink transition hover:bg-cloud">
              <Icon name="download" className="size-4" />
              {t('navReportExports')}
            </Link>
          </Can>
        }
      />

      <ReportFilters
        value={filters}
        onChange={(patch) =>
          update((next) => {
            for (const [key, value] of Object.entries(patch)) {
              if (value) next.set(key, value)
              else next.delete(key)
            }
          }, true)
        }
        error={invalid ? t(invalid) : null}
        extra={
          <div className="max-w-md">
            <Toggle id="rp-compare" checked={compare} onChange={(on) => setParam('compare', on ? null : 'off')} label={t('rpCompare')} description={t('rpCompareCopy')} />
          </div>
        }
      />

      {!invalid && kpis.error ? (
        <Card>
          {rangeMessage(kpis.error) ? (
            <p role="alert" className="py-6 text-center font-bold text-danger">
              {rangeMessage(kpis.error)}
            </p>
          ) : (
            <PermissionError error={kpis.error} permission="reports.view" onRetry={kpis.reload} />
          )}
        </Card>
      ) : !invalid && kpis.loading && !kpis.data ? (
        <PageSpinner />
      ) : !invalid && kpis.data ? (
        <div className={kpis.loading ? 'opacity-60 transition-opacity' : 'transition-opacity'} aria-busy={kpis.loading}>
          {KPI_SECTIONS.map(({ section, key }) => {
            const list = metricsOfSection(metrics, section)
            if (list.length === 0) return null
            return (
              <section key={section} className="mb-6" aria-labelledby={`rp-section-${section}`}>
                <h2 id={`rp-section-${section}`} className="mb-3 text-lg font-bold">
                  {t(key)}
                </h2>
                {renderCards(list)}
              </section>
            )
          })}

          <Card
            className="mb-6"
            title={selectedName}
            description={t('rpSeriesCopy')}
            action={
              <Tabs
                value={granularity}
                onChange={(value) => setParam('granularity', value)}
                options={GRANULARITIES.map((value) => ({ value, label: t(`rpGranularity_${value}` as const) }))}
              />
            }
          >
            {series.error ? (
              rangeMessage(series.error) ?? <PermissionError error={series.error} permission="reports.view" onRetry={series.reload} />
            ) : series.loading && !series.data ? (
              <PageSpinner />
            ) : series.data && series.data.points.length > 0 ? (
              <div className={series.loading ? 'opacity-60' : ''}>
                <KpiSeriesChart points={series.data.points} unit={series.data.unit ?? selectedUnit} title={selectedName} />
              </div>
            ) : (
              <p className="py-10 text-center text-sm text-muted">{t('rpNoData')}</p>
            )}
          </Card>

          <Card
            flush
            className="mb-8"
            title={t('rpBreakdownTitle')}
            description={`${selectedName} · ${t('rpBreakdownCopy')}`}
            action={<Tabs value={groupBy} onChange={(value) => setParam('groupBy', value)} options={GROUPS.map((value) => ({ value, label: t(`rpGroup_${value}` as const) }))} />}
          >
            {breakdown.error ? (
              <PermissionError error={breakdown.error} permission="reports.view" onRetry={breakdown.reload} />
            ) : (
              <Table
                columns={breakdownColumns}
                rows={breakdownRows}
                rowKey={(row) => row.key}
                loading={breakdown.loading}
                emptyTitle={t('rpNoData')}
                emptyDescription=""
              />
            )}
          </Card>

          {v11.length > 0 && (
            <section className="rounded-3xl border border-dashed border-line p-4 sm:p-5" aria-labelledby="rp-section-v11" data-testid="kpi-v11">
              <div className="mb-3">
                <h2 id="rp-section-v11" className="text-lg font-bold">
                  {t('rpSectionV11')}
                </h2>
                <p className="text-sm text-muted">{t('rpSectionV11Copy')}</p>
              </div>
              {renderCards(v11)}
            </section>
          )}
        </div>
      ) : null}
    </>
  )
}
