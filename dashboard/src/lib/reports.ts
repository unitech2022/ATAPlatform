import type { TranslationKey } from '../i18n'
import { formatMoney, formatNumber } from './format'
import type { KpiMetric, KpiUnit, ReportDataset } from './types'

/** Server defaults (docs/12 §F20.8); used for client-side hints only — the server answers `422 report_range_too_large`. */
export const REPORT_MAX_RANGE_DAYS = 366
export const REPORT_MAX_EXPORT_ROWS = 100000

export type KpiSection = 'trips' | 'revenue' | 'people' | 'quality' | 'v11'

export interface KpiMeta {
  key: TranslationKey
  unit: KpiUnit
  section: KpiSection
  /** Rising values are bad (delta shown in danger). `undefined` = neutral (no tone). */
  better?: 'up' | 'down'
}

/** KPI catalogue (§F20.6), in display order. v1.1 metrics live in their own section (§F20.9). */
export const KPI_META: Record<string, KpiMeta> = {
  completed_trips: { key: 'kpiCompletedTrips', unit: 'count', section: 'trips', better: 'up' },
  requested_trips: { key: 'kpiRequestedTrips', unit: 'count', section: 'trips', better: 'up' },
  completion_rate: { key: 'kpiCompletionRate', unit: 'percent', section: 'trips', better: 'up' },
  no_drivers_rate: { key: 'kpiNoDriversRate', unit: 'percent', section: 'trips', better: 'down' },
  average_eta: { key: 'kpiAverageEta', unit: 'seconds', section: 'trips', better: 'down' },
  average_time_to_assign: { key: 'kpiTimeToAssign', unit: 'seconds', section: 'trips', better: 'down' },
  driver_acceptance_rate: { key: 'kpiDriverAcceptance', unit: 'percent', section: 'trips', better: 'up' },
  driver_cancellation_rate: { key: 'kpiDriverCancellation', unit: 'percent', section: 'trips', better: 'down' },
  passenger_cancellation_rate: { key: 'kpiPassengerCancellation', unit: 'percent', section: 'trips', better: 'down' },
  gmv: { key: 'kpiGmv', unit: 'sar', section: 'revenue', better: 'up' },
  platform_revenue: { key: 'kpiPlatformRevenue', unit: 'sar', section: 'revenue', better: 'up' },
  take_rate: { key: 'kpiTakeRate', unit: 'percent', section: 'revenue', better: 'up' },
  average_fare: { key: 'kpiAverageFare', unit: 'sar', section: 'revenue' },
  driver_earnings_per_online_hour: { key: 'kpiEarningsPerHour', unit: 'sar', section: 'revenue', better: 'up' },
  incentives_paid: { key: 'kpiIncentivesPaid', unit: 'sar', section: 'revenue' },
  refunds_amount: { key: 'kpiRefunds', unit: 'sar', section: 'revenue', better: 'down' },
  active_riders: { key: 'kpiActiveRiders', unit: 'count', section: 'people', better: 'up' },
  active_drivers: { key: 'kpiActiveDrivers', unit: 'count', section: 'people', better: 'up' },
  new_riders: { key: 'kpiNewRiders', unit: 'count', section: 'people', better: 'up' },
  repeat_rate: { key: 'kpiRepeatRate', unit: 'percent', section: 'people', better: 'up' },
  trips_per_active_rider: { key: 'kpiTripsPerRider', unit: 'ratio', section: 'people', better: 'up' },
  online_hours: { key: 'kpiOnlineHours', unit: 'hours', section: 'people', better: 'up' },
  customer_rating: { key: 'kpiCustomerRating', unit: 'rating', section: 'quality', better: 'up' },
  support_resolution_time: { key: 'kpiSupportResolution', unit: 'hours', section: 'quality', better: 'down' },
  cancellation_fee_revenue: { key: 'kpiCancellationFeeRevenue', unit: 'sar', section: 'v11' },
  repeat_cancellation_rate: { key: 'kpiRepeatCancellation', unit: 'percent', section: 'v11', better: 'down' },
  driver_reliability_rate: { key: 'kpiDriverReliability', unit: 'percent', section: 'v11', better: 'up' },
  passenger_reliability_rate: { key: 'kpiPassengerReliability', unit: 'percent', section: 'v11', better: 'up' },
  favorite_driver_booking_rate: { key: 'kpiFavoriteBookingRate', unit: 'percent', section: 'v11', better: 'up' },
  favorite_driver_discount_usage: { key: 'kpiFavoriteDiscountUsage', unit: 'sar', section: 'v11' },
  scheduled_ride_completion_rate: { key: 'kpiScheduledCompletion', unit: 'percent', section: 'v11', better: 'up' },
  scheduled_ride_cancellation_rate: { key: 'kpiScheduledCancellation', unit: 'percent', section: 'v11', better: 'down' },
}

