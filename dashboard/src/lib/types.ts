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
