import type { TranslationKey } from '../i18n'
import type { BookingType, FavoriteDiscountRule, FavoriteDiscountRuleInput, FavoriteRuleStatus, FavoriteStatus } from './types'

// ---------------------------------------------------------------------------
// F16 — favorite driver discount rules (docs/10 §F16.1–§F16.3)
// ---------------------------------------------------------------------------

export const FAVORITE_BOOKING_TYPES: BookingType[] = ['now', 'scheduled']
export const FAVORITE_RULE_STATUSES: FavoriteRuleStatus[] = ['active', 'scheduled', 'expired', 'inactive']
export const FAVORITE_STATUSES: FavoriteStatus[] = ['requested', 'accepted', 'unavailable', 'rejected', 'expired']

/** `discountPercent` bounds from §F16.3 ("بين 1 و50"). */
export const FAVORITE_MIN_PERCENT = 1
export const FAVORITE_MAX_PERCENT = 50

/** `Favorites:*` defaults (§F16 settings table) — shown as help text only. */
export const FAVORITES_MAX_PER_PASSENGER = 20
export const FAVORITES_EXCLUSIVE_TIMEOUT_SECONDS = 30

/** Statuses after which the trip went on to normal matching (§F16.2 steps 2–3). */
export const FAVORITE_FALLBACK_STATUSES: FavoriteStatus[] = ['unavailable', 'rejected', 'expired']

export const FAVORITE_OUTCOME_KEY: Record<FavoriteStatus, TranslationKey> = {
  requested: 'fvOutcomeRequested',
  accepted: 'fvOutcomeAccepted',
  unavailable: 'fvOutcomeUnavailable',
  rejected: 'fvOutcomeRejected',
  expired: 'fvOutcomeExpired',
}

/** Discount line sources stored in `fare_breakdown.discounts[].source` (docs/08 §F11.6). */
export const DISCOUNT_SOURCE_KEY: Record<string, TranslationKey> = {
  promotion: 'tripSrcPromotion',
  favorite_driver: 'tripSrcFavorite',
}

/** Status of a rule: inactive wins, then the validity window (`validTo` may be open ended). */
export function favoriteRuleStatusOf(rule: Pick<FavoriteDiscountRule, 'isActive' | 'validFrom' | 'validTo'>, now = Date.now()): FavoriteRuleStatus {
  if (!rule.isActive) return 'inactive'
  const from = new Date(rule.validFrom).getTime()
  const to = rule.validTo ? new Date(rule.validTo).getTime() : Number.NaN
  if (Number.isFinite(to) && to <= now) return 'expired'
  if (Number.isFinite(from) && from > now) return 'scheduled'
  return 'active'
}

/** Full PUT body from a fetched/listed rule (drops server-owned fields). */
export function favoriteRuleInputOf(rule: FavoriteDiscountRule, isActive = rule.isActive): FavoriteDiscountRuleInput {
  return {
    name: rule.name,
    discountPercent: rule.discountPercent,
    maxDiscountAmount: rule.maxDiscountAmount,
    minFare: rule.minFare ?? null,
    stackableWithPromotions: rule.stackableWithPromotions,
    validFrom: rule.validFrom,
    validTo: rule.validTo ?? null,
    rideCategoryIds: rule.rideCategoryIds ?? null,
    zoneIds: rule.zoneIds ?? null,
    bookingTypes: rule.bookingTypes ?? null,
    priority: rule.priority,
    isActive,
  }
}

export type FavoriteRuleErrorField = 'name' | 'discountPercent' | 'maxDiscountAmount' | 'minFare' | 'validFrom' | 'validTo' | 'priority'

const hasAtMostTwoDecimals = (value: number) => Math.abs(value * 100 - Math.round(value * 100)) < 1e-6

/** Client mirror of the §F16.1/§F16.3 column rules (the server stays authoritative with `422 validation_failed`). */
export function validateFavoriteRule(input: FavoriteDiscountRuleInput): Partial<Record<FavoriteRuleErrorField, TranslationKey>> {
  const errors: Partial<Record<FavoriteRuleErrorField, TranslationKey>> = {}
  if (!input.name.trim()) errors.name = 'fieldRequired'
  if (!Number.isFinite(input.discountPercent) || input.discountPercent < FAVORITE_MIN_PERCENT || input.discountPercent > FAVORITE_MAX_PERCENT) errors.discountPercent = 'fvErrPercent'
  else if (!hasAtMostTwoDecimals(input.discountPercent)) errors.discountPercent = 'fvErrDecimals'
  if (!Number.isFinite(input.maxDiscountAmount) || input.maxDiscountAmount <= 0) errors.maxDiscountAmount = 'fvErrMaxAmount'
  else if (!hasAtMostTwoDecimals(input.maxDiscountAmount)) errors.maxDiscountAmount = 'fvErrDecimals'
  if (input.minFare !== null && (!Number.isFinite(input.minFare) || input.minFare < 0)) errors.minFare = 'invalidNumber'
  if (!input.validFrom) errors.validFrom = 'fieldRequired'
  if (input.validTo && input.validFrom && new Date(input.validTo) <= new Date(input.validFrom)) errors.validTo = 'endAfterStart'
  if (!Number.isInteger(input.priority) || input.priority < 0) errors.priority = 'fvErrPriority'
  return errors
}

