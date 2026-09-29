/** Shapes mirror docs/05-api-contract.md (camelCase JSON). */

export interface Paginated<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface ApiErrorBody {
  error: {
    code: string
    message: string
    details?: Record<string, unknown>
  }
}

export type UserStatus = 'active' | 'suspended' | 'deleted'
export type Gender = 'unknown' | 'male' | 'female'

export interface User {
  id: string
  phoneNumber: string | null
  fullName: string | null
  language: 'ar' | 'en'
  gender: Gender
  roles: string[]
  status?: UserStatus
  termsAcceptedAt: string | null
  createdAt: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresIn: number
  refreshToken: string
  refreshTokenExpiresAt: string
  isNewUser: boolean
  user: User
  permissions?: string[]
}

export interface DashboardSummary {
  pendingDriverApplications: number
  approvedDrivers: number
  onlineDrivers: number
  passengers: number
  tripsToday: number
  usersToday: number
  /** Added by F21 (docs/12 §summary); rendered only when the backend provides it. */
  today?: { completedTrips?: number; gmv?: number } | null
}

export type DriverStatus =
  | 'draft'
  | 'submitted'
  | 'under_review'
  | 'approved'
  | 'rejected'
  | 'suspended'

export interface DriverListItem {
  id: string
  applicationNumber: string
  fullName: string | null
  phoneNumber: string | null
  status: DriverStatus
  cityName: string | null
  vehicle: string | null
  submittedAt: string | null
  documentsPending: number
  /** F15 — present for approved drivers once tiers are computed. */
  tier?: DriverTier | null
  ratingAvg?: number | null
  ratingCount?: number | null
}

export type DocumentStatus = 'pending' | 'verified' | 'rejected' | 'expired'

export interface DriverDocument {
  id: string
  documentTypeId: string
  documentTypeCode: string
  documentTypeName: string
  status: DocumentStatus
  expiresAt: string | null
  reviewNote: string | null
  fileId: string
  fileName: string
  uploadedAt: string
}

export interface RequiredDocument {
  documentTypeId: string
  code: string
  name: string
  appliesTo: 'driver' | 'vehicle'
  isRequired: boolean
  requiresExpiry: boolean
  uploaded: boolean
}

export interface DriverProfile {
  fullName: string | null
  nationalId: string | null
  dateOfBirth: string | null
  cityId: string | null
  cityName?: string | null
  gender: Gender
  iban: string | null
}

export interface DriverVehicle {
  id: string
  make: string
  model: string
  year: number
  color: string
  plateNumber: string
  seats: number
  rideCategoryId: string
  rideCategoryName?: string | null
}

/** One entry of the driver's status history, derived from audit_logs on the server. */
export interface StatusHistoryEntry {
  id: string
  action: string
  fromStatus: DriverStatus | null
  toStatus: DriverStatus | null
  actorName: string | null
  reason: string | null
  createdAt: string
}

export interface DriverDetail {
  id: string
  applicationNumber: string
  status: DriverStatus
  rejectionReason: string | null
  submittedAt: string | null
  approvedAt: string | null
  profile: DriverProfile
  vehicle: DriverVehicle | null
  documents: DriverDocument[]
  requiredDocuments: RequiredDocument[]
  user: User
  statusHistory: StatusHistoryEntry[]
  /** F15 — `drivers.tier` / `rating_avg` / `rating_count`. */
  tier?: DriverTier | null
  ratingAvg?: number | null
  ratingCount?: number | null
}

export type DriverReviewAction = 'start_review' | 'approve' | 'reject' | 'suspend' | 'reinstate'

export interface PassengerListItem {
  id: string
  /** Present when the passenger row is keyed separately from its user. */
  userId?: string
  fullName: string | null
  phoneNumber: string
  status: UserStatus
  createdAt: string
  tripsCount: number
  /** F15 — `passengers.rating_avg` / `rating_count`. */
  ratingAvg?: number | null
  ratingCount?: number | null
}

export interface RideCategory {
  id: string
  code: string
  nameAr: string
  nameEn: string
  descriptionAr: string | null
  descriptionEn: string | null
  icon: string | null
  seats: number
  maxStops: number
  sortOrder: number
  isActive: boolean
}

export type RideCategoryInput = Omit<RideCategory, 'id'>

export interface AuditLog {
  id: string
  actorUserId: string | null
  actorName?: string | null
  actorRole: string | null
  action: string
  entityType: string
  entityId: string | null
  before: unknown
  after: unknown
  ipAddress: string | null
  createdAt: string
}

// ---------------------------------------------------------------------------
// F8 — trips (docs/06-feature-f8-trip-lifecycle.md, "كائن Trip" + "الإدارة")
// ---------------------------------------------------------------------------

export type TripStatus =
  | 'requested'
  | 'searching'
  | 'driver_assigned'
  | 'driver_en_route'
  | 'driver_arrived'
  | 'waiting'
  | 'pin_verified'
  | 'in_trip'
  | 'completed'
  | 'cancelled'
  | 'no_drivers'

export type BookingType = 'now' | 'scheduled'
export type PaymentMethod = 'cash' | 'wallet' | 'card'
export type PricingMode = 'fixed' | 'saver' | 'offer'
export type TripActor = 'passenger' | 'driver' | 'system' | 'admin'

export interface TripPoint {
  name: string | null
  address: string | null
  lat: number
  lng: number
}

export interface TripStop extends TripPoint {
  sequence?: number
  arrivedAt?: string | null
}

export interface TripListItem {
  id: string
  tripNumber: string
  status: TripStatus
  passengerName: string | null
  passengerPhone?: string | null
  driverName: string | null
  categoryName: string | null
  pickupName: string | null
  dropoffName: string | null
  estimatedFare: number | null
  finalFare: number | null
  paymentMethod?: PaymentMethod
  requestedAt: string
}

export interface TripPassenger {
  id: string
  fullName: string | null
  phoneNumber: string | null
}

export interface TripDriver {
  id: string
  fullName: string | null
  ratingAvg: number | null
  photoFileId: string | null
  phoneMasked: string | null
  /** Admin responses may carry the full number instead of the masked one. */
  phoneNumber?: string | null
  gender?: Gender
}

export interface TripVehicle {
  make: string
  model: string
  color: string
  plateNumber: string
}

export interface TripTimeline {
  requestedAt: string | null
  assignedAt: string | null
  arrivedAt: string | null
  startedAt: string | null
  completedAt: string | null
  cancelledAt: string | null
}

