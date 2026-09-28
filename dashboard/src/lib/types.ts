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
