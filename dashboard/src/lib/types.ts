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
  /** F20 (docs/12 §F20.5): admin logins carry the forced-change flag; MFA verification reports the remaining recovery codes. */
  mustChangePassword?: boolean
  recoveryCodesRemaining?: number
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
  | 'scheduled'
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
export type PaymentMethod = 'cash' | 'wallet' | 'card' | 'corporate'
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
  /** F16 — favorite driver request on the trip (docs/10 §F16.3 `Trip.favorite`, assumed to be on the admin payload too). */
  favorite?: TripFavoriteInfo | null
  /** F17 — scheduled-ride details (docs/11 §F17.4 `Trip.scheduling`, extended with assumed admin-only fields). */
  scheduling?: TripSchedulingInfo | null
  /** F17 — airport pickup / dropoff details (docs/11 §F17.8 `Trip.airport`). */
  airport?: TripAirportInfo | null
  /** F19 — corporate booking details (docs/12 §F19.4 `Trip.corporate`, extended with assumed admin-only fields); null for non-corporate trips. */
  corporate?: TripCorporateInfo | null
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

// ---------------------------------------------------------------------------
// F16 — favorite driver (docs/10 §F16)
// ---------------------------------------------------------------------------

/** `trips.favorite_status` (§F16.1). */
export type FavoriteStatus = 'requested' | 'accepted' | 'unavailable' | 'rejected' | 'expired'

/** `Trip.favorite` (§F16.3); the trailing fields are assumed admin extras. */
export interface TripFavoriteInfo {
  driverId?: string | null
  driverName?: string | null
  status: FavoriteStatus
  discountApplied?: boolean | null
  /** Assumed: the rule pinned at acceptance (`trips.favorite_discount_rule_id`). */
  discountRuleId?: string | null
  discountRuleName?: string | null
}

/** Body of `POST/PUT /admin/favorite-discount-rules` (§F16.3). */
export interface FavoriteDiscountRuleInput {
  name: string
  /** 1..50 (DECIMAL(5,2)). */
  discountPercent: number
  maxDiscountAmount: number
  minFare: number | null
  stackableWithPromotions: boolean
  validFrom: string
  /** null = open ended. */
  validTo: string | null
  rideCategoryIds: string[] | null
  zoneIds: string[] | null
  bookingTypes: BookingType[] | null
  priority: number
  isActive: boolean
}

/** Row / detail of `/admin/favorite-discount-rules`. */
export interface FavoriteDiscountRule extends FavoriteDiscountRuleInput {
  id: string
  createdByName?: string | null
  createdAt?: string | null
  updatedAt?: string | null
}

export type FavoriteRuleStatus = 'active' | 'scheduled' | 'expired' | 'inactive'

export interface FavoriteTopDriver {
  driverId: string
  name: string | null
  favoritesCount: number
  favoriteTrips: number
}

/** `GET /admin/favorites/stats?from=&to=&cityId=` (§F16.3). */
export interface FavoriteStats {
  favoriteRequests: number
  accepted: number
  fallback: number
  favoriteBookingRate: number
  discountUsageCount: number
  discountTotal: number
  topDrivers: FavoriteTopDriver[]
}

// ---------------------------------------------------------------------------
// F17 — scheduled rides (docs/11 §F17.2–§F17.5)
// ---------------------------------------------------------------------------

/** `scheduled_ride_reservations.status` (§F17.2). */
export type ReservationStatus = 'reserved' | 'confirmed' | 'assigned' | 'released' | 'no_show' | 'completed' | 'cancelled'
/** `reservationStatus` of a list row: `none` means the trip has no active reservation. */
export type ScheduledReservationState = ReservationStatus | 'none'
/** `reservation=` filter of `GET /admin/scheduled-trips` (§F17.4). */
export type ScheduledReservationFilter = 'none' | 'reserved' | 'confirmed' | 'assigned'
export type ReservationSource = 'marketplace' | 'favorite' | 'admin'
export type ReservationReleaseReason = 'driver_released' | 'confirmation_missed' | 'final_confirmation_missed' | 'no_show' | 'trip_cancelled' | 'admin'
export type ScheduledFeeType = 'none' | 'fixed' | 'percent' | 'pricing_rule'
export type ReminderKind = 'reminder' | 'confirm_request' | 'final_confirm_request'
export type ReminderStatus = 'pending' | 'sent' | 'skipped' | 'cancelled'