export interface TripEvent {
  id?: string
  type: string
  actor: TripActor
  actorName?: string | null
  actorUserId?: string | null
  lat?: number | null
  lng?: number | null
  data?: unknown
  createdAt: string
}

export interface TripDetail {
  id: string
  tripNumber: string
  status: TripStatus
  bookingType: BookingType
  scheduledAt: string | null
  rideCategory: { id: string; code: string; name: string } | null
  pickup: TripPoint
  dropoff: TripPoint
  stops: TripStop[]
  paymentMethod: PaymentMethod
  pricingMode: PricingMode
  offeredPrice: number | null
  estimatedFare: number | null
  finalFare: number | null
  estimatedDistanceMeters: number | null
  estimatedDurationSeconds: number | null
  finalDistanceMeters?: number | null
  finalDurationSeconds?: number | null
  preferFemaleDriver?: boolean
  riderNote?: string | null
  passenger?: TripPassenger | null
  driver: TripDriver | null
  vehicle: TripVehicle | null
  waitingSeconds: number | null
  cancelledBy: TripActor | null
  cancellationReason: string | null
  timeline: TripTimeline
  events: TripEvent[]
  /** F11 — card payment attached to the trip (null for cash/wallet trips). */
  payment?: TripPayment | null
  discountTotal?: number
  /** F14 — cancellation event summary (docs/09 §F14.4 "Trip يضاف إليه"); null when not cancelled. */
  cancellation?: TripCancellation | null
  /** F12 — planned route `[[lat,lng],…]` used by route-deviation detection (§F12.1). */
  plannedRoute?: LatLngTuple[] | null
  /** F15 — promo reservation, stored breakdown and (assumed) both ratings. */
  promotion?: TripPromotionInfo | null
  fareBreakdown?: { discount?: number | null; discounts?: TripDiscountLine[] | null } | null
  discounts?: TripDiscountLine[] | null
  ratings?: TripRatingInfo[] | null
}

export type LiveDriverStatus = 'idle' | 'on_trip'

export interface LiveDriver {
  driverId: string
  name: string | null
  lat: number
  lng: number
  isOnline: boolean
  status: LiveDriverStatus
  categoryCode: string | null
  /** Present when the driver is currently assigned to a trip. */
  currentTripId?: string | null
  heading?: number | null
  updatedAt?: string | null
}

export interface LiveTrip {
  id: string
  tripNumber?: string
  status: TripStatus
  pickup: TripPoint
  dropoff: TripPoint
  driverId: string | null
  passengerName?: string | null
  requestedAt?: string | null
}

export interface LiveSnapshot {
  drivers: LiveDriver[]
  activeTrips: LiveTrip[]
  searchingTrips: LiveTrip[]
  generatedAt?: string
}

// ---------------------------------------------------------------------------
// F10 — zones, pricing, demand (docs/07-feature-f9-f10-matching-pricing.md)
// ---------------------------------------------------------------------------

/** `[lat, lng]` pair as stored in `zones.polygon`. */
export type LatLngTuple = [number, number]

export interface OperatingHour {
  /** 0 = Sunday … 6 = Saturday. */
  day: number
  /** "HH:mm" */
  from: string
  /** "HH:mm" */
  to: string
}

export interface ZoneCategorySetting {
  rideCategoryId: string
  isEnabled: boolean
  surgeCap: number
}

export interface Zone {
  id: string
  cityId: string | null
  code: string
  nameAr: string
  nameEn: string
  polygon: LatLngTuple[]
  centerLat: number
  centerLng: number
  priority: number
  isActive: boolean
  /** null = always open. */
  operatingHours: OperatingHour[] | null
  zoneCategorySettings: ZoneCategorySetting[]
  createdAt?: string
  updatedAt?: string
}

export type ZoneInput = Omit<Zone, 'id' | 'createdAt' | 'updatedAt'>

export interface TimeMultiplier {
  id?: string
  /** null = every day. */
  dayOfWeek: number | null
  /** "HH:mm" */
  fromTime: string
  /** "HH:mm" */
  toTime: string
  multiplier: number
  label: string
}

export interface PricingRule {
  id: string
  rideCategoryId: string
  /** null = whole city. */
  zoneId: string | null
  name: string
  baseFare: number
  perKm: number
  perMinute: number
  bookingFee: number
  serviceFeePercent: number
  minFare: number
  waitingPerMinute: number
  freeWaitingMinutes: number
  cancellationFee: number
  driverSharePercent: number
  effectiveFrom: string
  effectiveTo: string | null
  priority: number
  isActive: boolean
  timeMultipliers: TimeMultiplier[]
}

export type PricingRuleInput = Omit<PricingRule, 'id'>

export interface GeoPoint {
  lat: number
  lng: number
}

export interface SimulateRequest {
  pickup: GeoPoint
  dropoff: GeoPoint
  stops: GeoPoint[]
  rideCategoryId?: string
  /** ISO datetime used instead of "now" when evaluating time multipliers and demand. */
  at?: string
}

export interface DemandLevelSummary {
  code: string
  name: string
  multiplier: number
  /** CSS colour supplied by the API (e.g. `#c23b4a`). */
  color: string
}

export interface FareBreakdown {
  baseFare: number
  distanceFare: number
  timeFare: number
  minFareApplied: boolean
  timeMultiplier: number
  demandMultiplier: number
  bookingFee: number
  serviceFee: number
  discount: number
}

export interface SimulatedCategory {
  rideCategoryId: string
  code: string
  name: string
  etaMinutes: number | null
  total: number
  driverNetEarnings: number
  offerMin: number
  offerMax: number
  breakdown: FareBreakdown
}

export interface SimulateResult {
  distanceMeters: number
  durationSeconds: number
  pickupZone: { id: string | null; name: string | null } | null
  demand: DemandLevelSummary | null
  categories: SimulatedCategory[]
}

export interface DemandLevel {
  id: string
  code: string
  nameAr: string
  nameEn: string
  multiplier: number
  color: string
  sortOrder: number
}

export type DemandMetric = 'requests_per_driver'

export interface DemandRule {
  id: string
  zoneId: string | null
  rideCategoryId: string | null
  metric: DemandMetric
  windowMinutes: number
  thresholdModerate: number
  thresholdHigh: number
  thresholdVeryHigh: number
  isActive: boolean
}

export type DemandRuleInput = Omit<DemandRule, 'id'>

