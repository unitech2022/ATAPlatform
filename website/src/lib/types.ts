// Models from docs/05-api-contract.md (Step 1), camelCase as served by the API.

export type Role = 'passenger' | 'driver'

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