/** Body of `POST/PUT /admin/scheduled-ride-rules` — every column of `scheduled_ride_rules` in camelCase (§F17.2/§F17.4). */
export interface ScheduledRuleInput {
  cityId: string | null
  rideCategoryId: string | null
  maxDaysAhead: number
  minLeadMinutes: number
  maxOpenPerPassenger: number
  lockDemandNormal: boolean
  marketplaceEnabled: boolean
  marketplaceRadiusKm: number
  favoriteExclusiveMinutes: number
  driverAssignmentLeadMinutes: number
  confirmationTimeoutMinutes: number
  finalConfirmationMinutesBefore: number
  finalConfirmationTimeoutMinutes: number
  searchStartMinutesBefore: number
  riderReminderOffsets: number[]
  driverReminderOffsets: number[]
  freeCancelMinutesBefore: number
  lateCancelFeeType: ScheduledFeeType
  lateCancelFeeAmount: number | null
  lateCancelFeePercent: number | null
  lateCancelDriverCompensationPercent: number
  driverFreeReleaseMinutesBefore: number
  driverLateReleasePenaltyPoints: number
  driverConfirmationMissedPenaltyPoints: number
  driverNoShowPenaltyPoints: number
  driverNoShowGraceMinutes: number
  maxReservationsPerDriver: number
  reservationGapMinutes: number
  isActive: boolean
}

export interface ScheduledRule extends ScheduledRuleInput {
  id: string
  createdAt?: string | null
  updatedAt?: string | null
}

/** Row of `GET /admin/scheduled-trips` (§F17.4); `rideCategoryId`/`zoneId`/`driverId`/`status` are assumed optional extras. */
export interface ScheduledTripRow {
  tripId: string
  tripNumber: string
  scheduledAt: string
  passengerName: string | null
  categoryName: string | null
  pickupName: string | null
  dropoffName: string | null
  reservationStatus: ScheduledReservationState | null
  driverName: string | null
  minutesToPickup: number
  /** No first confirmation within 90 minutes of pickup. */
  atRisk: boolean
  rideCategoryId?: string | null
  zoneId?: string | null
  driverId?: string | null
  status?: TripStatus | null
}

/** `GET /admin/scheduling/stats?from=&to=` (§F17.4). */
export interface SchedulingStats {
  booked: number
  completed: number
  cancelledByPassenger: number
  cancelledLate: number
  driverReleases: number
  confirmationMissed: number
  driverNoShows: number
  rematched: number
  scheduledCompletionRate: number
  scheduledCancellationRate: number
  avgReservationLeadHours: number | null
}

/** One reservation of a trip (§F17.2 columns); the admin trip payload is assumed to list them all as `scheduling.reservations`. */
export interface ScheduledReservation {
  id?: string
  driverId?: string | null
  driverName?: string | null
  source?: ReservationSource | null
  status: ReservationStatus
  reservedAt?: string | null
  confirmRequestedAt?: string | null
  confirmedAt?: string | null
  finalConfirmRequestedAt?: string | null
  assignedAt?: string | null
  releasedAt?: string | null
  releaseReason?: ReservationReleaseReason | null
  isLateRelease?: boolean | null
  penaltyPoints?: number | null
}

/** `scheduled_ride_reminders` row (§F17.2); assumed to be listed as `scheduling.reminders` on the admin trip payload. */
export interface ScheduledReminder {
  id?: string
  recipientRole: 'passenger' | 'driver'
  kind: ReminderKind
  offsetMinutes?: number | null
  sendAt: string
  sentAt?: string | null
  status: ReminderStatus
}

/** `Trip.scheduling` (§F17.4 passenger shape) plus assumed admin fields (`reservations`, `reminders`). */
export interface TripSchedulingInfo {
  freeCancelUntil?: string | null
  searchStartsAt?: string | null
  reservation?: {
    status: ReservationStatus
    driverId?: string | null
    driverName?: string | null
    driverFirstName?: string | null
    reservedAt?: string | null
  } | null
  reservations?: ScheduledReservation[] | null
  reminders?: ScheduledReminder[] | null
}

// ---------------------------------------------------------------------------
// F17 — airports (docs/11 §F17.6–§F17.8)
// ---------------------------------------------------------------------------

export type AirportZoneKind = 'terminal' | 'pickup_zone' | 'driver_waiting_area'
export type AirportQueueStatus = 'waiting' | 'offered' | 'dispatched' | 'left' | 'removed'

