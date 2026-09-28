import type { PillTone } from '../components/business/ui'
import type { TranslationKey, TranslationParams } from '../i18n'
import type { CorporateEmployee, CorporateUserStatus, InvoiceStatus, PolicyViolation, TripStatus } from './types'

export const TRIP_TONES: Record<TripStatus, PillTone> = {
  requested: 'muted',
  searching: 'muted',
  scheduled: 'brand',
  driver_assigned: 'brand',
  driver_en_route: 'brand',
  driver_arrived: 'solid',
  waiting: 'solid',
  pin_verified: 'solid',
  in_trip: 'ink',
  completed: 'brand',
  cancelled: 'danger',
  no_drivers: 'danger',
}

export const TERMINAL_TRIP_STATUSES: TripStatus[] = ['completed', 'cancelled', 'no_drivers']

/** Statuses from which the booker may still cancel (before the trip starts). */
export const CANCELLABLE_TRIP_STATUSES: TripStatus[] = [
  'requested',
  'searching',
  'scheduled',
  'driver_assigned',
  'driver_en_route',
  'driver_arrived',
  'waiting',
]

export const INVOICE_TONES: Record<InvoiceStatus, PillTone> = {
  draft: 'muted',
  issued: 'brand',
  paid: 'solid',
  overdue: 'danger',
  void: 'muted',
}

export const EMPLOYEE_TONES: Record<CorporateUserStatus, PillTone> = {
  invited: 'warning',
  active: 'brand',
  disabled: 'muted',
}

const VIOLATION_KEYS: Record<string, TranslationKey> = {
  category: 'violation.category',
  day: 'violation.day',
  time_window: 'violation.time_window',
  zone: 'violation.zone',
  max_fare: 'violation.max_fare',
  scheduled: 'violation.scheduled',
  purpose_required: 'violation.purpose_required',
  cost_center_required: 'violation.cost_center_required',
}

/** Localized, human-readable policy violation (`{ rule, limit? }`). */
export function describeViolation(
  violation: PolicyViolation,
  t: (key: TranslationKey, params?: TranslationParams) => string,
  money: (amount: number) => string,
): string {
  const key = VIOLATION_KEYS[violation.rule]
  if (!key) return violation.rule
  return t(key, { limit: typeof violation.limit === 'number' ? money(violation.limit) : '' })
}

/** Read `details.violations` from a `corporate_policy_violation` error. */
export function violationsFrom(details: Record<string, unknown>): PolicyViolation[] {
  const raw = details.violations
  if (!Array.isArray(raw)) return []
  return raw.filter(
    (item): item is PolicyViolation =>
      typeof item === 'object' && item !== null && typeof (item as { rule?: unknown }).rule === 'string',
  )
}

/** Days of week as stored by the API (0 = Sunday … 6 = Saturday). */
export const WEEK_DAYS = [0, 1, 2, 3, 4, 5, 6] as const

/** CSV template for `POST /corporate/employees/import`. */
export const EMPLOYEE_CSV_TEMPLATE =
  'phone_number,full_name,employee_number,department,cost_center_code,monthly_budget,role\n' +
  '+966500000001,Sara Ahmed,E-1001,Finance,FIN-01,1500,employee\n'

export function costCenterLabel(value: CorporateEmployee['costCenter']): string {
  if (!value) return '—'
  return typeof value === 'string' ? value : value.code
}
