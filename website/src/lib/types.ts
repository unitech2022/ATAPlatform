// Models from docs/05-api-contract.md (Step 1), camelCase as served by the API.

export type Role = 'passenger' | 'driver' | 'corporate_admin'

export type ApplicationStatus =
  | 'draft'
  | 'submitted'
  | 'under_review'
  | 'approved'
  | 'rejected'
  | 'suspended'

export type DocumentStatus = 'pending' | 'verified' | 'rejected'

export type Gender = 'male' | 'female' | 'unknown'

export interface ApiErrorBody {
  error: {
    code: string
    message: string
    details?: Record<string, unknown>
  }
}

// ---- Auth ------------------------------------------------------------------

export interface OtpRequestPayload {
  phoneNumber: string
  role: Role
  language: 'ar' | 'en'
}

export interface OtpRequestResponse {
  requestId: string
  phoneNumber: string
  expiresInSeconds: number
  resendAfterSeconds: number
  devCode?: string | null
}

export interface DeviceInfo {
  deviceId: string
  platform: 'android' | 'ios' | 'web'
  deviceName: string
  appVersion: string
}

export interface OtpVerifyPayload {
  requestId: string
  phoneNumber: string
  code: string
  role: Role
  device: DeviceInfo
}

export interface User {
  id: string
  phoneNumber: string
  fullName: string | null
  language: 'ar' | 'en'
  gender: Gender
  roles: string[]
  termsAcceptedAt: string | null
  createdAt: string
}

export interface DriverSummary {
  applicationNumber: string
  applicationStatus: ApplicationStatus
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresIn: number
  refreshToken: string
  refreshTokenExpiresAt: string
  isNewUser: boolean
  user: User
  driver: DriverSummary | null
}

// ---- Catalog ---------------------------------------------------------------

export interface RideCategory {
  id: string
  code: string
  name: string
  description: string
  icon: string
  seats: number
  maxStops: number
  sortOrder: number
  estimate: { etaMinutes: number; price: number } | null
}

export interface City {
  id: string
  code: string
  name: string
}

// ---- Driver application ----------------------------------------------------

export interface DriverProfile {
  fullName: string | null
  nationalId: string | null
  dateOfBirth: string | null
  cityId: string | null
  gender: Gender | null
  iban: string | null
}

export interface DriverProfilePayload {
  fullName: string
  nationalId: string
  dateOfBirth: string
  cityId: string
  gender: 'male' | 'female'
  iban?: string
}

export interface Vehicle {
  id: string
  make: string
  model: string
  year: number
  color: string
  plateNumber: string
  seats: number
  rideCategoryId: string
}

export type VehiclePayload = Omit<Vehicle, 'id'>

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

export interface ApplicationSteps {
  profileComplete: boolean
  vehicleComplete: boolean
  documentsComplete: boolean
  canSubmit: boolean
}

export interface DriverApplication {
  applicationNumber: string
  status: ApplicationStatus
  rejectionReason: string | null
  submittedAt: string | null
  approvedAt: string | null
  profile: DriverProfile
  vehicle: Vehicle | null
  documents: DriverDocument[]
  requiredDocuments: RequiredDocument[]
  steps: ApplicationSteps
}

export interface SubmitResponse {
  status: ApplicationStatus
}

// ---- Shared ----------------------------------------------------------------

/** Paged list envelope (`?page=&pageSize=`) from docs/05-api-contract.md. */
export interface Page<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export type LatLngTuple = [number, number]

export type TripStatus =
  | 'requested'
  | 'searching'
  | 'scheduled'
  | 'driver_assigned'
  | 'driver_en_route'
  | 'driver_arrived'
  | 'waiting'
  | 'pin_verified'
  | 'in_trip'
  | 'completed'
  | 'cancelled'
  | 'no_drivers'

export interface Place {
  name: string
  address?: string | null
  lat: number
  lng: number
}

// ---- Public trip share (F12, /public/trip-shares/{token}) ------------------

export interface PublicTripShare {
  status: TripStatus
  passengerFirstName: string | null
  driver: { firstName: string; ratingAvg: number | null; photoUrl: string | null } | null
  vehicle: { make: string; model: string; color: string; plateNumber: string } | null
  rideCategory: { code: string; name: string } | null
  pickup: Place
  dropoff: Place
  stops: Place[]
  driverLocation: { lat: number; lng: number; heading: number | null; updatedAt: string } | null
  route: { planned: LatLngTuple[]; travelled: LatLngTuple[] } | null
  etaSeconds: number | null
  etaTarget: 'pickup' | 'dropoff' | null
  timeline: {
    assignedAt: string | null
    arrivedAt: string | null
    startedAt: string | null
    completedAt: string | null
    cancelledAt: string | null
  }
  expiresAt: string | null
  refreshSeconds: number | null
}