/** Body of `POST/PUT /admin/airports` (§F17.6 `airports`). */
export interface AirportInput {
  cityId: string
  /** IATA, 3 letters, unique. */
  code: string
  nameAr: string
  nameEn: string
  lat: number
  lng: number
  geofence: LatLngTuple[]
  requiresPickupZone: boolean
  defaultFreeWaitingMinutes: number | null
  defaultWaitingPerMinute: number | null
  queueEnabled: boolean
  isActive: boolean
}

export interface Airport extends AirportInput {
  id: string
  createdAt?: string | null
  updatedAt?: string | null
  /** Assumed optional counters on list rows. */
  zonesCount?: number | null
}

/** Body of `POST/PUT /admin/airports/{id}/zones` (§F17.6 `airport_zones`). */
export interface AirportZoneInput {
  kind: AirportZoneKind
  code: string
  terminalCode: string | null
  nameAr: string
  nameEn: string
  /** Required for `driver_waiting_area`. */
  polygon: LatLngTuple[] | null
  lat: number
  lng: number
  instructionsAr: string | null
  instructionsEn: string | null
  freeWaitingMinutes: number | null
  waitingPerMinute: number | null
  sortOrder: number
  isActive: boolean
}

export interface AirportZone extends AirportZoneInput {
  id: string
  airportId?: string
  createdAt?: string | null
  updatedAt?: string | null
}

/** Row of `GET /admin/airports/{id}/queue` (§F17.8); `driverId` is an assumed optional extra for the profile link. */
export interface AirportQueueEntry {
  entryId: string
  position: number
  driverName: string | null
  categoryCode: string | null
  enteredAt: string
  lastSeenAt: string | null
  status: AirportQueueStatus
  driverId?: string | null
}

/** `Trip.airport` (§F17.8). */
export interface TripAirportInfo {
  code: string
  direction: 'pickup' | 'dropoff'
  zoneName?: string | null
  terminalCode?: string | null
  flightNumber?: string | null
  freeWaitingMinutes?: number | null
}

// ---------------------------------------------------------------------------
// F18 — support (docs/11 §F18.1 / §F18.3)
// ---------------------------------------------------------------------------

export type TicketStatus = 'open' | 'pending_user' | 'in_progress' | 'resolved' | 'closed'
export type TicketPriority = 'urgent' | 'high' | 'normal' | 'low'
export type TicketType = 'trip_issue' | 'payment_issue' | 'lost_item' | 'safety' | 'account' | 'other'
export type TicketChannel = 'app' | 'website' | 'dashboard' | 'phone'
export type TicketRequesterRole = 'passenger' | 'driver' | 'corporate_admin'
export type TicketSlaState = 'ok' | 'due_soon' | 'breached'
export type TicketMessageAuthor = 'user' | 'agent' | 'system'

export type DisputeReason = 'overcharged' | 'route_longer' | 'waiting_charged' | 'cancellation_fee' | 'promo_not_applied' | 'other'
export type DisputeStatus = 'open' | 'under_review' | 'approved' | 'partially_approved' | 'rejected'
export type DisputeResolution = 'refund_full' | 'refund_partial' | 'no_refund'

export type HelpAudience = 'passenger' | 'driver' | 'all'

/** `GET /admin/support/summary` (§F18.3). */
export interface SupportSummary {
  open: number
  unassigned: number
  pendingUser: number
  breachingFirstResponse: number
  breachingResolution: number
  avgFirstResponseMinutes: number | null
  avgResolutionHours: number | null
  csatAvg: number | null
}

/**
 * Row of `GET /admin/support/tickets` (§F18.3). `channel`, `firstResponseAt` and `requesterUserId` are assumed optional
 * extras (the spec lists a channel filter in the dashboard brief but not the response field).
 */
export interface TicketListItem {
  id: string
  ticketNumber: string
  type: TicketType
  subject: string
  status: TicketStatus
  priority: TicketPriority
  requesterName: string | null
  requesterRole: TicketRequesterRole
  tripNumber: string | null
  assignedToName: string | null
  firstResponseDueAt: string
  resolutionDueAt: string
  slaState: TicketSlaState
  lastMessageAt: string
  lastMessageBy: TicketMessageAuthor
  createdAt: string
  channel?: TicketChannel | null
  firstResponseAt?: string | null
  requesterUserId?: string | null
}

export interface TicketAttachment {
  fileId: string
  fileName: string | null
  contentType: string | null
}