export interface DemandOverride {
  id: string
  zoneId: string
  zoneName?: string | null
  rideCategoryId: string | null
  demandLevelId: string
  reason: string
  startsAt: string
  endsAt: string
  createdBy: string | null
  createdByName?: string | null
}

export type DemandOverrideInput = Omit<DemandOverride, 'id' | 'createdBy' | 'createdByName' | 'zoneName'>

export type DemandSource = 'override' | 'snapshot' | 'default'

export interface CurrentDemand {
  zoneId: string
  zoneName: string
  rideCategoryId: string | null
  level: DemandLevelSummary
  requestsCount: number
  onlineDrivers: number
  ratio: number | null
  computedAt: string | null
  source: DemandSource
}

// ---------------------------------------------------------------------------
// F9 — matching
// ---------------------------------------------------------------------------

export interface MatchingWeights {
  distance: number
  eta: number
  rating: number
  acceptance: number
  cancellation: number
  tier: number
  favorite: number
}

export interface MatchingSettings {
  id: string
  zoneId: string | null
  rideCategoryId: string | null
  radiusMeters: number
  maxRadiusMeters: number
  radiusStepMeters: number
  offerTimeoutSeconds: number
  searchTimeoutSeconds: number
  maxCandidates: number
  weights: MatchingWeights
  allowCategoryUpgrade: boolean
  preferFavoriteDriver: boolean
  isActive: boolean
}

export type MatchingSettingsInput = Omit<MatchingSettings, 'id'>

export type MatchingOutcome = 'assigned' | 'exhausted' | 'timeout' | 'cancelled' | 'in_progress'
export type CandidateResponse = 'accepted' | 'rejected' | 'expired' | null

export interface MatchingCandidate {
  driverId: string
  driverName: string | null
  distanceMeters: number
  etaSeconds: number
  /** 0..1, higher is better. */
  score: number
  rank: number
  offered: boolean
  response: CandidateResponse
}

export interface MatchingAttempt {
  id: string
  round: number
  radiusMeters: number
  candidatesCount: number
  startedAt: string
  finishedAt: string | null
  outcome: MatchingOutcome | null
  candidates: MatchingCandidate[]
}

export interface TripMatching {
  attempts: MatchingAttempt[]
}

export interface MatchingStats {
  tripsRequested: number
  assigned: number
  noDrivers: number
  averageAssignSeconds: number | null
  /** 0..1 */
  offerAcceptanceRate: number | null
  averageRounds: number | null
}

// ---------------------------------------------------------------------------
// F11 — payments, refunds, payouts, settlements, wallets, ledger
// (docs/08-feature-f11-f13-payments-notifications.md §F11.2 / §F11.5)
// ---------------------------------------------------------------------------

export type PaymentStatus = 'initiated' | 'authorized' | 'captured' | 'failed' | 'voided' | 'refunded' | 'partially_refunded'
export type PaymentPurpose = 'trip' | 'topup' | 'cancellation_fee'
export type PaymentChannel = 'card' | 'apple_pay' | 'sandbox'
export type PaymentProvider = 'sandbox' | 'moyasar'
export type CardBrand = 'mada' | 'visa' | 'mastercard'

export interface PaymentAction {
  type: 'redirect'
  url: string
  expiresAt?: string | null
}

/** `Trip.payment` (§F11.5 "الراكب"). */
export interface TripPayment {
  id: string
  status: PaymentStatus
  method: PaymentChannel
  brand?: CardBrand | string | null
  last4?: string | null
  authorizedAmount: number | null
  capturedAmount: number | null
  action?: PaymentAction | null
}

export interface PaymentListItem {
  id: string
  purpose: PaymentPurpose
  status: PaymentStatus
  method: PaymentChannel
  provider: PaymentProvider | string
  amount: number
  capturedAmount: number | null
  refundedAmount: number
  userName: string | null
  userPhone: string | null
  tripNumber: string | null
  gatewayPaymentId: string | null
  createdAt: string
}

export type WebhookProcessingStatus = 'pending' | 'processed' | 'ignored' | 'failed'

export interface PaymentWebhookEvent {
  id: string
  provider: string
  eventId: string
  eventType: string
  gatewayPaymentId: string | null
  signatureValid: boolean
  payload?: unknown
  processingStatus: WebhookProcessingStatus
  error: string | null
  receivedAt: string
  processedAt: string | null
}

/** One ledger line tied to the payment (either a wallet transaction or a journal). */
export interface LedgerLine {
  id: string
  account: string
  debit: number
  credit: number
  transactionId?: string | null
  journalId?: string | null
  /** Wallet transaction type or journal type (§F11.3). */
  type?: string | null
  description?: string | null
  createdAt: string
}

export type RefundStatus = 'pending_approval' | 'approved' | 'processing' | 'succeeded' | 'failed' | 'rejected'
export type RefundDestination = 'original_method' | 'wallet'
export type RefundReasonCode =
  | 'fare_dispute'
  | 'trip_not_taken'
  | 'duplicate_charge'
  | 'service_issue'
  | 'cancellation_fee_waived'
  | 'goodwill'
  | 'other'

export interface Refund {
  id: string
  refundNumber: string
  paymentId: string | null
  tripId: string | null
  tripNumber?: string | null
  userId: string
  userName?: string | null
  userPhone?: string | null
  amount: number
  type: 'full' | 'partial'
  destination: RefundDestination
  reasonCode: RefundReasonCode
  reason: string | null
  status: RefundStatus
  requestedBy: string
  requestedByName?: string | null
  approvedBy: string | null
  approvedByName?: string | null
  approvedAt: string | null
  rejectedBy?: string | null
  rejectedByName?: string | null
  rejectedReason: string | null
  gatewayRefundId?: string | null
  failureMessage: string | null
  processedAt: string | null
  createdAt: string
}

export interface RefundInput {
  amount: number
  reasonCode: RefundReasonCode
  reason: string
  destination?: RefundDestination
}

/** GET /admin/trips/{id}/receipt (§F11.6). */
export interface ReceiptLine {
  code: string
  label: string
  amount: number
  source?: string | null
  reference?: string | null
}

export interface Receipt {
  tripId: string
  tripNumber: string
  status: string
  issuedAt: string
  currency: string
  lines: ReceiptLine[]
  subtotal: number
  discountTotal: number
  total: number
  vatRate: number
  vatIncluded: number
  payment: {
    method: PaymentMethod | string
    brand?: string | null
    last4?: string | null
    status?: PaymentStatus | string | null
    paidAmount: number | null
    fallbackToCash: boolean
  } | null
  refunds: { id: string; amount: number; status: RefundStatus; destination: RefundDestination; createdAt: string }[]
  netPaid: number
}

