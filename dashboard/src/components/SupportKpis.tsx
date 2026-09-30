import { useState } from 'react'
import { useLang } from '../context/lang'
import { useSpanUnits } from '../hooks/useSpanUnits'
import { useQuery } from '../hooks/useQuery'
import { support } from '../lib/admin'
import { formatNumber } from '../lib/format'
import { daysAgoIso, todayIso } from '../lib/pricing'
import { formatRatio } from '../lib/rewards'
import { formatMinuteSpan, TICKET_TYPE_KEY } from '../lib/support'
import { Card } from './Card'
import { Select } from './Field'
import { PageSpinner } from './Spinner'

const PERIODS = [
  { days: 7, key: 'spKpiLast7' },
  { days: 30, key: 'spKpiLast30' },
  { days: 90, key: 'spKpiLast90' },
] as const

/**
 * Support KPIs of §F18.5 from `GET /admin/support/stats` (an endpoint beyond the §F18.3 table): tickets created / resolved, mean and median
 * resolution and first-response time, resolution-SLA compliance, CSAT and the split by type. Hidden when the call fails (403 / 404).
 */
export function SupportKpis() {
  const { t } = useLang()
  const units = useSpanUnits()
  const [days, setDays] = useState<number>(7)
  const to = todayIso()
  const from = daysAgoIso(days - 1)
  const query = useQuery(() => support.stats({ from, to }), `support-stats:${from}:${to}`)
  if (query.error) return null

  const s = query.data
  const metrics: { key: string; label: string; value: string }[] = [
    { key: 'created', label: t('spKpiCreated'), value: s ? formatNumber(s.created) : '…' },
    { key: 'resolved', label: t('spKpiResolved'), value: s ? formatNumber(s.resolved) : '…' },
    { key: 'avgRes', label: t('spKpiAvgResolution'), value: s ? formatMinuteSpan(s.avgResolutionMinutes, units) : '…' },
    { key: 'medRes', label: t('spKpiMedianResolution'), value: s ? formatMinuteSpan(s.medianResolutionMinutes, units) : '…' },
    { key: 'avgFr', label: t('spKpiAvgFirstResponse'), value: s ? formatMinuteSpan(s.avgFirstResponseMinutes, units) : '…' },
    { key: 'medFr', label: t('spKpiMedianFirstResponse'), value: s ? formatMinuteSpan(s.medianFirstResponseMinutes, units) : '…' },
    { key: 'sla', label: t('spKpiSlaCompliance'), value: s ? formatRatio(s.slaCompliance) : '…' },
    { key: 'csat', label: t('spKpiCsat'), value: s ? (s.csatAvg === null ? '—' : `${s.csatAvg.toFixed(2)} (${formatNumber(s.csatCount)} ${t('spKpiCsatCount')})`) : '…' },
  ]

  return (
    <Card
      className="mb-6"
      title={t('spKpiTitle')}
      description={t('spKpiCopy')}
      action={
        <Select id="kpi-period" aria-label={t('spKpiPeriod')} value={String(days)} onChange={(event) => setDays(Number(event.target.value))} wrapperClassName="w-44">
          {PERIODS.map((period) => (
            <option key={period.days} value={period.days}>
              {t(period.key)}
            </option>
          ))}
        </Select>
      }
    >
      {query.loading && !s ? (
        <PageSpinner />
      ) : (
        <>
          <dl className="grid grid-cols-2 gap-3 sm:grid-cols-4" data-testid="support-kpis">
            {metrics.map((metric) => (
              <div key={metric.key} className="min-w-0 rounded-2xl bg-cloud px-4 py-3">
                <dt className="truncate text-xs font-bold text-muted">{metric.label}</dt>
                <dd className="ltr-nums mt-1 truncate text-lg font-bold">{metric.value}</dd>
              </div>
            ))}
          </dl>
          {s && s.byType.length > 0 && (
            <div className="mt-4">
              <p className="mb-2 text-xs font-bold text-muted">{t('spKpiByType')}</p>
              <ul className="flex flex-wrap gap-2">
                {s.byType.map((row) => (
                  <li key={row.type} className="rounded-full bg-cloud px-3 py-1.5 text-xs font-bold">
                    {t(TICKET_TYPE_KEY[row.type] ?? 'spTypeOther')} <span className="ltr-nums text-muted">{formatNumber(row.created)} / {formatNumber(row.resolved)}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}
    </Card>
  )
}