export interface SupportMessage {
  id: string
  authorRole: TicketMessageAuthor
  authorName: string | null
  body: string
  isInternal: boolean
  attachments: TicketAttachment[]
  createdAt: string
  cannedResponseCode?: string | null
}

/** Refund created by F11 for a resolved dispute (`refund` of the resolve answer, §F18.3). */
export interface DisputeRefund {
  id: string
  refundNumber?: string | null
  status: RefundStatus
  amount?: number | null
  destination?: RefundDestination | null
}

/** Fare dispute as returned inline by the admin ticket detail and by `GET /admin/support/disputes` (§F18.1 `fare_disputes`). */
export interface FareDispute {
  id: string
  ticketId: string
  tripId: string
  requesterUserId?: string | null
  reason: DisputeReason
  chargedAmount: number
  requestedRefundAmount: number | null
  status: DisputeStatus
  resolution: DisputeResolution | null
  approvedRefundAmount: number | null
  refundId: string | null
  resolvedAt: string | null
  resolutionNote: string | null
  createdAt: string
  /** Assumed display extras of the admin list. */
  ticketNumber?: string | null
  tripNumber?: string | null
  requesterName?: string | null
  resolvedByName?: string | null
  refund?: DisputeRefund | null
  refundNumber?: string | null
  refundStatus?: RefundStatus | null
}

export interface DisputeResolveInput {
  resolution: DisputeResolution
  amount?: number
  note: string
}

export interface DisputeResolveResult extends FareDispute {
  refund: DisputeRefund | null
}

export interface TicketRequester {
  userId: string
  fullName: string | null
  role: TicketRequesterRole
  /** Full phone number (§F18.3 — agents see it complete). */
  phoneNumber: string | null
  language?: 'ar' | 'en' | null
  createdAt?: string
  /** Assumed extra for the driver profile link. */
  driverId?: string | null
}

/** `trip` of the admin ticket detail (backend `AdminTicketTripDto`); `receipt` is the F11 receipt of a completed trip. */
export interface TicketTripSummary {
  id: string
  tripNumber: string
  status?: string | null
  pickupName?: string | null
  dropoffName?: string | null
  finalFare?: number | null
  estimatedFare?: number | null
  paymentMethod?: PaymentMethod | null
  requestedAt?: string | null
  completedAt?: string | null
  cancelledAt?: string | null
  driverName?: string | null
  receipt?: { total?: number | null; netPaid?: number | null } | null
}

export interface TicketLinkedCase {
  id: string
  /** `caseNumber` / `reportNumber`. */
  number?: string | null
  status?: string | null
}

/** `linked` of the admin ticket detail (§F18.3 "الحالات المرتبطة"). */
export interface TicketLinks {
  safetyCase?: TicketLinkedCase | null
  lostItemReport?: TicketLinkedCase | null
}

/**
 * `GET /admin/support/tickets/{id}` (§F18.3). Requester, trip, linked cases and the SLA clock fields follow §F18.1;
 * the nested shapes of `requester`, `trip`, `safetyCase` and `lostItem` are assumed.
 */
export interface SupportTicketDetail {
  id: string
  ticketNumber: string
  type: TicketType
  subject: string
  status: TicketStatus
  priority: TicketPriority
  channel: TicketChannel
  requester: TicketRequester
  trip: TicketTripSummary | null
  messages: SupportMessage[]
  dispute: FareDispute | null
  assignedToUserId: string | null
  assignedToName: string | null
  assignedAt?: string | null
  firstResponseDueAt: string
  resolutionDueAt: string
  firstResponseAt: string | null
  slaPausedAt: string | null
  slaPausedSeconds: number
  slaState?: TicketSlaState
  resolvedAt: string | null
  closedAt: string | null
  lastMessageAt: string
  lastMessageBy: TicketMessageAuthor
  csatScore: number | null
  csatComment: string | null
  unreadByUser?: number
  linked?: TicketLinks | null
  /** Flat ids of §F18.1 (`safety_case_id`, `lost_item_report_id`), used when `linked` is absent. */
  safetyCaseId?: string | null
  lostItemReportId?: string | null
  createdAt: string
  updatedAt?: string
}

export interface SupportMessageInput {
  body: string
  isInternal: boolean
  fileIds?: string[]
  cannedResponseCode?: string
}

