import type { TranslationKey } from '../i18n'
import { formatMoney, formatNumber } from './format'
import type {
  AdminRating,
  BookingType,
  DriverTier,
  DriverTierRuleInput,
  Incentive,
  IncentiveInput,
  IncentiveListStatus,
  IncentiveProgressStatus,
  IncentiveType,
  PaymentMethod,
  Promotion,
  PromotionInput,
  PromotionListStatus,
  PromotionType,
  RaterRole,
  RatingFlagReviewAction,
  RatingFlagStatus,
  RatingFlagType,
  RatingStatus,
  RedemptionReleaseReason,
  RedemptionStatus,
} from './types'

// ---------------------------------------------------------------------------
// Ratings (§F15.1–§F15.3)
// ---------------------------------------------------------------------------

export const RATER_ROLES: RaterRole[] = ['passenger', 'driver']
export const STAR_VALUES = [1, 2, 3, 4, 5] as const
export const RATING_STATUSES: RatingStatus[] = ['visible', 'hidden']
export const RATING_FLAG_TYPES: RatingFlagType[] = ['low_rating', 'low_average', 'abusive_comment']
export const RATING_FLAG_STATUSES: RatingFlagStatus[] = ['open', 'actioned', 'dismissed']
export const RATING_FLAG_REVIEW_ACTIONS: RatingFlagReviewAction[] = ['dismiss', 'warn', 'suspension_review']

/** `Ratings:LowRatingThreshold` default — stars at or below this raise a `low_rating` flag. */
export const LOW_RATING_THRESHOLD = 2

/** Direction label: the rater's role decides who is being rated. */
export const RATING_DIRECTION_KEY: Record<RaterRole, TranslationKey> = {
  passenger: 'rtDirPassengerToDriver',
  driver: 'rtDirDriverToPassenger',
}

export const RATING_FLAG_TYPE_KEY: Record<RatingFlagType, TranslationKey> = {
  low_rating: 'rtFlagTypeLowRating',
  low_average: 'rtFlagTypeLowAverage',
  abusive_comment: 'rtFlagTypeAbusive',
}

export const RATING_FLAG_ACTION_KEY: Record<RatingFlagReviewAction, TranslationKey> = {
  dismiss: 'rtActionDismiss',
  warn: 'rtActionWarn',
  suspension_review: 'rtActionSuspensionReview',
}

/** Stored `rating_flags.action` values. */
export const RATING_FLAG_OUTCOME_KEY: Record<string, TranslationKey> = {
  warned: 'rtOutcomeWarned',
  suspension_review: 'rtActionSuspensionReview',
  none: 'rtOutcomeNone',
}

/** Seeded tag codes (docs/10 "البيانات الأولية"); the catalog names win when available. */
export const RATING_TAG_KEY: Record<string, TranslationKey> = {
  driving: 'rtTagDriving',
  cleanliness: 'rtTagCleanliness',
  behaviour: 'rtTagBehaviour',
  navigation: 'rtTagNavigation',
  vehicle_condition: 'rtTagVehicleCondition',
  punctuality: 'rtTagPunctuality',
}

export const DRIVER_TAGS = ['driving', 'cleanliness', 'behaviour', 'navigation', 'vehicle_condition']
export const PASSENGER_TAGS = ['punctuality', 'behaviour', 'cleanliness']

const avgFormatter = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** Rating average with two decimals (`4.87`). */
export function formatAvg(value: number | null | undefined) {
  return typeof value === 'number' && Number.isFinite(value) ? avgFormatter.format(value) : '—'
}

export interface RatingBreakdown {
  count: number
  average: number | null
  distribution: Record<1 | 2 | 3 | 4 | 5, number>
  /** Tag counts split by meaning: ≥ 4 stars = liked, ≤ 3 = disliked (§F15.2). */
  tags: { code: string; positive: number; negative: number }[]
  recentComments: AdminRating[]
}

/** Client-side summary of the ratings a user received (the admin API exposes only the list). */
export function summarizeRatings(rows: AdminRating[]): RatingBreakdown {
  const visible = rows.filter((row) => row.status !== 'hidden')
  const distribution: RatingBreakdown['distribution'] = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 }
  const tags = new Map<string, { code: string; positive: number; negative: number }>()
  let sum = 0
  for (const row of visible) {
    const stars = Math.min(5, Math.max(1, Math.round(row.stars))) as 1 | 2 | 3 | 4 | 5
    distribution[stars] += 1
    sum += row.stars
    for (const code of row.tags ?? []) {
      const entry = tags.get(code) ?? { code, positive: 0, negative: 0 }
      if (row.stars >= 4) entry.positive += 1
      else entry.negative += 1
      tags.set(code, entry)
    }
  }
  return {
    count: visible.length,
    average: visible.length > 0 ? sum / visible.length : null,
    distribution,
    tags: [...tags.values()].sort((a, b) => b.positive + b.negative - (a.positive + a.negative)),
    recentComments: visible.filter((row) => row.comment && !row.commentHidden).slice(0, 5),
  }
}

