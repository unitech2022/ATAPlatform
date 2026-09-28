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