/** Payload of the `SupportTicketUpdated` / `SupportTicketCreated` hub events for the `admins` group (§F18.3 SignalR). */
export interface SupportTicketUpdate {
  ticketId: string
  status: TicketStatus
  priority?: TicketPriority
  lastMessageBy?: TicketMessageAuthor
}

export interface CannedResponse {
  id: string
  code: string
  title: string
  bodyAr: string
  bodyEn: string
  ticketType: TicketType | null
  isActive: boolean
  createdAt?: string
  updatedAt?: string
}

export interface CannedResponseInput {
  code: string
  title: string
  bodyAr: string
  bodyEn: string
  ticketType: TicketType | null
  isActive: boolean
}

export interface SlaPolicy {
  id?: string
  priority: TicketPriority
  firstResponseMinutes: number
  resolutionMinutes: number
  updatedAt?: string | null
}

export interface HelpCategory {
  id: string
  code: string
  nameAr: string
  nameEn: string
  icon: string | null
  audience: HelpAudience
  sortOrder: number
  isActive: boolean
  /** Assumed extra shown in the CMS table. */
  articlesCount?: number
  createdAt?: string
  updatedAt?: string
}

export interface HelpCategoryInput {
  code: string
  nameAr: string
  nameEn: string
  icon: string | null
  audience: HelpAudience
  sortOrder: number
  isActive: boolean
}

export interface HelpArticle {
  id: string
  categoryId: string
  categoryName?: string | null
  slug: string
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  audience: HelpAudience
  tags: string[] | null
  sortOrder: number
  isPublished: boolean
  publishedAt: string | null
  viewCount: number
  helpfulYes: number
  helpfulNo: number
  updatedBy?: string | null
  createdAt?: string
  updatedAt?: string
}

export interface HelpArticleInput {
  categoryId: string
  slug: string
  titleAr: string
  titleEn: string
  bodyAr: string
  bodyEn: string
  audience: HelpAudience
  tags: string[]
  sortOrder: number
}

/** `GET /admin/support/stats` (§F18.5): KPIs over a period; `slaCompliance` is a 0..1 share. */
export interface SupportStats {
  created: number
  resolved: number
  closed: number
  openNow: number
  avgResolutionMinutes: number | null
  medianResolutionMinutes: number | null
  avgFirstResponseMinutes: number | null
  medianFirstResponseMinutes: number | null
  slaCompliance: number | null
  csatAvg: number | null
  csatCount: number
  byType: { type: TicketType; created: number; resolved: number }[]
}

// ---------------------------------------------------------------------------
// F19 — corporate accounts (docs/12 §F19.1 / §F19.4 "الإدارة")
// ---------------------------------------------------------------------------

export type CorporateAccountStatus = 'pending' | 'active' | 'suspended' | 'closed'
export type CorporateUserStatus = 'invited' | 'active' | 'disabled'
export type CorporateRole = 'corporate_admin' | 'employee'
export type CorporateInvoiceStatus = 'draft' | 'issued' | 'paid' | 'overdue' | 'void'
export type CorporateZoneMatch = 'pickup_and_dropoff' | 'pickup_or_dropoff'

/** `corporate_accounts.billing_address` — the Saudi national address (§F19.1). */
export interface CorporateBillingAddress {
  buildingNumber: string
  street: string
  district: string
  city: string
  postalCode: string
  additionalNumber: string
  countryCode: string
}

/** Row of `GET /admin/corporate/accounts`: the base `corporate_accounts` columns (camelCase) plus the assumed `cityName`. */
export interface CorporateAccountListItem {
  id: string
  accountNumber: string
  displayName: string
  legalNameAr: string
  legalNameEn: string
  crNumber: string
  status: CorporateAccountStatus
  cityId: string | null
  cityName?: string | null
  creditLimit: number
  createdAt: string
}

/** Assumed optional block of `GET /admin/corporate/accounts/{id}`; mirrors `GET /corporate/dashboard` (§F19.4). */
export interface CorporateAccountSummary {
  monthToDate?: { trips: number; spend: number } | null
  activeEmployees?: number | null
  invitedEmployees?: number | null
  budgetUtilizationPercent?: number | null
  creditUsed?: number | null
  openInvoices?: { count: number; amount: number } | null
}

export interface CorporateAccount extends CorporateAccountListItem {
  vatNumber: string | null
  billingEmail: string
  billingAddress: CorporateBillingAddress | null
  contactName: string
  contactPhone: string
  billingCycle: 'monthly'
  paymentTermsDays: number
  defaultPolicyId?: string | null
  notes: string | null
  updatedAt?: string
  summary?: CorporateAccountSummary | null
}