// ---------------------------------------------------------------------------
// Effective rule / overlap analysis (§F16.2 "الخصم": highest priority among active, valid, matching rules)
// ---------------------------------------------------------------------------

type Scope = Pick<FavoriteDiscountRuleInput, 'rideCategoryIds' | 'zoneIds' | 'bookingTypes' | 'minFare'>
type Window = Pick<FavoriteDiscountRuleInput, 'validFrom' | 'validTo'>

const listOf = <T>(value: T[] | null | undefined) => (value && value.length > 0 ? value : null)

/** `a` (null = "everything") contains every value `b` can select. */
function covers<T>(a: T[] | null | undefined, b: T[] | null | undefined) {
  const left = listOf(a)
  if (!left) return true
  const right = listOf(b)
  return right !== null && right.every((item) => left.includes(item))
}

/** Two selectors can both match some trip. */
function intersects<T>(a: T[] | null | undefined, b: T[] | null | undefined) {
  const left = listOf(a)
  const right = listOf(b)
  return !left || !right || left.some((item) => right.includes(item))
}

function windowsOverlap(a: Window, b: Window) {
  const aFrom = new Date(a.validFrom).getTime()
  const bFrom = new Date(b.validFrom).getTime()
  const aTo = a.validTo ? new Date(a.validTo).getTime() : Number.POSITIVE_INFINITY
  const bTo = b.validTo ? new Date(b.validTo).getTime() : Number.POSITIVE_INFINITY
  return aFrom < bTo && bFrom < aTo
}

/** Every trip that satisfies `b`'s restrictions also satisfies `a`'s (so `a` matches wherever `b` does). */
function scopeCovers(a: Scope, b: Scope) {
  return covers(a.rideCategoryIds, b.rideCategoryIds) && covers(a.zoneIds, b.zoneIds) && covers(a.bookingTypes, b.bookingTypes) && (a.minFare ?? 0) <= (b.minFare ?? 0)
}

function scopesIntersect(a: Scope, b: Scope) {
  return intersects(a.rideCategoryIds, b.rideCategoryIds) && intersects(a.zoneIds, b.zoneIds) && intersects(a.bookingTypes, b.bookingTypes)
}

/** Active rules whose window contains `now`. */
function inForce<T extends Pick<FavoriteDiscountRule, 'isActive' | 'validFrom' | 'validTo'>>(rules: T[], now: number) {
  return rules.filter((rule) => favoriteRuleStatusOf(rule, now) === 'active')
}

export interface RuleAnalysis {
  /** Rules that win for at least part of the trip space right now. */
  effective: Set<string>
  /** Active rules that a higher-priority active rule fully covers — they never apply while it is in force. */
  shadowed: Map<string, string>
  /** Rule id → ids of other active rules with the same priority and overlapping scope + window (undefined winner). */
  ties: Map<string, string[]>
}

type Analyzable = FavoriteDiscountRuleInput & { id: string }

/**
 * Explains which rules currently decide the discount: the highest `priority` among active, in-window, matching rules wins
 * (§F16.2), so a rule is *shadowed* when a higher-priority rule in force covers everything it matches, and rules that share a
 * priority while overlapping have no defined winner (a *tie* the admin should resolve by changing a priority).
 */
export function analyzeRules(rules: Analyzable[], now = Date.now()): RuleAnalysis {
  const live = inForce(rules, now)
  const effective = new Set<string>()
  const shadowed = new Map<string, string>()
  for (const rule of live) {
    const cover = live.find((other) => other.id !== rule.id && other.priority > rule.priority && scopeCovers(other, rule))
    if (cover) shadowed.set(rule.id, cover.id)
    else effective.add(rule.id)
  }
  const ties = new Map<string, string[]>()
  const candidates = rules.filter((rule) => rule.isActive)
  for (const rule of candidates) {
    const same = candidates.filter((other) => other.id !== rule.id && other.priority === rule.priority && windowsOverlap(rule, other) && scopesIntersect(rule, other)).map((other) => other.id)
    if (same.length > 0) ties.set(rule.id, same)
  }
  return { effective, shadowed, ties }
}

/** Other active rules that would tie with a draft (same priority, overlapping scope and window). */
export function findTies(draft: FavoriteDiscountRuleInput, others: Analyzable[]) {
  if (!draft.isActive || !draft.validFrom) return []
  return others.filter((other) => other.isActive && other.priority === draft.priority && windowsOverlap(draft, other) && scopesIntersect(draft, other))
}

/** Rate normalisation: the spec does not say whether `favoriteBookingRate` is 0..1 or a percentage; values above 1 are read as percent. */
export function favoriteRateShare(value: number | null | undefined) {
  if (typeof value !== 'number' || !Number.isFinite(value) || value < 0) return null
  return value > 1 ? Math.min(1, value / 100) : value
}