// ---- Help center (F18, /help/*) --------------------------------------------

export type HelpAudience = 'passenger' | 'driver'

export interface HelpCategory {
  id: string
  code: string
  name: string
  icon: string
  articlesCount: number
}

export interface HelpArticleSummary {
  id: string
  slug: string
  title: string
  excerpt: string
  categoryId: string
  updatedAt: string
}

export interface HelpArticle {
  id: string
  slug: string
  title: string
  body: string
  category: { id: string; code: string; name: string }
  tags: string[] | null
  updatedAt: string
  related: { slug: string; title: string }[]
}

// ---- Corporate portal (F19, /corporate/*) ----------------------------------

export type CorporateAccountStatus = 'pending' | 'active' | 'suspended' | 'closed'
export type CorporateRole = 'corporate_admin' | 'employee'
export type CorporateUserStatus = 'invited' | 'active' | 'disabled'
export type InvoiceStatus = 'draft' | 'issued' | 'paid' | 'overdue' | 'void'

export interface NationalAddress {
  buildingNumber: string
  street: string
  district: string
  city: string
  postalCode: string
  additionalNumber: string
  countryCode: string
}

export interface CorporateAccount {
  id: string
  accountNumber: string
  legalNameAr: string
  legalNameEn: string
  displayName: string
  crNumber: string
  vatNumber: string | null
  billingEmail: string
  billingAddress: NationalAddress | null
  cityId: string | null
  cityName?: string | null
  contactName: string
  contactPhone: string
  status: CorporateAccountStatus
  creditLimit: number
  billingCycle: string
  paymentTermsDays: number
  defaultPolicyId: string | null
}

export interface CorporateAccountPayload {
  billingEmail: string
  billingAddress: NationalAddress
  contactName: string
  contactPhone: string
}

export interface CorporateTripSummary {
  id: string
  tripNumber: string
  status: TripStatus
  employeeName: string | null
  guestName: string | null
  isGuest: boolean
  pickupName: string
  dropoffName: string
  categoryName: string | null
  amount: number | null
  scheduledAt: string | null
  requestedAt: string | null
  completedAt: string | null
}

export interface CorporateDashboard {
  monthToDate: { trips: number; spend: number }
  activeEmployees: number
  invitedEmployees: number
  budgetUtilizationPercent: number | null
  creditLimit: number
  creditUsed: number
  openInvoices: { count: number; amount: number }
  recentTrips: CorporateTripSummary[]
}

export interface CorporateEmployee {
  id: string
  fullName: string | null
  phoneNumber: string
  role: CorporateRole
  employeeNumber: string | null
  department: string | null
  costCenter: { id: string; code: string; name: string } | string | null
  costCenterId?: string | null
  policyId?: string | null
  policyName: string | null
  monthlyBudget: number | null
  spentThisMonth: number
  status: CorporateUserStatus
  activatedAt: string | null
}

export interface CorporateEmployeePayload {
  phoneNumber: string
  fullName: string
  role: CorporateRole
  employeeNumber?: string | null
  department?: string | null
  costCenterId?: string | null
  policyId?: string | null
  monthlyBudget?: number | null
}

export interface EmployeeImportResult {
  created: number
  skipped: { row: number; reason: string }[]
}

export interface TimeWindow {
  from: string
  to: string
}

export interface CorporatePolicy {
  id: string
  name: string
  isDefault: boolean
  allowedRideCategoryIds: string[] | null
  allowedDays: number[] | null
  timeWindows: TimeWindow[] | null
  allowedZoneIds: string[] | null
  zoneMatch: 'pickup_and_dropoff' | 'pickup_or_dropoff'
  maxFarePerTrip: number | null
  monthlyBudgetPerEmployee: number | null
  requirePurpose: boolean
  requireCostCenter: boolean
  allowScheduled: boolean
  allowGuestBooking: boolean
  isActive: boolean
}

export type CorporatePolicyPayload = Omit<CorporatePolicy, 'id' | 'isDefault'>

export interface CostCenter {
  id: string
  code: string
  name: string
  isActive: boolean
}