export interface PaymentDetail extends PaymentListItem {
  userId: string
  tripId: string | null
  walletId: string | null
  paymentMethodId: string | null
  currency: string
  authorizedAmount: number | null
  captureMode: 'manual' | 'auto'
  gatewayStatus: string | null
  card?: { brand: CardBrand | string; last4: string } | null
  actionUrl: string | null
  actionExpiresAt: string | null
  failureCode: string | null
  failureMessage: string | null
  idempotencyKey: string | null
  authorizedAt: string | null
  capturedAt: string | null
  failedAt: string | null
  voidedAt: string | null
  updatedAt?: string | null
  metadata?: unknown
  webhookEvents: PaymentWebhookEvent[]
  refunds: Refund[]
  ledger: LedgerLine[]
}

export type PayoutStatus = 'requested' | 'approved' | 'paid' | 'rejected' | 'cancelled'

export interface Payout {
  id: string
  payoutNumber: string
  driverId?: string
  driverName?: string | null
  driverPhone?: string | null
  batchId?: string | null
  batchNumber?: string | null
  amount: number
  ibanMasked: string | null
  accountHolderName?: string | null
  status: PayoutStatus
  requestedAt: string
  approvedAt: string | null
  paidAt: string | null
  rejectedReason: string | null
  bankReference: string | null
}

export type PayoutBatchStatus = 'open' | 'exported' | 'paid'

export interface PayoutBatch {
  id: string
  batchNumber: string
  status: PayoutBatchStatus
  payoutsCount: number
  totalAmount: number
  exportedAt: string | null
  exportedByName?: string | null
  bankReference: string | null
  paidAt: string | null
  paidByName?: string | null
  createdByName?: string | null
  createdAt: string
  /** Present on GET /admin/payout-batches/{id}. */
  payouts?: Payout[]
}

export type SettlementBatchStatus = 'generating' | 'ready' | 'finalized' | 'failed'
export type SettlementDirection = 'payable_to_driver' | 'due_from_driver' | 'zero'

export interface SettlementBatch {
  id: string
  batchNumber: string
  cityId: string | null
  cityName?: string | null
  periodStart: string
  periodEnd: string
  status: SettlementBatchStatus
  driversCount: number
  totalTrips: number
  totalGrossFares: number
  totalEarnings: number
  totalCommission: number
  totalCashCollected: number
  totalIncentives: number
  totalCompensation: number
  totalAdjustments: number
  totalNet: number
  error: string | null
  generatedByName?: string | null
  generatedAt: string | null
  finalizedByName?: string | null
  finalizedAt: string | null
  createdAt: string
}

export interface SettlementBatchInput {
  periodStart: string
  periodEnd: string
  cityId?: string
}

export interface Settlement {
  id: string
  batchId: string
  driverId: string
  driverName: string | null
  driverPhone?: string | null
  tripsCount: number
  grossFares: number
  earnings: number
  commission: number
  cashCollected: number
  incentives: number
  cancellationCompensation: number
  adjustments: number
  fees: number
  topups: number
  payoutsInPeriod: number
  netAmount: number
  openingBalance: number
  closingBalance: number
  direction: SettlementDirection
  payoutId: string | null
  status: 'open' | 'finalized'
  createdAt: string
}

export type WalletKind = 'passenger' | 'driver'
export type WalletStatus = 'active' | 'frozen'
export type WalletTransactionType =
  | 'topup'
  | 'trip_payment'
  | 'trip_earning'
  | 'refund'
  | 'payout'
  | 'payout_reversal'
  | 'adjustment'
  | 'incentive'
  | 'cancellation_fee'
  | 'cancellation_compensation'
  | 'cash_collection'

export interface WalletListItem {
  id: string
  userId: string
  userName: string | null
  phone: string | null
  kind: WalletKind
  balance: number
  status: WalletStatus | string
}

export interface WalletTransaction {
  id: string
  type: WalletTransactionType | string
  direction: 'credit' | 'debit'
  amount: number
  balanceAfter: number
  referenceType?: string | null
  referenceId?: string | null
  description: string | null
  createdByName?: string | null
  createdAt: string
}

export interface WalletDetail extends WalletListItem {
  currency?: string
  /** Driver wallets: max(0, −balance). */
  cashDebt?: number
  createdAt?: string
  transactions: WalletTransaction[]
}

export interface WalletAdjustmentInput {
  direction: 'credit' | 'debit'
  amount: number
  reason: string
}

export interface LedgerBalance {
  account: string
  debit: number
  credit: number
  balance: number
}

// ---------------------------------------------------------------------------
// F13 — notification templates, campaigns, deliveries (§F13.3 / §F13.6)
// ---------------------------------------------------------------------------

export type NotificationChannel = 'push' | 'sms' | 'inapp'
export type NotificationCategory = 'trips' | 'offers' | 'safety' | 'wallet' | 'promotions' | 'system'

export interface NotificationEvent {
  code: string
  category: NotificationCategory | string
  isCritical: boolean
  recipients: string[] | string
  allowedChannels: NotificationChannel[]
  placeholders: string[]
  deepLink: string | null
}

export interface NotificationTemplate {
  id: string
  code: string
  channel: NotificationChannel
  titleAr: string | null
  titleEn: string | null
  bodyAr: string
  bodyEn: string
  isActive: boolean
  updatedAt: string | null
  updatedByName: string | null
}

export interface NotificationTemplateInput {
  code: string
  channel: NotificationChannel
  titleAr: string | null
  titleEn: string | null
  bodyAr: string
  bodyEn: string
  isActive: boolean
}

export interface TemplatePreview {
  title: string | null
  body: string
  length: number
  smsSegments: number
}

export type CampaignStatus = 'draft' | 'scheduled' | 'sending' | 'sent' | 'cancelled' | 'failed'
export type CampaignCategory = 'promotions' | 'system' | 'trips' | 'wallet'
export type CampaignChannel = 'inapp' | 'push' | 'sms'

export interface CampaignAudience {
  roles?: ('passenger' | 'driver')[]
  cityIds?: string[]
  languages?: ('ar' | 'en')[]
  genders?: ('male' | 'female')[]
  driverTiers?: string[]
  lastActiveWithinDays?: number
  hasCompletedTrip?: boolean
  userIds?: string[]
}