export const KPI_SECTIONS: { section: KpiSection; key: TranslationKey }[] = [
  { section: 'trips', key: 'rpSectionTrips' },
  { section: 'revenue', key: 'rpSectionRevenue' },
  { section: 'people', key: 'rpSectionPeople' },
  { section: 'quality', key: 'rpSectionQuality' },
]

export const KPI_CODES = Object.keys(KPI_META)

export function kpiSectionOf(code: string): KpiSection {
  return KPI_META[code]?.section ?? 'quality'
}

/** Metrics of one section in catalogue order; unknown server codes are appended to "quality". */
export function metricsOfSection(metrics: readonly KpiMetric[], section: KpiSection): KpiMetric[] {
  const order = (code: string) => {
    const index = KPI_CODES.indexOf(code)
    return index === -1 ? Number.MAX_SAFE_INTEGER : index
  }
  return metrics.filter((metric) => kpiSectionOf(metric.code) === section).sort((a, b) => order(a.code) - order(b.code))
}

export function unitOf(metric: { code: string; unit?: KpiUnit | null }): KpiUnit {
  return metric.unit ?? KPI_META[metric.code]?.unit ?? 'count'
}

/**
 * Share (0..1) of a percent metric. Prefers numerator ÷ denominator (the `ratio` aggregation of §F20.6); otherwise
 * reads `value` as 0..1, or as a percentage when above 1 (the doc does not fix the unit).
 */
export function percentShare(value: number | null | undefined, numerator?: number | null, denominator?: number | null): number | null {
  if (typeof numerator === 'number' && typeof denominator === 'number' && denominator > 0) return numerator / denominator
  if (typeof value !== 'number' || !Number.isFinite(value)) return null
  return Math.abs(value) > 1 ? value / 100 : value
}