export type CostCenterPayload = Omit<CostCenter, 'id'>

export interface PolicyViolation {
  rule: string
  limit?: number | null
  allowed?: unknown
}

export interface QuoteCategory {
  rideCategoryId: string
  code: string
  name: string
  etaMinutes: number | null
  total: number
}

export interface CorporateQuote {
  quoteId: string
  expiresAt: string
  distanceMeters: number
  durationSeconds: number
  categories: QuoteCategory[]
  corporate: { allowed: boolean; violations: PolicyViolation[]; remainingBudget: number | null } | null
}

export interface CorporateQuotePayload {
  pickup: Place
  dropoff: Place
  stops: Place[]
  rideCategoryId?: string
  bookingType: 'now' | 'scheduled'
  scheduledAt?: string
  employeeId?: string
  purpose?: string
  costCenterId?: string
}

export interface CorporateBookingPayload {
  employeeId?: string
  guest?: { name: string; phoneNumber: string }
  pickup: Place
  dropoff: Place
  stops: Place[]
  rideCategoryId: string
  bookingType: 'now' | 'scheduled'
  scheduledAt?: string
  quoteId?: string
  tripPurpose: string
  costCenterId?: string
  riderNote?: string
}

export interface Trip {
  id: string
  tripNumber: string
  status: TripStatus
  bookingType: 'now' | 'scheduled'
  scheduledAt: string | null
  rideCategory: { id: string; code: string; name: string } | null
  pickup: Place
  dropoff: Place
  stops: Place[]
  paymentMethod: string
  estimatedFare: number | null
  finalFare: number | null
  estimatedDistanceMeters: number | null
  estimatedDurationSeconds: number | null
  driver: { id: string; fullName: string | null; ratingAvg: number | null; phoneMasked: string | null } | null
  vehicle: { make: string; model: string; color: string; plateNumber: string } | null
  cancelledBy: string | null
  cancellationReason: string | null
  timeline: {
    requestedAt: string | null
    assignedAt: string | null
    arrivedAt: string | null
    startedAt: string | null
    completedAt: string | null
    cancelledAt: string | null
  }
  corporate: {
    companyName: string
    purpose: string | null
    costCenter: string | null
    isGuest: boolean
    guestName: string | null
    employeeName?: string | null
  } | null
  cancellation?: { reasonName: string | null; fee: number | null; feeCharged: number | null } | null
}

export interface CancellationReason {
  code: string
  name: string
  requiresNote: boolean
}

export interface CancellationPreview {
  stage: string
  fee: number
  isFree: boolean
  freeUntil: string | null
  requiresReview: boolean
  message: string | null
}

export interface CorporateInvoice {
  id: string
  invoiceNumber: string
  periodStart: string
  periodEnd: string
  issueDate: string
  dueDate: string
  currency: string
  tripsCount: number
  subtotalExclVat: number
  vatRate: number
  vatAmount: number
  totalInclVat: number
  status: InvoiceStatus
  paidAmount: number | null
  paidAt: string | null
  pdfFileId: string | null
}

export interface InvoiceLine {
  id: string
  lineType: 'trip' | 'cancellation_fee' | 'adjustment'
  tripNumber: string | null
  tripDate: string | null
  employeeName: string | null
  department: string | null
  costCenterCode: string | null
  guestName: string | null
  purpose: string | null
  pickupName: string | null
  dropoffName: string | null
  description: string
  amountExclVat: number
  vatAmount: number
  amountInclVat: number
}

export interface CorporateInvoiceDetail extends CorporateInvoice {
  lines: Page<InvoiceLine>
}

export type ReportGroupBy = 'employee' | 'department' | 'cost_center' | 'month' | 'category'

export interface ReportSummary {
  rows: { key: string; label: string; trips: number; amount: number; avgFare: number }[]
  totals: { trips: number; amount: number }
}

export interface ReportTripRow {
  id: string
  tripNumber: string
  date: string
  employee: string | null
  employeeNumber: string | null
  department: string | null
  costCenter: string | null
  guest: string | null
  purpose: string | null
  category: string | null
  pickup: string
  dropoff: string
  distanceKm: number | null
  amountInclVat: number
  vat: number
  status: TripStatus
}

export interface CorporateApiKey {
  id: string
  name: string
  keyPrefix: string
  scopes: string[]
  lastUsedAt: string | null
  expiresAt: string | null
  revokedAt: string | null
  createdAt: string
  /** Only returned once, on creation. */
  key?: string
}