export interface Campaign {
  id: string
  name: string
  category: CampaignCategory
  channels: CampaignChannel[]
  audience: CampaignAudience
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  deepLink: string | null
  status: CampaignStatus
  scheduledAt: string | null
  startedAt: string | null
  completedAt: string | null
  targetCount: number
  inappCreated: number
  pushSent: number
  pushFailed: number
  pushSkipped: number
  smsSent: number
  smsFailed: number
  openedCount: number
  createdByName?: string | null
  createdAt: string
  updatedAt?: string | null
}

export interface CampaignInput {
  name: string
  category: CampaignCategory
  channels: CampaignChannel[]
  audience: CampaignAudience
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  deepLink: string | null
}

export interface AudiencePreview {
  count: number
  sample: { userId: string; name: string | null; phoneMasked: string | null }[]
}

export type DeliveryStatus = 'queued' | 'sent' | 'failed' | 'skipped'
export type DeliverySkippedReason = 'preference_off' | 'no_subscription' | 'template_inactive' | 'no_phone' | 'user_inactive'

export interface NotificationDelivery {
  id: string
  eventCode: string
  channel: 'push' | 'sms'
  status: DeliveryStatus
  skippedReason: DeliverySkippedReason | string | null
  userId?: string | null
  userName: string | null
  phoneMasked: string | null
  campaignId?: string | null
  provider: string | null
  providerMessageId: string | null
  errorCode: string | null
  errorMessage: string | null
  attempts: number
  sentAt: string | null
  openedAt: string | null
  createdAt: string
  payload?: unknown
}

// ---------------------------------------------------------------------------
// F12 — safety (docs/09-feature-f12-f14-safety-cancellation.md §F12.1 / §F12.7)
// ---------------------------------------------------------------------------

export type SafetyCaseType = 'sos' | 'unexpected_stop' | 'route_deviation' | 'trip_overrun' | 'safety_report'
export type SafetyCaseSource = 'rider_sos' | 'driver_sos' | 'alert' | 'report' | 'support' | 'admin'
export type SafetyPriority = 'critical' | 'high' | 'medium' | 'low'
export type SafetyCaseStatus = 'open' | 'in_progress' | 'escalated' | 'resolved'
export type SafetyReporterRole = 'passenger' | 'driver' | 'system' | 'admin'
export type SafetyEscalationTarget = 'police' | 'ambulance' | 'civil_defense' | 'management' | 'other'
export type SafetyResolutionCode =
  | 'false_alarm'
  | 'resolved_contacted'
  | 'escalated_authorities'
  | 'action_taken_driver'
  | 'action_taken_passenger'
  | 'no_action'
  | 'other'
export type SafetyNoteKind = 'note' | 'status_change' | 'assignment' | 'contact_attempt' | 'system'
export type SafetyReportCategory = 'unsafe_driving' | 'harassment' | 'vehicle_mismatch' | 'driver_mismatch' | 'passenger_misconduct' | 'other'

/** `GET /admin/safety/summary`. */
export interface SafetySummary {
  open: Record<SafetyPriority, number>
  unassigned: number
  avgFirstResponseSeconds: number | null
  pendingAlerts: number
  onDutyAgents: number
}

/** Row of `GET /admin/safety/cases`; also the payload of `SafetyCaseOpened` / `SafetyCaseUpdated`. */
export interface SafetyCaseListItem {
  id: string
  caseNumber: string
  type: SafetyCaseType
  source: SafetyCaseSource
  priority: SafetyPriority
  status: SafetyCaseStatus
  /** Not listed in §F12.7 but present on the hub payload; used for links when available. */
  tripId?: string | null
  tripNumber: string | null
  reporterName: string | null
  reporterRole: SafetyReporterRole
  assignedToName: string | null
  openedAt: string
  firstResponseAt: string | null
  /** Age at response time; the UI keeps it ticking from the moment it was received. */
  ageSeconds: number
  lastLat?: number | null
  lastLng?: number | null
}

export interface SafetyCaseNote {
  id: string
  kind: SafetyNoteKind
  body: string
  isInternal: boolean
  authorUserId: string | null
  authorName?: string | null
  createdAt: string
}

export interface SafetyCaseAttachment {
  id: string
  fileId: string
  fileName?: string | null
  uploadedByName?: string | null
  createdAt: string
}

export interface SafetyParty {
  id: string
  userId?: string | null
  fullName: string | null
  /** Admin case detail returns the full number (§F12.7 "الأطراف بالجوال الكامل"). */
  phoneNumber: string | null
}

export interface SafetyCaseTrip {
  id: string
  tripNumber: string
  status: TripStatus
  pickup: TripPoint
  dropoff: TripPoint
  stops?: TripStop[]
  plannedRoute?: LatLngTuple[] | null
  rideCategory?: { id: string; code: string; name: string } | null
  passenger: SafetyParty | null
  driver: SafetyParty | null
  vehicle?: TripVehicle | null
  startedAt?: string | null
}

export interface SafetyLiveLocation {
  lat: number
  lng: number
  heading?: number | null
  updatedAt?: string | null
  /** Which party the point belongs to, when the server says so. */
  source?: 'reporter' | 'driver' | string | null
}

export interface NotifiedContact {
  name: string
  phoneNumber?: string | null
}

export interface SafetyCaseDetail extends SafetyCaseListItem {
  reporterUserId: string | null
  subjectUserId: string | null
  subjectName?: string | null
  reportCategory: SafetyReportCategory | null
  description: string | null
  lat: number | null
  lng: number | null
  lastLat: number | null
  lastLng: number | null
  lastLocationAt: string | null
  contactsNotified: number
  assignedToUserId: string | null
  assignedAt: string | null
  escalatedTo: SafetyEscalationTarget | null
  resolutionCode: SafetyResolutionCode | null
  resolution: string | null
  reporterCancelledAt: string | null
  supportTicketId: string | null
  resolvedAt: string | null
  trip: SafetyCaseTrip | null
  liveLocation: SafetyLiveLocation | null
  alerts: SafetyAlert[]
  notes: SafetyCaseNote[]
  attachments: SafetyCaseAttachment[]
  sharesCount: number
  /** Count or list of the reporter's trusted contacts that were notified (shape not pinned by §F12.7). */
  trustedContactsNotified: number | NotifiedContact[] | null
}

