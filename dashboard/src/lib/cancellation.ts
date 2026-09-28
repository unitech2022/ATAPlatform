import type { TranslationKey } from '../i18n'
import type {
  AtFault,
  BookingType,
  CancellationStats,
  CancellationFeeStatus,
  CancellationFeeType,
  CancellationRuleInput,
  CancellationStage,
  ExcuseStatus,
  ReasonActor,
  ReliabilityAction,
  ReliabilityRole,
  RestrictionLevel,
  RuleActor,
  RuleStage,
  TripActor,
} from './types'

export const CANCELLATION_STAGES: CancellationStage[] = ['before_accept', 'after_accept', 'en_route', 'arrived', 'waiting', 'no_show', 'scheduled']
export const RULE_STAGES: RuleStage[] = ['before_accept', 'after_accept', 'en_route', 'arrived', 'waiting', 'no_show']
export const REASON_ACTORS: ReasonActor[] = ['passenger', 'driver', 'system']
export const RULE_ACTORS: RuleActor[] = ['passenger', 'driver']
export const EVENT_ACTORS: TripActor[] = ['passenger', 'driver', 'system', 'admin']
export const AT_FAULTS: AtFault[] = ['none', 'passenger', 'driver']
export const BOOKING_TYPES: BookingType[] = ['now', 'scheduled']
export const FEE_TYPES: CancellationFeeType[] = ['none', 'fixed', 'percent', 'pricing_rule']
export const FEE_STATUSES: CancellationFeeStatus[] = ['none', 'charged', 'pending_review', 'waived', 'failed', 'refunded']
export const EXCUSE_STATUSES: ExcuseStatus[] = ['pending', 'approved', 'rejected', 'not_applicable']
export const RELIABILITY_ROLES: ReliabilityRole[] = ['driver', 'passenger']
export const RESTRICTION_LEVELS: RestrictionLevel[] = ['none', 'warning', 'matching_deprioritized', 'incentives_reduced', 'temporarily_restricted', 'suspended']
export const RELIABILITY_ACTIONS: ReliabilityAction[] = ['add_points', 'remove_points', 'set_level', 'clear_restriction']

/** `Cancellation:ExcuseReviewSlaHours` default (docs/09 §F14.5); the queue's `slaBreached` flag is authoritative. */
export const EXCUSE_SLA_HOURS = 48

export const STAGE_KEY: Record<CancellationStage, TranslationKey> = {
  before_accept: 'cxStageBeforeAccept',
  after_accept: 'cxStageAfterAccept',
  en_route: 'cxStageEnRoute',
  arrived: 'cxStageArrived',
  waiting: 'cxStageWaiting',
  no_show: 'cxStageNoShow',
  scheduled: 'cxStageScheduled',
}

export const ACTOR_KEY: Record<TripActor, TranslationKey> = {
  passenger: 'actorPassenger',
  driver: 'actorDriver',
  system: 'actorSystem',
  admin: 'actorAdmin',
}

export const AT_FAULT_KEY: Record<AtFault, TranslationKey> = {
  none: 'cxFaultNone',
  passenger: 'actorPassenger',
  driver: 'actorDriver',
}

export const BOOKING_TYPE_KEY: Record<BookingType, TranslationKey> = {
  now: 'bookingNow',
  scheduled: 'bookingScheduled',
}

export const FEE_TYPE_KEY: Record<CancellationFeeType, TranslationKey> = {
  none: 'cxFeeTypeNone',
  fixed: 'cxFeeTypeFixed',
  percent: 'cxFeeTypePercent',
  pricing_rule: 'cxFeeTypePricingRule',
}

export const ROLE_KEY: Record<ReliabilityRole, TranslationKey> = {
  driver: 'actorDriver',
  passenger: 'actorPassenger',
}

export const RELIABILITY_ACTION_KEY: Record<ReliabilityAction, TranslationKey> = {
  add_points: 'rlActionAddPoints',
  remove_points: 'rlActionRemovePoints',
  set_level: 'rlActionSetLevel',
  clear_restriction: 'rlActionClear',
}

/** Levels a threshold can define per role (seeded ladder in §F14.6; passengers have no matching/incentive effects). */
export const THRESHOLD_LEVELS: Record<ReliabilityRole, Exclude<RestrictionLevel, 'none'>[]> = {
  driver: ['warning', 'matching_deprioritized', 'incentives_reduced', 'temporarily_restricted', 'suspended'],
  passenger: ['warning', 'temporarily_restricted', 'suspended'],
}

const percentFormatter = new Intl.NumberFormat('en-US', { minimumFractionDigits: 1, maximumFractionDigits: 1 })

/** 0..1 ratio → `18.0%`. */
export function formatRate(value: number | null | undefined) {
  return typeof value === 'number' && Number.isFinite(value) ? `${percentFormatter.format(value * 100)}%` : '—'
}

/** Seconds → `m:ss` (free windows are short). */
export function formatWindow(seconds: number | null | undefined) {
  if (typeof seconds !== 'number') return '—'
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m}:${String(s).padStart(2, '0')}`
}

export type RuleErrorField = 'name' | 'stage' | 'feeType' | 'feeAmount' | 'feePercent' | 'minFee' | 'maxFee' | 'driverCompensationPercent' | 'penaltyPoints' | 'freeWindowSeconds'

/** Client mirror of the save-time checks in §F14.3 (server stays authoritative with `422 validation_failed`). */
export function validateRule(input: CancellationRuleInput): Partial<Record<RuleErrorField, TranslationKey>> {
  const errors: Partial<Record<RuleErrorField, TranslationKey>> = {}
  if (!input.name.trim()) errors.name = 'fieldRequired'
  if (input.actor === 'passenger' && input.stage === 'before_accept' && input.feeType !== 'none') errors.feeType = 'cxErrNoFeeBeforeAccept'
  if (input.stage === 'no_show' && input.actor !== 'passenger') errors.stage = 'cxErrNoShowPassengerOnly'
  if (input.feeType === 'fixed' && (input.feeAmount === null || input.feeAmount < 0)) errors.feeAmount = 'fieldRequired'
  if (input.feeType === 'percent' && (input.feePercent === null || input.feePercent < 0 || input.feePercent > 100)) errors.feePercent = 'cxErrPercentRange'
  if (input.minFee !== null && input.maxFee !== null && input.minFee > input.maxFee) errors.maxFee = 'cxErrMinMax'
  if (input.driverCompensationPercent < 0 || input.driverCompensationPercent > 100) errors.driverCompensationPercent = 'cxErrPercentRange'
  if (!Number.isInteger(input.penaltyPoints) || input.penaltyPoints < 0) errors.penaltyPoints = 'invalidNumber'
  if (!Number.isInteger(input.freeWindowSeconds) || input.freeWindowSeconds < 0) errors.freeWindowSeconds = 'invalidNumber'
  return errors
}

/** Optional numeric input → number | null ('' → null, invalid → NaN so validation can flag it). */
export function optionalNumber(value: string): number | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : Number(trimmed)
}

/** Revenue KPI: Σ fee_charged − refunded (§F14.7); accepts either field name. */
export function feeRevenueOf(stats: CancellationStats | null | undefined) {
  return stats?.cancellationFeeRevenue ?? stats?.feeRevenue ?? null
}