// ---------------------------------------------------------------------------
// Promotions (§F15.4–§F15.6)
// ---------------------------------------------------------------------------

export const PROMOTION_TYPES: PromotionType[] = ['percent', 'fixed', 'free_booking_fee']
export const PROMOTION_STATUSES: PromotionListStatus[] = ['active', 'scheduled', 'expired', 'inactive']
export const REDEMPTION_STATUSES: RedemptionStatus[] = ['reserved', 'applied', 'released']
export const PROMO_PAYMENT_METHODS: PaymentMethod[] = ['cash', 'wallet', 'card']
export const PROMO_BOOKING_TYPES: BookingType[] = ['now', 'scheduled']

export const PROMOTION_TYPE_KEY: Record<PromotionType, TranslationKey> = {
  percent: 'prTypePercent',
  fixed: 'prTypeFixed',
  free_booking_fee: 'prTypeFreeBookingFee',
}

export const RELEASE_REASON_KEY: Record<RedemptionReleaseReason, TranslationKey> = {
  trip_cancelled: 'prRelTripCancelled',
  no_drivers: 'prRelNoDrivers',
  not_stacked: 'prRelNotStacked',
  payment_failed: 'prRelPaymentFailed',
  not_eligible_at_completion: 'prRelNotEligible',
  admin: 'prRelAdmin',
}

/** Latin capitals and digits, 4–20 (§F15.4); the server normalises to upper case too. */
export const PROMO_CODE_PATTERN = /^[A-Z0-9]{4,20}$/

export function normalizePromoCode(value: string) {
  return value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 20)
}

/** Status the list tabs use: inactive wins, then the validity window. */
export function promotionStatusOf(promotion: { isActive: boolean; validFrom: string; validTo: string }, now = Date.now()): PromotionListStatus {
  if (!promotion.isActive) return 'inactive'
  const from = new Date(promotion.validFrom).getTime()
  const to = new Date(promotion.validTo).getTime()
  if (Number.isFinite(to) && to < now) return 'expired'
  if (Number.isFinite(from) && from > now) return 'scheduled'
  return 'active'
}

/** `20%` / `10.00 SAR` / free booking fee. */
export function formatPromoValue(type: PromotionType, value: number, t: (key: TranslationKey) => string) {
  if (type === 'percent') return `${formatNumber(value)}%`
  if (type === 'fixed') return `${formatMoney(value)} ${t('sar')}`
  return t('prTypeFreeBookingFee')
}

/** Full PUT body from a fetched promotion (drops the counters). */
export function promotionInputOf(promotion: Promotion, isActive = promotion.isActive): PromotionInput {
  return {
    code: promotion.code,
    nameAr: promotion.nameAr,
    nameEn: promotion.nameEn,
    descriptionAr: promotion.descriptionAr,
    descriptionEn: promotion.descriptionEn,
    type: promotion.type,
    value: promotion.value,
    maxDiscount: promotion.maxDiscount,
    minFare: promotion.minFare,
    validFrom: promotion.validFrom,
    validTo: promotion.validTo,
    totalUsageLimit: promotion.totalUsageLimit,
    perUserLimit: promotion.perUserLimit,
    budgetAmount: promotion.budgetAmount,
    firstTripOnly: promotion.firstTripOnly,
    newUsersOnly: promotion.newUsersOnly,
    newUserDays: promotion.newUserDays,
    cityId: promotion.cityId,
    rideCategoryIds: promotion.rideCategoryIds,
    zoneIds: promotion.zoneIds,
    paymentMethods: promotion.paymentMethods,
    bookingTypes: promotion.bookingTypes,
    isStackable: promotion.isStackable,
    isPublic: promotion.isPublic,
    isActive,
  }
}

export type PromotionErrorField =
  | 'code'
  | 'nameAr'
  | 'nameEn'
  | 'value'
  | 'maxDiscount'
  | 'minFare'
  | 'validFrom'
  | 'validTo'
  | 'totalUsageLimit'
  | 'perUserLimit'
  | 'budgetAmount'
  | 'newUserDays'

const isNum = (value: number | null) => value !== null && Number.isFinite(value)
const badOptional = (value: number | null, min = 0) => value !== null && (!Number.isFinite(value) || value < min)
const badInt = (value: number | null, min: number) => value !== null && (!Number.isInteger(value) || value < min)