export interface SafetyCaseCreateInput {
  tripId?: string
  type: 'safety_report'
  priority: SafetyPriority
  description: string
  subjectUserId?: string
}

export type SafetyAlertType = 'unexpected_stop' | 'route_deviation' | 'trip_overrun'
export type SafetyAlertStatus = 'pending_rider' | 'resolved_ok' | 'escalated' | 'no_response' | 'dismissed'

export interface SafetyAlertMetrics {
  stoppedSeconds?: number | null
  deviationMeters?: number | null
  deviationSeconds?: number | null
  elapsedSeconds?: number | null
  estimatedSeconds?: number | null
}

/** Row of `GET /admin/safety/alerts` and payload of `SafetyAlertRaised`. */
export interface SafetyAlert {
  id: string
  tripId: string
  tripNumber?: string | null
  type: SafetyAlertType
  status: SafetyAlertStatus
  detectedAt: string
  lat: number | null
  lng: number | null
  metrics: SafetyAlertMetrics | null
  promptedAt?: string | null
  respondBy: string | null
  respondedAt?: string | null
  response?: 'ok' | 'need_help' | null
  safetyCaseId?: string | null
  safetyCaseNumber?: string | null
  dismissedByName?: string | null
  createdAt?: string
}

export interface TrustedContact {
  id: string
  name: string
  phoneNumber: string
  relationship: string | null
  autoShare: boolean
  notifyOnSos: boolean
  createdAt: string
}

export type TripShareChannel = 'link' | 'sms' | 'auto'

export interface TripShare {
  id: string
  url: string
  channel: TripShareChannel
  trustedContactName: string | null
  viewCount: number
  expiresAt: string | null
  revokedAt: string | null
  lastViewedAt?: string | null
  createdAt: string
}

export type TripMessageSender = 'passenger' | 'driver' | 'system'
export type TripMessageKind = 'text' | 'quick_reply' | 'system'

export interface TripMessage {
  id: string
  tripId: string
  senderRole: TripMessageSender
  senderName?: string | null
  kind: TripMessageKind
  body: string
  quickReplyCode: string | null
  readAt: string | null
  createdAt: string
}

export type LostItemStatus = 'open' | 'driver_contacted' | 'found' | 'returned' | 'not_found' | 'closed'
export type LostItemCategory = 'phone' | 'wallet' | 'bag' | 'keys' | 'documents' | 'other'

export interface LostItemReport {
  id: string
  reportNumber: string
  tripId: string
  tripNumber: string | null
  reporterUserId?: string | null
  reporterName?: string | null
  reporterPhone?: string | null
  contactPhone: string | null
  driverId: string | null
  driverName?: string | null
  itemCategory: LostItemCategory
  description: string
  status: LostItemStatus
  driverResponse: 'found' | 'not_found' | null
  driverNote?: string | null
  driverRespondedAt?: string | null
  supportTicketId: string | null
  closedAt?: string | null
  createdAt: string
  updatedAt?: string
}

// ---------------------------------------------------------------------------
// F14 — cancellation & reliability (docs/09 §F14.2 / §F14.4)
// ---------------------------------------------------------------------------

export type CancellationStage = 'before_accept' | 'after_accept' | 'en_route' | 'arrived' | 'waiting' | 'no_show' | 'scheduled'
export type RuleStage = Exclude<CancellationStage, 'scheduled'>
export type ReasonActor = 'passenger' | 'driver' | 'system'
export type RuleActor = 'passenger' | 'driver'
export type AtFault = 'passenger' | 'driver' | 'none'
export type CancellationFeeType = 'none' | 'fixed' | 'percent' | 'pricing_rule'
export type CancellationFeeStatus = 'none' | 'charged' | 'pending_review' | 'waived' | 'failed' | 'refunded'
export type ExcuseStatus = 'not_applicable' | 'pending' | 'approved' | 'rejected'
export type ReliabilityRole = 'passenger' | 'driver'
export type RestrictionLevel = 'none' | 'warning' | 'matching_deprioritized' | 'incentives_reduced' | 'temporarily_restricted' | 'suspended'
export type ReliabilityAction = 'add_points' | 'remove_points' | 'set_level' | 'clear_restriction'

/** `Trip.cancellation` (§F14.4) plus the admin-only fields the event carries. */
export interface TripCancellation {
  id?: string | null
  eventId?: string | null
  actor?: TripActor | null
  stage: CancellationStage
  reasonCode: string
  reasonName: string | null
  note?: string | null
  atFault: AtFault
  fee?: number | null
  feeCharged?: number | null
  feeStatus: CancellationFeeStatus
  compensation?: number | null
  penaltyPoints?: number | null
  excuseStatus: ExcuseStatus
  reviewNote?: string | null
}

export interface CancellationReason {
  id: string
  code: string
  actor: ReasonActor
  nameAr: string
  nameEn: string
  /** null = every stage. */
  stages: CancellationStage[] | null
  isExcusable: boolean
  isEmergency: boolean
  requiresNote: boolean
  isSelectable: boolean
  sortOrder: number
  isActive: boolean
}

export type CancellationReasonInput = Omit<CancellationReason, 'id'>

export interface CancellationRule {
  id: string
  name: string
  actor: RuleActor
  stage: RuleStage
  bookingType: BookingType | null
  rideCategoryId: string | null
  zoneId: string | null
  freeWindowSeconds: number
  feeType: CancellationFeeType
  feeAmount: number | null
  feePercent: number | null
  minFee: number | null
  maxFee: number | null
  driverCompensationPercent: number
  penaltyPoints: number
  priority: number
  isActive: boolean
}

export type CancellationRuleInput = Omit<CancellationRule, 'id'>

export interface CancellationSimulateInput {
  actor: RuleActor
  stage: RuleStage
  bookingType: BookingType
  rideCategoryId?: string
  zoneId?: string
  secondsSinceAnchor: number
  estimatedFare: number
}

export interface CancellationSimulateResult {
  ruleId: string | null
  ruleName: string | null
  fee: number
  compensation: number
  penaltyPoints: number
  isFree: boolean
}

export interface ReliabilityThreshold {
  id: string
  role: ReliabilityRole
  level: Exclude<RestrictionLevel, 'none'>
  minPenaltyPoints: number | null
  /** 0..1 */
  minCancellationRate: number | null
  minTripsForRate: number
  restrictionHours: number | null
  deprioritizeFactor: number | null
  incentiveReductionPercent: number | null
  sortOrder: number
  isActive: boolean
}