const oneDecimal = new Intl.NumberFormat('en-US', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
const twoDecimals = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** Numeric value on the display scale of the unit: percent → 0..100, seconds → minutes. Used by charts and bars. */
export function displayValue(unit: KpiUnit, value: number | null | undefined, numerator?: number | null, denominator?: number | null): number | null {
  if (unit === 'percent') {
    const share = percentShare(value, numerator, denominator)
    return share === null ? null : share * 100
  }
  if (typeof value !== 'number' || !Number.isFinite(value)) return null
  if (unit === 'seconds') return value / 60
  return value
}

export type UnitLabels = { sar: string; minutes: string; hours: string }

/** KPI value per unit (SAR, percent, minutes, hours, count, rating, ratio); numbers are always LTR in the caller. */
export function formatKpi(unit: KpiUnit, value: number | null | undefined, labels: UnitLabels, numerator?: number | null, denominator?: number | null): string {
  const shown = displayValue(unit, value, numerator, denominator)
  if (shown === null) return '—'
  switch (unit) {
    case 'percent':
      return `${oneDecimal.format(shown)}%`
    case 'sar':
      return `${formatMoney(shown)} ${labels.sar}`
    case 'seconds':
      return `${oneDecimal.format(shown)} ${labels.minutes}`
    case 'hours':
      return `${oneDecimal.format(shown)} ${labels.hours}`
    case 'rating':
    case 'ratio':
      return twoDecimals.format(shown)
    default:
      return formatNumber(Math.round(shown))
  }
}

/** Short axis tick (no unit word; the chart caption names the unit). */
export function formatAxisTick(unit: KpiUnit, value: number) {
  if (unit === 'percent') return `${Math.round(value)}%`
  if (unit === 'rating' || unit === 'ratio') return twoDecimals.format(value)
  if (Math.abs(value) >= 1_000_000) return `${oneDecimal.format(value / 1_000_000)}M`
  if (Math.abs(value) >= 10_000) return `${oneDecimal.format(value / 1000)}K`
  return formatNumber(Math.round(value * 10) / 10)
}

/** `changePercent` from the server, or derived from value vs previousValue. */
export function changeOf(metric: KpiMetric): number | null {
  if (typeof metric.changePercent === 'number' && Number.isFinite(metric.changePercent)) return metric.changePercent
  if (typeof metric.value === 'number' && typeof metric.previousValue === 'number' && metric.previousValue !== 0) {
    return ((metric.value - metric.previousValue) / Math.abs(metric.previousValue)) * 100
  }
  return null
}

export type DeltaTone = 'good' | 'bad' | 'neutral'

export function deltaTone(code: string, change: number | null): DeltaTone {
  if (change === null || change === 0) return 'neutral'
  const better = KPI_META[code]?.better
  if (!better) return 'neutral'
  return (change > 0) === (better === 'up') ? 'good' : 'bad'
}

export function formatChange(change: number) {
  return `${change > 0 ? '+' : ''}${oneDecimal.format(change)}%`
}

// ---------------------------------------------------------------------------
// Date ranges (Riyadh days; `YYYY-MM-DD`)
// ---------------------------------------------------------------------------

export function isoDay(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function addDays(iso: string, days: number) {
  const [year, month, day] = iso.split('-').map(Number)
  const date = new Date(year, month - 1, day)
  date.setDate(date.getDate() + days)
  return isoDay(date)
}

/** Inclusive number of days in `[from, to]`; 0 when invalid or reversed. */
export function rangeDays(from: string, to: string) {
  const start = Date.parse(`${from}T00:00:00Z`)
  const end = Date.parse(`${to}T00:00:00Z`)
  if (!Number.isFinite(start) || !Number.isFinite(end) || end < start) return 0
  return Math.round((end - start) / 86_400_000) + 1
}

export type RangePreset = '7' | '28' | '90' | 'mtd'

export const RANGE_PRESETS: { value: RangePreset; key: TranslationKey }[] = [
  { value: '7', key: 'rpPreset7' },
  { value: '28', key: 'rpPreset28' },
  { value: '90', key: 'rpPreset90' },
  { value: 'mtd', key: 'rpPresetMtd' },
]

export function presetRange(preset: RangePreset, today = isoDay(new Date())) {
  if (preset === 'mtd') return { from: `${today.slice(0, 8)}01`, to: today }
  return { from: addDays(today, -(Number(preset) - 1)), to: today }
}

export function defaultRange() {
  return presetRange('28')
}

/** Validation shared by the KPI page and the exports page. */
export function rangeError(from: string, to: string): TranslationKey | null {
  if (!from || !to) return 'rpErrRangeRequired'
  const days = rangeDays(from, to)
  if (days === 0) return 'rpErrRangeOrder'
  if (days > REPORT_MAX_RANGE_DAYS) return 'rpErrRangeTooLarge'
  return null
}

// ---------------------------------------------------------------------------
// Export datasets (§F20.7)
// ---------------------------------------------------------------------------

export const REPORT_DATASETS: { value: ReportDataset; key: TranslationKey; columns: string[] }[] = [
  { value: 'kpis', key: 'rxDatasetKpis', columns: ['date', 'scope', 'metric_code', 'value', 'numerator', 'denominator'] },
  {
    value: 'trips',
    key: 'rxDatasetTrips',
    columns: [
      'trip_number', 'requested_at', 'completed_at', 'status', 'category', 'city', 'zone', 'passenger_id', 'driver_id', 'booking_type', 'payment_method',
      'distance_km', 'duration_min', 'estimated_fare', 'final_fare', 'discount_total', 'driver_earnings', 'cancelled_by', 'cancellation_reason', 'corporate_account',
    ],
  },
  {
    value: 'payments',
    key: 'rxDatasetPayments',
    columns: ['payment_id', 'created_at', 'purpose', 'method', 'provider', 'status', 'amount', 'captured_amount', 'refunded_amount', 'trip_number', 'gateway_payment_id'],
  },
  { value: 'payouts', key: 'rxDatasetPayouts', columns: ['payout_number', 'requested_at', 'driver', 'amount', 'status', 'approved_at', 'paid_at', 'bank_reference', 'batch_number'] },
  {
    value: 'cancellations',
    key: 'rxDatasetCancellations',
    columns: ['trip_number', 'created_at', 'actor', 'at_fault', 'stage', 'reason_code', 'fee_amount', 'fee_charged', 'fee_status', 'compensation', 'penalty_points', 'excuse_status'],
  },
  { value: 'ratings', key: 'rxDatasetRatings', columns: ['trip_number', 'created_at', 'rater_role', 'stars', 'tags', 'has_comment'] },
  {
    value: 'support_tickets',
    key: 'rxDatasetSupport',
    columns: ['ticket_number', 'created_at', 'type', 'priority', 'status', 'first_response_minutes', 'resolution_hours', 'sla_met', 'csat'],
  },
  {
    value: 'drivers',
    key: 'rxDatasetDrivers',
    columns: ['driver_id', 'application_number', 'status', 'tier', 'rating_avg', 'city', 'approved_at', 'completed_trips', 'online_hours', 'reliability_level'],
  },
  { value: 'incentives', key: 'rxDatasetIncentives', columns: ['incentive', 'driver', 'period_start', 'completed_trips', 'status', 'reward_amount', 'paid_at'] },
]

export const REPORT_DATASET_VALUES = REPORT_DATASETS.map((dataset) => dataset.value)
