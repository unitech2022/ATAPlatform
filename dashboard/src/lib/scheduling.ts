import type { TranslationKey } from '../i18n'
import type {
  ReminderKind,
  ReservationReleaseReason,
  ReservationSource,
  ScheduledFeeType,
  ScheduledReservationFilter,
  ScheduledRule,
  ScheduledRuleInput,
  ScheduledTripRow,
  TripDetail,
} from './types'

// ---------------------------------------------------------------------------
// F17 — scheduled ride rules, reservations and KPIs (docs/11 §F17.1–§F17.5)
// ---------------------------------------------------------------------------

/** Column defaults of `scheduled_ride_rules` (§F17.2) — the seeded general rule. */
export const DEFAULT_SCHEDULED_RULE: ScheduledRuleInput = {
  cityId: null,
  rideCategoryId: null,
  maxDaysAhead: 7,
  minLeadMinutes: 30,
  maxOpenPerPassenger: 3,
  lockDemandNormal: true,
  marketplaceEnabled: true,
  marketplaceRadiusKm: 30,
  favoriteExclusiveMinutes: 30,
  driverAssignmentLeadMinutes: 60,
  confirmationTimeoutMinutes: 10,
  finalConfirmationMinutesBefore: 15,
  finalConfirmationTimeoutMinutes: 5,
  searchStartMinutesBefore: 10,
  riderReminderOffsets: [1440, 60, 15],
  driverReminderOffsets: [1440, 180],
  freeCancelMinutesBefore: 60,
  lateCancelFeeType: 'fixed',
  lateCancelFeeAmount: 10,
  lateCancelFeePercent: null,
  lateCancelDriverCompensationPercent: 50,
  driverFreeReleaseMinutesBefore: 120,
  driverLateReleasePenaltyPoints: 3,
  driverConfirmationMissedPenaltyPoints: 3,
  driverNoShowPenaltyPoints: 6,
  driverNoShowGraceMinutes: 10,
  maxReservationsPerDriver: 5,
  reservationGapMinutes: 30,
  isActive: true,
}

export const SCHEDULED_FEE_TYPES: ScheduledFeeType[] = ['none', 'fixed', 'percent', 'pricing_rule']

export const SCHEDULED_FEE_TYPE_KEY: Record<ScheduledFeeType, TranslationKey> = {
  none: 'cxFeeTypeNone',
  fixed: 'cxFeeTypeFixed',
  percent: 'cxFeeTypePercent',
  pricing_rule: 'cxFeeTypePricingRule',
}

/** `reservation=` filter values of `GET /admin/scheduled-trips`. */
export const SCHEDULED_RESERVATION_FILTERS: ScheduledReservationFilter[] = ['none', 'reserved', 'confirmed', 'assigned']

export function parseReservationFilter(value: string | null): ScheduledReservationFilter | '' {
  return SCHEDULED_RESERVATION_FILTERS.includes(value as ScheduledReservationFilter) ? (value as ScheduledReservationFilter) : ''
}

export const RESERVATION_FILTER_KEY: Record<ScheduledReservationFilter, TranslationKey> = {
  none: 'sdResNone',
  reserved: 'sdResReserved',
  confirmed: 'sdResConfirmed',
  assigned: 'sdResAssigned',
}

export const RESERVATION_SOURCE_KEY: Record<ReservationSource, TranslationKey> = {
  marketplace: 'sdSourceMarketplace',
  favorite: 'sdSourceFavorite',
  admin: 'sdSourceAdmin',
}

export const RELEASE_REASON_KEY: Record<ReservationReleaseReason, TranslationKey> = {
  driver_released: 'sdReleaseDriver',
  confirmation_missed: 'sdReleaseConfirmationMissed',
  final_confirmation_missed: 'sdReleaseFinalMissed',
  no_show: 'sdReleaseNoShow',
  trip_cancelled: 'sdReleaseTripCancelled',
  admin: 'sdReleaseAdmin',
}

export const REMINDER_KIND_KEY: Record<ReminderKind, TranslationKey> = {
  reminder: 'sdKindReminder',
  confirm_request: 'sdKindConfirm',
  final_confirm_request: 'sdKindFinalConfirm',
}

/** A reservation slot that ended without the driver serving the trip (counts against the driver, §F17.3 step 10). */
export const AT_FAULT_RELEASES: ReservationReleaseReason[] = ['confirmation_missed', 'final_confirmation_missed', 'no_show']

/** Reservation states that hold the trip (only one active reservation per trip). */
export const ACTIVE_RESERVATION_STATUSES = ['reserved', 'confirmed', 'assigned'] as const