export type ReliabilityThresholdInput = Omit<ReliabilityThreshold, 'id' | 'role' | 'level'>

/** Row of `GET /admin/cancellations`. */
export interface CancellationEvent {
  id: string
  tripId: string
  tripNumber: string
  actor: TripActor
  userId?: string | null
  userName: string | null
  atFault: AtFault
  stage: CancellationStage
  reasonCode: string
  reasonName: string | null
  note?: string | null
  feeAmount: number
  feeCharged: number
  feeStatus: CancellationFeeStatus
  compensationAmount: number
  penaltyPoints: number
  excuseStatus: ExcuseStatus
  reviewedByName?: string | null
  reviewedAt?: string | null
  reviewNote?: string | null
  createdAt: string
}

/** Row of `GET /admin/cancellations/excuses`. */
export interface ExcuseQueueItem extends CancellationEvent {
  ageHours: number
  slaBreached: boolean
  /** Penalty points the matched rule would apply when rejected (when the server exposes them). */
  pendingPenaltyPoints?: number | null
}

/** `GET /admin/cancellations/stats` — KPIs of §F14.7; every field optional so partial backends still render. */
export interface CancellationStats {
  /** 0..1 */
  passengerCancellationRate?: number | null
  /** 0..1 */
  driverCancellationRate?: number | null
  cancellationFeeRevenue?: number | null
  /** Alias some backends use for `cancellationFeeRevenue`. */
  feeRevenue?: number | null
  repeatCancellationRate?: number | null
  driverReliabilityRate?: number | null
  passengerReliabilityRate?: number | null
  excuseApprovalRate?: number | null
  noShowRate?: number | null
  totalCancellations?: number | null
}

export interface ReliabilityProfileListItem {
  userId: string
  name: string | null
  phone: string | null
  role: ReliabilityRole
  level: RestrictionLevel
  restrictedUntil: string | null
  cancellationRate: number
  reliabilityRate: number
  penaltyPoints: number
  noShowCount: number
  tripsAccepted: number
  lastComputedAt: string | null
}

export interface ReliabilityEvent {
  id?: string
  tripId: string
  tripNumber: string | null
  stage: CancellationStage
  reasonCode?: string | null
  reasonName: string | null
  atFault?: AtFault | null
  feeCharged: number | null
  penaltyPoints: number
  excuseStatus: ExcuseStatus
  countsTowardRate?: boolean | null
  createdAt: string
}

export interface ReliabilityAdjustment {
  id: string
  action: ReliabilityAction
  points: number | null
  level: RestrictionLevel | null
  until: string | null
  reason: string
  createdByName?: string | null
  createdAt: string
}

export interface ReliabilityNextLevel {
  level: RestrictionLevel
  minPenaltyPoints: number | null
  minCancellationRate: number | null
}

export interface ReliabilityProfileDetail extends ReliabilityProfileListItem {
  windowDays: number
  tripsRequested: number
  tripsCompleted: number
  cancellationsAtFault: number
  offersReceived?: number | null
  offersAccepted?: number | null
  acceptanceRate?: number | null
  levelChangedAt?: string | null
  nextLevel?: ReliabilityNextLevel | null
  effects?: { matchingFactor: number; incentiveMultiplier: number } | null
  events: ReliabilityEvent[]
  adjustments: ReliabilityAdjustment[]
}

export interface ReliabilityAdjustInput {
  role: ReliabilityRole
  action: ReliabilityAction
  points?: number
  level?: Exclude<RestrictionLevel, 'none'>
  until?: string
  reason: string
}

// ---------------------------------------------------------------------------
// F15 — ratings, promotions, driver tiers, incentives
// (docs/10 §F15). Fields marked "assumed" are not in the
// contract; they are optional so a backend without them still renders.
// ---------------------------------------------------------------------------

export type RaterRole = 'passenger' | 'driver'
export type RatingStatus = 'visible' | 'hidden'
export type RatingFlagType = 'low_rating' | 'low_average' | 'abusive_comment'
export type RatingFlagStatus = 'open' | 'dismissed' | 'actioned'
export type RatingFlagAction = 'warned' | 'suspension_review' | 'none'
export type RatingFlagReviewAction = 'dismiss' | 'warn' | 'suspension_review'

/** `GET /catalog/rating-tags?target=` item. */
export interface RatingTag {
  code: string
  name: string
}

/** Row of `GET /admin/ratings` (§F15.3). */
export interface AdminRating {
  id: string
  tripNumber: string
  raterName: string | null
  raterRole: RaterRole
  rateeName: string | null
  stars: number
  tags: string[]
  comment: string | null
  status: RatingStatus
  createdAt: string
  /** Assumed: links and audit details the list may carry. */
  tripId?: string | null
  raterUserId?: string | null
  rateeUserId?: string | null
  rateeRole?: RaterRole | null
  /** Assumed: comment auto-hidden by the abusive-words filter while the stars stay visible. */
  commentHidden?: boolean | null
  hiddenReason?: string | null
  hiddenByName?: string | null
  hiddenAt?: string | null
  flagged?: boolean | null
}

/** Row of `GET /admin/rating-flags` — columns of `rating_flags` (§F15.1) in camelCase. */
export interface RatingFlag {
  id: string
  userId: string
  role: RaterRole
  type: RatingFlagType
  ratingId: string | null
  /** Stars for `low_rating`, the average for `low_average`. */
  value: number | null
  status: RatingFlagStatus
  action: RatingFlagAction | null
  reviewedAt: string | null
  note: string | null
  createdAt: string
  /** Assumed display helpers. */
  userName?: string | null
  driverId?: string | null
  reviewedByName?: string | null
  ratingCount?: number | null
  rating?: { stars: number; comment: string | null; tags: string[]; tripNumber?: string | null; tripId?: string | null } | null
}

export type PromotionType = 'percent' | 'fixed' | 'free_booking_fee'
export type PromotionListStatus = 'active' | 'scheduled' | 'expired' | 'inactive'
export type RedemptionStatus = 'reserved' | 'applied' | 'released'
export type RedemptionReleaseReason = 'trip_cancelled' | 'no_drivers' | 'not_stacked' | 'payment_failed' | 'not_eligible_at_completion' | 'admin'

/** Row of `GET /admin/promotions` (§F15.6). */
export interface PromotionListItem {
  id: string
  code: string
  nameAr: string
  nameEn?: string | null
  type: PromotionType
  value: number
  validFrom: string
  validTo: string
  usageCount: number
  totalUsageLimit: number | null
  spentAmount: number
  budgetAmount: number | null
  isActive: boolean
}