/** Client mirror of the §F15.4 column rules (server stays authoritative with `422 validation_failed`). */
export function validatePromotion(input: PromotionInput): Partial<Record<PromotionErrorField, TranslationKey>> {
  const errors: Partial<Record<PromotionErrorField, TranslationKey>> = {}
  if (!PROMO_CODE_PATTERN.test(input.code)) errors.code = 'prErrCode'
  if (!input.nameAr.trim()) errors.nameAr = 'fieldRequired'
  if (!input.nameEn.trim()) errors.nameEn = 'fieldRequired'
  if (input.type === 'percent' && (!isNum(input.value) || input.value < 1 || input.value > 100)) errors.value = 'prErrPercent'
  if (input.type === 'fixed' && (!isNum(input.value) || input.value <= 0)) errors.value = 'invalidNumber'
  if (badOptional(input.maxDiscount, 0.01)) errors.maxDiscount = 'invalidNumber'
  if (badOptional(input.minFare)) errors.minFare = 'invalidNumber'
  if (!input.validFrom) errors.validFrom = 'fieldRequired'
  if (!input.validTo) errors.validTo = 'fieldRequired'
  else if (input.validFrom && new Date(input.validTo) <= new Date(input.validFrom)) errors.validTo = 'endAfterStart'
  if (badInt(input.totalUsageLimit, 1)) errors.totalUsageLimit = 'invalidNumber'
  if (badInt(input.perUserLimit, 1)) errors.perUserLimit = 'invalidNumber'
  if (badOptional(input.budgetAmount, 0.01)) errors.budgetAmount = 'invalidNumber'
  if (input.newUsersOnly && badInt(input.newUserDays, 1)) errors.newUserDays = 'invalidNumber'
  return errors
}

// ---------------------------------------------------------------------------
// Driver tiers (§F15.7)
// ---------------------------------------------------------------------------

export const DRIVER_TIERS: DriverTier[] = ['bronze', 'silver', 'gold', 'platinum']

/** `Tiers:PeriodDays` default. */
export const TIER_PERIOD_DAYS = 28

export type TierErrorField = 'minCompletedTrips' | 'minRatingAvg' | 'minAcceptanceRate' | 'maxCancellationRate' | 'commissionDiscountPercent' | 'matchingNorm'

export function validateTierRule(input: DriverTierRuleInput): Partial<Record<TierErrorField, TranslationKey>> {
  const errors: Partial<Record<TierErrorField, TranslationKey>> = {}
  if (!Number.isInteger(input.minCompletedTrips) || input.minCompletedTrips < 0) errors.minCompletedTrips = 'invalidNumber'
  if (!Number.isFinite(input.minRatingAvg) || input.minRatingAvg < 0 || input.minRatingAvg > 5) errors.minRatingAvg = 'tierErrRating'
  if (!Number.isFinite(input.minAcceptanceRate) || input.minAcceptanceRate < 0 || input.minAcceptanceRate > 1) errors.minAcceptanceRate = 'cxErrPercentRange'
  if (!Number.isFinite(input.maxCancellationRate) || input.maxCancellationRate < 0 || input.maxCancellationRate > 1) errors.maxCancellationRate = 'cxErrPercentRange'
  if (!Number.isFinite(input.commissionDiscountPercent) || input.commissionDiscountPercent < 0 || input.commissionDiscountPercent > 100) errors.commissionDiscountPercent = 'cxErrPercentRange'
  if (!Number.isFinite(input.matchingNorm) || input.matchingNorm < 0 || input.matchingNorm > 1) errors.matchingNorm = 'tierErrNorm'
  return errors
}

/** `driver_share + (100 − driver_share) × discount / 100` — e.g. 80% with 10% → 82%. */
export function effectiveDriverShare(driverSharePercent: number, commissionDiscountPercent: number) {
  return driverSharePercent + ((100 - driverSharePercent) * commissionDiscountPercent) / 100
}

// ---------------------------------------------------------------------------
// Incentives (§F15.8–§F15.10)
// ---------------------------------------------------------------------------

export const INCENTIVE_TYPES: IncentiveType[] = ['daily', 'weekly', 'zone_quest', 'one_time']
export const INCENTIVE_STATUSES: IncentiveListStatus[] = ['active', 'upcoming', 'ended', 'inactive']
export const PROGRESS_STATUSES: IncentiveProgressStatus[] = ['in_progress', 'achieved', 'paid', 'expired', 'voided']

/** `Incentives:PayoutDelayHours` default. */
export const PAYOUT_DELAY_HOURS = 2

export const INCENTIVE_TYPE_KEY: Record<IncentiveType, TranslationKey> = {
  daily: 'icTypeDaily',
  weekly: 'icTypeWeekly',
  zone_quest: 'icTypeZoneQuest',
  one_time: 'icTypeOneTime',
}