// ---------------------------------------------------------------------------
// KPI helpers (§F17.5)
// ---------------------------------------------------------------------------

/** Rates come as 0..1; a value above 1 is read as a percentage (the spec does not fix the unit). */
export function rateShare(value: number | null | undefined): number | null {
  if (typeof value !== 'number' || !Number.isFinite(value) || value < 0) return null
  return value > 1 ? Math.min(1, value / 100) : value
}

const percentFormatter = new Intl.NumberFormat('en-US', { minimumFractionDigits: 1, maximumFractionDigits: 1 })

export function formatShare(value: number | null | undefined) {
  const share = rateShare(value)
  return share === null ? '—' : `${percentFormatter.format(share * 100)}%`
}

/** Driver no-show rate: `driverNoShows ÷ booked` (§F17.5 defines no such rate; derived from the stats counters). */
export function driverNoShowRate(stats: { driverNoShows: number; booked: number } | null | undefined): number | null {
  if (!stats || !Number.isFinite(stats.booked) || stats.booked <= 0 || !Number.isFinite(stats.driverNoShows)) return null
  return Math.min(1, stats.driverNoShows / stats.booked)
}

// ---------------------------------------------------------------------------
// Rule selection & planned timeline (§F17.2 "اختيار القاعدة", §F17.3 "الخط الزمني")
// ---------------------------------------------------------------------------

/** Most specific active rule: (city + category) → city → category → general. */
export function pickRule(rules: ScheduledRule[], scope: { cityId?: string | null; rideCategoryId?: string | null }): ScheduledRule | null {
  const active = rules.filter((rule) => rule.isActive)
  const city = scope.cityId ?? null
  const category = scope.rideCategoryId ?? null
  const find = (cityId: string | null, rideCategoryId: string | null) =>
    active.find((rule) => (rule.cityId ?? null) === cityId && (rule.rideCategoryId ?? null) === rideCategoryId)
  return (city && category ? find(city, category) : undefined) ?? (city ? find(city, null) : undefined) ?? (category ? find(null, category) : undefined) ?? find(null, null) ?? null
}

export type TimelineStepKey = 'booked' | 'firstConfirm' | 'finalConfirm' | 'searchStart' | 'pickup'

export interface TimelineStep {
  key: TimelineStepKey
  /** Planned instant (ms) from the rule and `scheduledAt`; null when the rule is unknown. */
  plannedAt: number | null
  /** Recorded instant (ms) from the reservation history / trip fields; null when it did not happen (yet). */
  actualAt: number | null
  state: 'done' | 'missed' | 'pending'
}

const ms = (value: string | null | undefined) => {
  if (!value) return null
  const time = new Date(value).getTime()
  return Number.isNaN(time) ? null : time
}

/**
 * Reservation/confirmation timeline of a scheduled trip: T-60 first confirmation, T-15 final confirmation and T-10 search start
 * (planned from the applicable rule) merged with what the reservations recorded. The latest non-released reservation wins.
 */
export function buildTimeline(trip: TripDetail, rule: ScheduledRule | null, now = Date.now()): TimelineStep[] {
  const pickupAt = ms(trip.scheduledAt)
  const planned = (minutes: number | undefined) => (pickupAt !== null && typeof minutes === 'number' ? pickupAt - minutes * 60_000 : null)
  const reservations = trip.scheduling?.reservations ?? []
  const current =
    [...reservations].reverse().find((reservation) => ACTIVE_RESERVATION_STATUSES.includes(reservation.status as (typeof ACTIVE_RESERVATION_STATUSES)[number]) || reservation.status === 'completed') ??
    null
  const confirmedAt = ms(current?.confirmedAt)
  const finalAt = ms(current?.assignedAt)
  const searchAt = ms(trip.scheduling?.searchStartsAt) ?? planned(rule?.searchStartMinutesBefore)
  const status = (actual: number | null, plannedAt: number | null): TimelineStep['state'] => (actual !== null ? 'done' : plannedAt !== null && plannedAt < now ? 'missed' : 'pending')

  const firstPlanned = planned(rule?.driverAssignmentLeadMinutes)
  const finalPlanned = planned(rule?.finalConfirmationMinutesBefore)
  const assignedActual = finalAt ?? ms(trip.timeline.assignedAt)
  const searchStarted = trip.status !== 'scheduled' && trip.status !== 'cancelled' && trip.status !== 'no_drivers'
  return [
    { key: 'booked', plannedAt: null, actualAt: ms(trip.timeline.requestedAt), state: 'done' },
    { key: 'firstConfirm', plannedAt: firstPlanned, actualAt: confirmedAt, state: status(confirmedAt ?? (current?.status === 'assigned' ? assignedActual : null), firstPlanned) },
    { key: 'finalConfirm', plannedAt: finalPlanned, actualAt: assignedActual, state: status(assignedActual, finalPlanned) },
    { key: 'searchStart', plannedAt: searchAt, actualAt: searchStarted && searchAt !== null && searchAt <= now ? searchAt : null, state: searchStarted ? 'done' : 'pending' },
    { key: 'pickup', plannedAt: pickupAt, actualAt: ms(trip.timeline.arrivedAt) ?? ms(trip.timeline.startedAt), state: status(ms(trip.timeline.arrivedAt) ?? ms(trip.timeline.startedAt), pickupAt) },
  ]
}