export interface CorporateAccountInput {
  legalNameAr: string
  legalNameEn: string
  displayName: string
  crNumber: string
  vatNumber: string | null
  billingEmail: string
  billingAddress: CorporateBillingAddress
  cityId: string | null
  contactName: string
  contactPhone: string
  creditLimit: number
  billingCycle: 'monthly'
  paymentTermsDays: number
  notes: string | null
}

export interface CorporateAdminInviteInput {
  phoneNumber: string
  fullName: string
}

/** `GET /admin/corporate/receivables` (§F19.4); `unbilled` and `unpaidInvoices` are amounts. */
export interface CorporateReceivable {
  accountId: string
  name: string
  creditLimit: number
  unbilled: number
  unpaidInvoices: number
  overdueAmount: number
}

/** Row of `GET /admin/corporate/accounts/{id}/employees` — same shape as the company-admin list (§F19.4); `invitationExpiresAt` is assumed. */
export interface CorporateEmployee {
  id: string
  fullName: string | null
  phoneNumber: string
  role: CorporateRole
  employeeNumber: string | null
  department: string | null
  costCenter: string | null
  policyName: string | null
  monthlyBudget: number | null
  spentThisMonth: number | null
  status: CorporateUserStatus
  activatedAt: string | null
  invitationExpiresAt?: string | null
}

/** `corporate_policies` columns (camelCase); returned by the assumed read-only `GET /admin/corporate/accounts/{id}/policies`. */
export interface CorporatePolicy {
  id: string
  name: string
  isDefault: boolean
  allowedRideCategoryIds: string[] | null
  allowedDays: number[] | null
  timeWindows: { from: string; to: string }[] | null
  allowedZoneIds: string[] | null
  zoneMatch: CorporateZoneMatch
  maxFarePerTrip: number | null
  monthlyBudgetPerEmployee: number | null
  requirePurpose: boolean
  requireCostCenter: boolean
  allowScheduled: boolean
  allowGuestBooking: boolean
  isActive: boolean
}

/** `corporate_cost_centers` columns; returned by the assumed `GET /admin/corporate/accounts/{id}/cost-centers`. */
export interface CorporateCostCenter {
  id: string
  code: string
  name: string
  isActive: boolean
}

/** Row of the assumed `GET /admin/corporate/accounts/{id}/trips`: the company CSV columns (§F19.4 `/corporate/reports/trips/export`) plus ids. */
export interface CorporateTripRow {
  tripId: string
  tripNumber: string
  date: string
  employee: string | null
  employeeNumber: string | null
  department: string | null
  costCenter: string | null
  guest: string | null
  purpose: string | null
  category: string | null
  pickup: string | null
  dropoff: string | null
  distanceKm: number | null
  amountInclVat: number | null
  vat: number | null
  status: TripStatus
}

/** `corporate_invoices` columns (camelCase) plus the company name for the cross-company overview. */
export interface CorporateInvoice {
  id: string
  invoiceNumber: string
  accountId: string
  accountName: string | null
  accountNumber?: string | null
  periodStart: string
  periodEnd: string
  issueDate: string | null
  dueDate: string | null
  currency: string
  tripsCount: number
  subtotalExclVat: number
  vatRate: number
  vatAmount: number
  totalInclVat: number
  status: CorporateInvoiceStatus
  issuedAt?: string | null
  paidAt: string | null
  paidAmount: number | null
  paymentReference: string | null
  voidReason: string | null
}

export interface CorporateMarkPaidInput {
  amount: number
  reference: string
  paidAt?: string
}

export interface CorporateAdjustmentInput {
  amount: number
  description: string
}

/** `Trip.corporate` (§F19.4); the trailing fields are assumed admin extras. */
export interface TripCorporateInfo {
  companyName: string
  purpose: string | null
  costCenter: string | null
  isGuest: boolean
  guestName: string | null
  accountId?: string | null
  employeeName?: string | null
  employeeNumber?: string | null
  department?: string | null
  guestPhone?: string | null
  policyName?: string | null
}

// ---------------------------------------------------------------------------
// F20 — identity, MFA, roles & permissions (docs/12 §F20.4–§F20.5)
// ---------------------------------------------------------------------------

export type MfaMethod = 'totp' | 'recovery_code'

