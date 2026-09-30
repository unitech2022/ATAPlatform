import { useState } from 'react'
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis, type TooltipContentProps, type TooltipValueType } from 'recharts'
import { useLang } from '../context/lang'
import { formatDate } from '../lib/format'
import { displayValue, formatAxisTick, formatKpi, type UnitLabels } from '../lib/reports'
import type { KpiSeriesPoint, KpiUnit } from '../lib/types'
import { Button } from './Button'

interface ChartRow {
  periodStart: string
  value: number | null
  label: string
  /** Compact numeric axis tick (`dd/MM`, or `MM/yyyy` for monthly points) — bidi-safe inside the LTR SVG. */
  tick: string
  formatted: string
}

function tickOf(periodStart: string, monthly: boolean) {
  const [year = '', month = '', day = ''] = periodStart.slice(0, 10).split('-')
  return monthly ? `${month}/${year}` : `${day}/${month}`
}

/**
 * Single-series time chart for the selected KPI (Recharts, §F20.9). One series → no legend (the card title names
 * it); brand line (2px) over a soft area, recessive grid, crosshair tooltip, and a table view for screen readers
 * and for the value-level detail. In RTL the time axis runs right-to-left with the value axis on the right.
 */
export function KpiSeriesChart({ points, unit, title }: { points: KpiSeriesPoint[]; unit: KpiUnit; title: string }) {
  const { t, lang, dir } = useLang()
  const [asTable, setAsTable] = useState(false)
  const labels: UnitLabels = { sar: t('sar'), minutes: t('rpMinutes'), hours: t('rpHours') }
  const rtl = dir === 'rtl'
  const monthly = points.length > 1 && points.every((point) => point.periodStart.slice(8, 10) === '01')
  const rows: ChartRow[] = points.map((point) => ({
    periodStart: point.periodStart,
    tick: tickOf(point.periodStart, monthly),
    value: displayValue(unit, point.value, point.numerator, point.denominator),
    label: formatDate(point.periodStart, lang),
    formatted: formatKpi(unit, point.value, labels, point.numerator, point.denominator),
  }))

  const renderTooltip = ({ active, payload }: TooltipContentProps<TooltipValueType, string | number>) => {
    const row = active ? (payload?.[0]?.payload as ChartRow | undefined) : undefined
    if (!row) return null
    return (
      <div dir={dir} className="rounded-xl border border-line bg-white px-3 py-2 text-sm shadow-float">
        <p className="ltr-nums font-bold">{row.formatted}</p>
        <p className="text-xs text-muted">{row.label}</p>
      </div>
    )
  }

  const summary = `${title}: ${rows.length} ${t('rpPoints')}`

  return (
    <div>
      <div className="mb-3 flex justify-end">
        <Button variant="ghost" size="sm" icon={asTable ? 'chart' : 'list'} onClick={() => setAsTable((current) => !current)} aria-pressed={asTable}>
          {asTable ? t('rpShowChart') : t('rpShowTable')}
        </Button>
      </div>
      {asTable ? (
        <div className="max-h-80 overflow-auto rounded-2xl border border-line">
          <table className="w-full text-sm">
            <caption className="sr-only">{title}</caption>
            <thead>
              <tr className="border-b border-line bg-cloud/60 text-xs text-muted">
                <th scope="col" className="px-4 py-2 text-start">
                  {t('rpPeriod')}
                </th>
                <th scope="col" className="px-4 py-2 text-end">
                  {t('rpValue')}
                </th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.periodStart} className="border-b border-line last:border-0">
                  <td className="px-4 py-2">{row.label}</td>
                  <td className="ltr-nums px-4 py-2 text-end font-bold">{row.formatted}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <figure role="img" aria-label={summary} className="h-72 w-full" dir="ltr" data-testid="kpi-chart">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={rows} margin={{ top: 8, right: 8, bottom: 0, left: 8 }}>
              <defs>
                <linearGradient id="kpi-fill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="var(--color-brand)" stopOpacity={0.22} />
                  <stop offset="100%" stopColor="var(--color-brand)" stopOpacity={0.02} />
                </linearGradient>
              </defs>
              <CartesianGrid stroke="var(--color-line)" strokeDasharray="0" vertical={false} />
              <XAxis
                dataKey="tick"
                reversed={rtl}
                tick={{ fill: 'var(--color-muted)', fontSize: 11 }}
                tickLine={false}
                axisLine={{ stroke: 'var(--color-line)' }}
                minTickGap={24}
              />
              <YAxis
                orientation={rtl ? 'right' : 'left'}
                width={52}
                tick={{ fill: 'var(--color-muted)', fontSize: 11 }}
                tickLine={false}
                axisLine={false}
                tickFormatter={(value: number) => formatAxisTick(unit, value)}
                domain={unit === 'percent' ? [0, 'auto'] : ['auto', 'auto']}
              />
              <Tooltip content={renderTooltip} cursor={{ stroke: 'var(--color-ink)', strokeOpacity: 0.3, strokeWidth: 1 }} isAnimationActive={false} />
              <Area
                type="monotone"
                dataKey="value"
                stroke="var(--color-brand)"
                strokeWidth={2}
                fill="url(#kpi-fill)"
                connectNulls
                dot={rows.length <= 16 ? { r: 3, fill: 'var(--color-brand)', stroke: '#fff', strokeWidth: 2 } : false}
                activeDot={{ r: 5, fill: 'var(--color-brand)', stroke: '#fff', strokeWidth: 2 }}
                isAnimationActive={false}
              />
            </AreaChart>
          </ResponsiveContainer>
        </figure>
      )}
    </div>
  )
}