export const TIMELINE_STEP_KEY: Record<TimelineStepKey, TranslationKey> = {
  booked: 'sdStepBooked',
  firstConfirm: 'sdStepFirstConfirm',
  finalConfirm: 'sdStepFinalConfirm',
  searchStart: 'sdStepSearchStart',
  pickup: 'sdStepPickup',
}

/** Commitment summary of a trip's reservation history. */
export function commitmentOf(trip: TripDetail) {
  const reservations = trip.scheduling?.reservations ?? []
  const released = reservations.filter((reservation) => reservation.status === 'released' || reservation.status === 'no_show')
  return {
    total: reservations.length,
    rematches: released.filter((reservation) => reservation.releaseReason !== 'trip_cancelled' && reservation.releaseReason !== 'admin').length,
    noShows: reservations.filter((reservation) => reservation.status === 'no_show' || reservation.releaseReason === 'no_show').length,
    lateReleases: reservations.filter((reservation) => reservation.isLateRelease).length,
    penaltyPoints: reservations.reduce((sum, reservation) => sum + (reservation.penaltyPoints ?? 0), 0),
  }
}

// ---------------------------------------------------------------------------
// List filters (client side complements of `GET /admin/scheduled-trips`)
// ---------------------------------------------------------------------------

export type DriverAssignmentFilter = '' | 'with' | 'without'

export function parseDriverFilter(value: string | null): DriverAssignmentFilter {
  return value === 'with' || value === 'without' ? value : ''
}

const hasDriver = (row: ScheduledTripRow) => row.reservationStatus !== null && row.reservationStatus !== 'none' && row.reservationStatus !== 'released' && row.reservationStatus !== 'cancelled'

export interface ScheduledClientFilters {
  driver: DriverAssignmentFilter
  atRisk: boolean
  rideCategoryId: string
  /** Names matched when a row has no `rideCategoryId`. */
  categoryNames: string[]
  zoneId: string
}

/** Filters that the endpoint may not support; applied to whatever rows came back. */
export function needsClientFilter(filters: ScheduledClientFilters) {
  return filters.atRisk || filters.driver === 'with' || filters.rideCategoryId !== '' || filters.zoneId !== ''
}

export function applyClientFilters(rows: ScheduledTripRow[], filters: ScheduledClientFilters) {
  return rows.filter((row) => {
    if (filters.atRisk && !row.atRisk) return false
    if (filters.driver === 'with' && !hasDriver(row)) return false
    if (filters.driver === 'without' && hasDriver(row)) return false
    if (filters.rideCategoryId) {
      if (row.rideCategoryId) {
        if (row.rideCategoryId !== filters.rideCategoryId) return false
      } else if (row.categoryName && !filters.categoryNames.includes(row.categoryName)) return false
    }
    if (filters.zoneId && row.zoneId && row.zoneId !== filters.zoneId) return false
    return true
  })
}

/** Countdown text `2d 3h` / `4h 10m` / `25m` from minutes; negative values (overdue) keep their sign. */
export function formatCountdown(minutes: number | null | undefined) {
  if (typeof minutes !== 'number' || !Number.isFinite(minutes)) return '—'
  const sign = minutes < 0 ? '-' : ''
  const total = Math.abs(Math.round(minutes))
  const days = Math.floor(total / 1440)
  const hours = Math.floor((total % 1440) / 60)
  const mins = total % 60
  if (days > 0) return `${sign}${days}d ${hours}h`
  if (hours > 0) return `${sign}${hours}h ${mins}m`
  return `${sign}${mins}m`
}

// ---------------------------------------------------------------------------
// Rule form: reminder offsets, validation (§F17.2)
// ---------------------------------------------------------------------------