/** `200 { mfaRequired, mfaToken, methods }` from `POST /auth/admin/login`. */
export interface MfaChallenge {
  mfaRequired: true
  mfaToken: string
  methods?: MfaMethod[]
}

/** `200 { mfaEnrollmentRequired, mfaToken }` from `POST /auth/admin/login`. */
export interface MfaEnrollmentChallenge {
  mfaEnrollmentRequired: true
  mfaToken: string
}

export type AdminLoginResponse = AuthResponse | MfaChallenge | MfaEnrollmentChallenge

export interface MfaEnrollment {
  secret: string
  otpauthUri: string
}

export interface MfaEnrollmentResult {
  recoveryCodes: string[]
  auth: AuthResponse
}

export interface RoleRef {
  id: string
  code: string
  name: string
}

/** `GET /admin/me`. `permissions` is `["*"]` for `super_admin`. */
export interface AdminMe {
  adminAccountId: string
  userId: string
  username: string
  fullName: string | null
  roles: RoleRef[]
  permissions: string[]
  mfaEnabled: boolean
  mustChangePassword: boolean
  onDuty?: boolean
}

export interface AdminSession {
  id: string
  userAgent: string | null
  ipAddress: string | null
  createdAt: string
  lastUsedAt: string | null
  current?: boolean
}

export interface AdminUser {
  id: string
  userId: string
  username: string
  fullName: string | null
  phoneNumber: string | null
  roles: RoleRef[]
  isActive: boolean
  mfaEnabled: boolean
  lastLoginAt: string | null
  onDuty?: boolean
  lockedUntil: string | null
  /** Detail-only extras (`GET /admin/admin-users/{id}`). */
  mustChangePassword?: boolean
  createdAt?: string | null
  mfaEnrolledAt?: string | null
  passwordChangedAt?: string | null
  failedLoginCount?: number | null
  permissions?: string[]
  sessions?: AdminSession[]
}

export interface AdminUserCreateInput {
  username: string
  fullName: string
  phoneNumber: string | null
  roleIds: string[]
  temporaryPassword?: string
}

export interface AdminUserCreateResult {
  adminUser: AdminUser
  temporaryPassword: string
}

export interface AdminUserUpdateInput {
  fullName: string
  roleIds: string[]
}

/** `GET /admin/permissions`; `name` follows `Accept-Language`. */
export interface PermissionInfo {
  code: string
  module: string
  name: string
  description: string | null
}

export interface Role {
  id: string
  code: string
  nameAr: string
  nameEn: string
  /** Localised name when the server adds one. */
  name?: string | null
  description: string | null
  isSystem: boolean
  /** Assumed list/detail extra (the doc does not name it). */
  userCount?: number | null
  permissionCodes?: string[]
  createdAt?: string | null
  updatedAt?: string | null
}

export interface RoleInput {
  code: string
  nameAr: string
  nameEn: string
  description: string | null
  permissionCodes: string[]
}

// ---------------------------------------------------------------------------
// F20 — reports (docs/12 §F20.6–§F20.7)
// ---------------------------------------------------------------------------

export type KpiUnit = 'count' | 'percent' | 'ratio' | 'seconds' | 'sar' | 'hours' | 'rating'

export interface KpiMetric {
  code: string
  name: string
  unit: KpiUnit
  value: number | null
  previousValue?: number | null
  changePercent?: number | null
  numerator?: number | null
  denominator?: number | null
}

export interface KpiFilters {
  cityId: string | null
  zoneId: string | null
  rideCategoryId: string | null
}

export interface KpiReport {
  from: string
  to: string
  filters?: KpiFilters
  metrics: KpiMetric[]
}

export type KpiGranularity = 'day' | 'week' | 'month'

export interface KpiSeriesPoint {
  periodStart: string
  value: number | null
  numerator?: number | null
  denominator?: number | null
}

export interface KpiSeries {
  code: string
  unit: KpiUnit
  points: KpiSeriesPoint[]
}

export type BreakdownGroup = 'city' | 'zone' | 'category'

export interface KpiBreakdownRow {
  key: string
  label: string
  value: number | null
  numerator?: number | null
  denominator?: number | null
}

export interface KpiBreakdown {
  metric: string
  rows: KpiBreakdownRow[]
}

export type ReportDataset = 'kpis' | 'trips' | 'payments' | 'payouts' | 'cancellations' | 'ratings' | 'support_tickets' | 'drivers' | 'incentives'