/** Body of `POST/PUT /admin/promotions` — every `promotions` column except the counters. */
export interface PromotionInput {
  code: string
  nameAr: string
  nameEn: string
  descriptionAr: string | null
  descriptionEn: string | null
  type: PromotionType
  value: number
  maxDiscount: number | null
  minFare: number | null
  validFrom: string
  validTo: string
  totalUsageLimit: number | null
  perUserLimit: number
  budgetAmount: number | null
  firstTripOnly: boolean
  newUsersOnly: boolean
  newUserDays: number
  cityId: string | null
  rideCategoryIds: string[] | null
  zoneIds: string[] | null
  paymentMethods: PaymentMethod[] | null
  bookingTypes: BookingType[] | null
  isStackable: boolean
  isPublic: boolean
  isActive: boolean
}

/** `GET /admin/promotions/{id}`. */
export interface Promotion extends PromotionInput {
  id: string
  usageCount: number
  spentAmount: number
  createdAt?: string | null
  updatedAt?: string | null
  createdByName?: string | null
}

/** Row of `GET /admin/promotions/{id}/redemptions`. */
export interface PromotionRedemption {
  id: string
  passengerName: string | null
  phoneMasked: string | null
  tripNumber: string
  status: RedemptionStatus
  reservedAmount: number | null
  discountAmount: number | null
  reservedAt: string
  appliedAt: string | null
  releaseReason: RedemptionReleaseReason | null
  /** Assumed. */
  tripId?: string | null
  releasedAt?: string | null
  promotionCode?: string | null
}

/** `GET /admin/promotions/{id}/stats`. */
export interface PromotionStats {
  reserved: number
  applied: number
  released: number
  totalDiscount: number
  uniqueUsers: number
  firstTripConversions: number
}

export type DriverTier = 'bronze' | 'silver' | 'gold' | 'platinum'

/** Row of `GET /admin/driver-tier-rules` (§F15.7). Rates are 0..1. */
export interface DriverTierRule {
  id: string
  tier: DriverTier
  minCompletedTrips: number
  minRatingAvg: number
  minAcceptanceRate: number
  maxCancellationRate: number
  commissionDiscountPercent: number
  matchingNorm: number
  benefitsAr: string | null
  benefitsEn: string | null
  sortOrder: number
  updatedAt?: string | null
  /** Assumed: current number of drivers on the tier (for the distribution bar). */
  driversCount?: number | null
}

export type DriverTierRuleInput = Omit<DriverTierRule, 'id' | 'tier' | 'updatedAt' | 'driversCount'>

export interface TierMetrics {
  completedTrips: number
  ratingAvg: number
  acceptanceRate: number
  cancellationRate: number
}

/** Row of `GET /admin/drivers/{id}/tier-history`. */
export interface DriverTierHistoryEntry {
  id: string
  fromTier: DriverTier | null
  toTier: DriverTier
  metrics: TierMetrics | null
  reason: 'weekly_recalc' | 'admin' | string
  computedAt: string
  /** Assumed: the admin reason / actor of a manual change. */
  note?: string | null
  actorName?: string | null
}

export type IncentiveType = 'daily' | 'weekly' | 'zone_quest' | 'one_time'
export type IncentiveProgressStatus = 'in_progress' | 'achieved' | 'paid' | 'expired' | 'voided'
/** Derived client-side from `isActive` and the window. */
export type IncentiveListStatus = 'active' | 'upcoming' | 'ended' | 'inactive'

/** Body of `POST/PUT /admin/incentives` — every `driver_incentives` column except counters (§F15.8). */
export interface IncentiveInput {
  nameAr: string
  nameEn: string
  descriptionAr: string | null
  descriptionEn: string | null
  type: IncentiveType
  cityId: string | null
  zoneIds: string[] | null
  rideCategoryIds: string[] | null
  targetTrips: number
  rewardAmount: number
  minTripFare: number | null
  startsAt: string
  endsAt: string
  /** 0 = Sunday … 6 = Saturday. */
  daysOfWeek: number[] | null
  /** `HH:mm`, Riyadh time. */
  dailyFrom: string | null
  dailyTo: string | null
  minTier: DriverTier | null
  minRating: number | null
  requiresOptIn: boolean
  maxParticipants: number | null
  budgetAmount: number | null
  notifyOnPublish: boolean
  isActive: boolean
}

export interface Incentive extends IncentiveInput {
  id: string
  spentAmount: number
  createdAt?: string | null
  updatedAt?: string | null
  /** Assumed counters. */
  participantsCount?: number | null
  achievedCount?: number | null
  paidCount?: number | null
}

/** Row of `GET /admin/incentives/{id}/progress`. */
export interface IncentiveProgress {
  id: string
  driverName: string | null
  periodStart: string
  completedTrips: number
  status: IncentiveProgressStatus
  rewardAmount: number | null
  incentiveMultiplier: number | null
  paidAt: string | null
  /** Assumed. */
  driverId?: string | null
  periodEnd?: string | null
  achievedAt?: string | null
  voidedReason?: string | null
}

/** Assumed `GET /admin/drivers/{id}/incentives` row (not in the contract; hidden on 404). */
export interface DriverIncentiveProgress {
  id: string
  incentiveId: string
  name?: string | null
  incentiveName?: string | null
  type?: IncentiveType | null
  targetTrips: number
  completedTrips: number
  periodStart: string
  periodEnd?: string | null
  status: IncentiveProgressStatus
  rewardAmount: number | null
  incentiveMultiplier?: number | null
  paidAt?: string | null
}

/** `breakdown.discounts[]` (docs/08 §F11.6). */
export interface TripDiscountLine {
  /** `promotion` in F15; other sources may be added by later features. */
  source: string
  reference: string | null
  label: string | null
  amount: number
}

export interface TripPromotionInfo {
  code: string
  status: RedemptionStatus
  discountAmount: number | null
  /** Assumed. */
  promotionId?: string | null
}

/** Assumed admin `Trip.ratings[]` (the contract only adds `myRating` for the app). */
export interface TripRatingInfo {
  id?: string | null
  raterRole: RaterRole
  stars: number
  tags: string[]
  comment?: string | null
  status?: RatingStatus | null
  createdAt?: string | null
}

/** `GET /catalog/cities`. */
export interface City {
  id: string
  code: string
  name: string
}