/** `1440, 60 15` → `[1440, 60, 15]`; null when any token is not a positive whole number. Duplicates collapse, order is descending. */
export function parseOffsets(text: string): number[] | null {
  const tokens = text
    .split(/[\s,،;]+/)
    .map((token) => token.trim())
    .filter(Boolean)
  const values: number[] = []
  for (const token of tokens) {
    const value = Number(token)
    if (!Number.isInteger(value) || value <= 0) return null
    values.push(value)
  }
  return [...new Set(values)].sort((a, b) => b - a)
}

export const formatOffsets = (offsets: number[]) => offsets.join(', ')

/** `1440` → `24h`, `90` → `1h 30m`, `15` → `15m`. */
export function formatOffsetLabel(minutes: number) {
  const hours = Math.floor(minutes / 60)
  const mins = minutes % 60
  if (hours === 0) return `${mins}m`
  return mins === 0 ? `${hours}h` : `${hours}h ${mins}m`
}

export type ScheduledRuleErrorField =
  | 'maxDaysAhead'
  | 'minLeadMinutes'
  | 'maxOpenPerPassenger'
  | 'marketplaceRadiusKm'
  | 'favoriteExclusiveMinutes'
  | 'driverAssignmentLeadMinutes'
  | 'confirmationTimeoutMinutes'
  | 'finalConfirmationMinutesBefore'
  | 'finalConfirmationTimeoutMinutes'
  | 'searchStartMinutesBefore'
  | 'riderReminderOffsets'
  | 'driverReminderOffsets'
  | 'freeCancelMinutesBefore'
  | 'lateCancelFeeAmount'
  | 'lateCancelFeePercent'
  | 'lateCancelDriverCompensationPercent'
  | 'driverFreeReleaseMinutesBefore'
  | 'driverLateReleasePenaltyPoints'
  | 'driverConfirmationMissedPenaltyPoints'
  | 'driverNoShowPenaltyPoints'
  | 'driverNoShowGraceMinutes'
  | 'maxReservationsPerDriver'
  | 'reservationGapMinutes'
  | 'scope'

const isWhole = (value: number, min: number) => Number.isInteger(value) && value >= min
const hasAtMostTwoDecimals = (value: number) => Math.abs(value * 100 - Math.round(value * 100)) < 1e-6

/**
 * Client mirror of the §F17.2/§F17.3 column rules (the server stays authoritative with `422 validation_failed`):
 * integers within range, the timeline order T−assignment lead > T−final confirmation > T−search start, fee fields that
 * match the fee type, percentages in 0..100 and no second rule for the same (city, category) pair.
 */