export const INCENTIVE_TYPE_HINT_KEY: Record<IncentiveType, TranslationKey> = {
  daily: 'icTypeDailyHint',
  weekly: 'icTypeWeeklyHint',
  zone_quest: 'icTypeZoneQuestHint',
  one_time: 'icTypeOneTimeHint',
}

export function incentiveStatusOf(incentive: Pick<Incentive, 'isActive' | 'startsAt' | 'endsAt'>, now = Date.now()): IncentiveListStatus {
  if (!incentive.isActive) return 'inactive'
  const start = new Date(incentive.startsAt).getTime()
  const end = new Date(incentive.endsAt).getTime()
  if (Number.isFinite(end) && end <= now) return 'ended'
  if (Number.isFinite(start) && start > now) return 'upcoming'
  return 'active'
}

/** Progress still editable (void allowed only before payout). */
export function canVoidProgress(status: IncentiveProgressStatus) {
  return status === 'in_progress' || status === 'achieved'
}

export type IncentiveErrorField =
  | 'nameAr'
  | 'nameEn'
  | 'cityId'
  | 'targetTrips'
  | 'rewardAmount'
  | 'minTripFare'
  | 'startsAt'
  | 'endsAt'
  | 'dailyFrom'
  | 'dailyTo'
  | 'minRating'
  | 'maxParticipants'
  | 'budgetAmount'
  | 'zoneIds'

const TIME = /^([01]\d|2[0-3]):[0-5]\d$/

export function validateIncentive(input: IncentiveInput): Partial<Record<IncentiveErrorField, TranslationKey>> {
  const errors: Partial<Record<IncentiveErrorField, TranslationKey>> = {}
  if (!input.nameAr.trim()) errors.nameAr = 'fieldRequired'
  if (!input.nameEn.trim()) errors.nameEn = 'fieldRequired'
  if (!input.cityId) errors.cityId = 'fieldRequired'
  if (!Number.isInteger(input.targetTrips) || input.targetTrips < 1) errors.targetTrips = 'invalidNumber'
  if (!Number.isFinite(input.rewardAmount) || input.rewardAmount <= 0) errors.rewardAmount = 'invalidNumber'
  if (badOptional(input.minTripFare)) errors.minTripFare = 'invalidNumber'
  if (!input.startsAt) errors.startsAt = 'fieldRequired'
  if (!input.endsAt) errors.endsAt = 'fieldRequired'
  else if (input.startsAt && new Date(input.endsAt) <= new Date(input.startsAt)) errors.endsAt = 'endAfterStart'
  if (input.dailyFrom !== null && !TIME.test(input.dailyFrom)) errors.dailyFrom = 'invalidTime'
  if (input.dailyTo !== null && !TIME.test(input.dailyTo)) errors.dailyTo = 'invalidTime'
  if ((input.dailyFrom === null) !== (input.dailyTo === null)) errors.dailyTo = 'icErrWindowPair'
  if (input.minRating !== null && (!Number.isFinite(input.minRating) || input.minRating < 1 || input.minRating > 5)) errors.minRating = 'tierErrRating'
  if (badInt(input.maxParticipants, 1)) errors.maxParticipants = 'invalidNumber'
  if (badOptional(input.budgetAmount, 0.01)) errors.budgetAmount = 'invalidNumber'
  if (input.type === 'zone_quest' && !(input.zoneIds && input.zoneIds.length > 0)) errors.zoneIds = 'icErrZoneQuestZones'
  return errors
}

// ---------------------------------------------------------------------------
// Shared helpers
// ---------------------------------------------------------------------------

/** Optional numeric input → number | null ('' → null, invalid → NaN so validation can flag it). */
export function optionalNumber(value: string): number | null {
  const trimmed = value.trim()
  return trimmed === '' ? null : Number(trimmed)
}

/** Empty selections mean "no restriction" and are sent as `null`. */
export function listOrNull<T>(values: T[]): T[] | null {
  return values.length > 0 ? values : null
}

const ratioFormatter = new Intl.NumberFormat('en-US', { maximumFractionDigits: 1 })

/** 0..1 ratio → `82%` (one decimal max). */
export function formatRatio(value: number | null | undefined) {
  return typeof value === 'number' && Number.isFinite(value) ? `${ratioFormatter.format(value * 100)}%` : '—'
}

/** Used/limit as a 0..1 share for progress bars; null when unlimited. */
export function usageShare(used: number, limit: number | null | undefined) {
  if (typeof limit !== 'number' || limit <= 0) return null
  return Math.min(1, Math.max(0, used / limit))
}