export function validateScheduledRule(input: ScheduledRuleInput, others: ScheduledRule[]): Partial<Record<ScheduledRuleErrorField, TranslationKey>> {
  const errors: Partial<Record<ScheduledRuleErrorField, TranslationKey>> = {}
  const whole = (field: ScheduledRuleErrorField, value: number, min: number) => {
    if (!isWhole(value, min)) errors[field] = min >= 1 ? 'sdErrPositiveInt' : 'sdErrNonNegativeInt'
  }
  whole('maxDaysAhead', input.maxDaysAhead, 1)
  whole('minLeadMinutes', input.minLeadMinutes, 0)
  whole('maxOpenPerPassenger', input.maxOpenPerPassenger, 1)
  if (input.marketplaceEnabled) whole('marketplaceRadiusKm', input.marketplaceRadiusKm, 1)
  whole('favoriteExclusiveMinutes', input.favoriteExclusiveMinutes, 0)
  whole('driverAssignmentLeadMinutes', input.driverAssignmentLeadMinutes, 1)
  whole('confirmationTimeoutMinutes', input.confirmationTimeoutMinutes, 1)
  whole('finalConfirmationMinutesBefore', input.finalConfirmationMinutesBefore, 1)
  whole('finalConfirmationTimeoutMinutes', input.finalConfirmationTimeoutMinutes, 1)
  whole('searchStartMinutesBefore', input.searchStartMinutesBefore, 0)
  whole('freeCancelMinutesBefore', input.freeCancelMinutesBefore, 0)
  whole('driverFreeReleaseMinutesBefore', input.driverFreeReleaseMinutesBefore, 0)
  whole('driverLateReleasePenaltyPoints', input.driverLateReleasePenaltyPoints, 0)
  whole('driverConfirmationMissedPenaltyPoints', input.driverConfirmationMissedPenaltyPoints, 0)
  whole('driverNoShowPenaltyPoints', input.driverNoShowPenaltyPoints, 0)
  whole('driverNoShowGraceMinutes', input.driverNoShowGraceMinutes, 0)
  whole('maxReservationsPerDriver', input.maxReservationsPerDriver, 1)
  whole('reservationGapMinutes', input.reservationGapMinutes, 0)

  if (!errors.driverAssignmentLeadMinutes && !errors.finalConfirmationMinutesBefore && input.driverAssignmentLeadMinutes <= input.finalConfirmationMinutesBefore) {
    errors.driverAssignmentLeadMinutes = 'sdErrLeadOrder'
  }
  if (!errors.finalConfirmationMinutesBefore && !errors.searchStartMinutesBefore && input.finalConfirmationMinutesBefore <= input.searchStartMinutesBefore) {
    errors.finalConfirmationMinutesBefore = 'sdErrFinalOrder'
  }

  for (const field of ['riderReminderOffsets', 'driverReminderOffsets'] as const) {
    const offsets = input[field]
    if (!offsets.every((value) => isWhole(value, 1))) errors[field] = 'sdErrOffsets'
  }

  if (input.lateCancelFeeType === 'fixed' && (input.lateCancelFeeAmount === null || !Number.isFinite(input.lateCancelFeeAmount) || input.lateCancelFeeAmount < 0)) errors.lateCancelFeeAmount = 'fieldRequired'
  else if (input.lateCancelFeeType === 'fixed' && input.lateCancelFeeAmount !== null && !hasAtMostTwoDecimals(input.lateCancelFeeAmount)) errors.lateCancelFeeAmount = 'fvErrDecimals'
  if (input.lateCancelFeeType === 'percent' && (input.lateCancelFeePercent === null || !Number.isFinite(input.lateCancelFeePercent) || input.lateCancelFeePercent < 0 || input.lateCancelFeePercent > 100)) {
    errors.lateCancelFeePercent = 'cxErrPercentRange'
  } else if (input.lateCancelFeeType === 'percent' && input.lateCancelFeePercent !== null && !hasAtMostTwoDecimals(input.lateCancelFeePercent)) errors.lateCancelFeePercent = 'fvErrDecimals'
  if (!Number.isFinite(input.lateCancelDriverCompensationPercent) || input.lateCancelDriverCompensationPercent < 0 || input.lateCancelDriverCompensationPercent > 100) errors.lateCancelDriverCompensationPercent = 'cxErrPercentRange'
  else if (!hasAtMostTwoDecimals(input.lateCancelDriverCompensationPercent)) errors.lateCancelDriverCompensationPercent = 'fvErrDecimals'

  if (others.some((other) => (other.cityId ?? null) === input.cityId && (other.rideCategoryId ?? null) === input.rideCategoryId)) errors.scope = 'sdErrScopeExists'
  return errors
}

/** Non-blocking hints: timeouts that run into the next step of the timeline (§F17.3 steps 5–7). */
export function scheduledRuleWarnings(input: ScheduledRuleInput): TranslationKey[] {
  const warnings: TranslationKey[] = []
  if (input.driverAssignmentLeadMinutes - input.confirmationTimeoutMinutes < input.finalConfirmationMinutesBefore) warnings.push('sdWarnConfirmOverlap')
  if (input.finalConfirmationMinutesBefore - input.finalConfirmationTimeoutMinutes < input.searchStartMinutesBefore) warnings.push('sdWarnFinalOverlap')
  if (input.minLeadMinutes < input.searchStartMinutesBefore) warnings.push('sdWarnLeadShort')
  return warnings
}

/** Full PUT body from a listed rule (drops server-owned fields). */
export function scheduledRuleInputOf(rule: ScheduledRule, isActive = rule.isActive): ScheduledRuleInput {
  const { id, createdAt, updatedAt, ...input } = rule
  void id
  void createdAt
  void updatedAt
  return { ...input, isActive }
}

/** Where a rule applies, most specific first (for sorting the list). */
export function ruleScopeRank(rule: Pick<ScheduledRule, 'cityId' | 'rideCategoryId'>) {
  return (rule.cityId ? 2 : 0) + (rule.rideCategoryId ? 1 : 0)
}

/** Rows whose pickup is between now and `minutes` ahead (the list endpoint filters by date only). Overdue rows are left out. */
export function upcomingWithin(rows: ScheduledTripRow[], minutes: number, now = Date.now()) {
  const limit = now + minutes * 60_000
  return rows.filter((row) => {
    const at = new Date(row.scheduledAt).getTime()
    return Number.isFinite(at) && at >= now - 60_000 && at <= limit
  })
}
