import { readStoredLanguage } from '../i18n'
import { API_BASE_URL } from './config'
import { readSession, sessionFromAuth, writeSession, type SessionScope } from './session'
import type {
  ApiErrorBody,
  AuthResponse,
  CancellationPreview,
  CancellationReason,
  City,
  CorporateAccount,
  CorporateAccountPayload,
  CorporateApiKey,
  CorporateBookingPayload,
  CorporateDashboard,
  CorporateEmployee,
  CorporateEmployeePayload,
  CorporateInvoice,
  CorporateInvoiceDetail,
  CorporatePolicy,
  CorporatePolicyPayload,
  CorporateQuote,
  CorporateQuotePayload,
  CorporateTripSummary,
  CostCenter,
  CostCenterPayload,
  DriverApplication,
  DriverDocument,
  DriverProfilePayload,
  EmployeeImportResult,
  HelpArticle,
  HelpArticleSummary,
  HelpAudience,
  HelpCategory,
  OtpRequestPayload,
  OtpRequestResponse,
  OtpVerifyPayload,
  Page,
  PublicTripShare,
  ReportGroupBy,
  ReportSummary,
  ReportTripRow,
  RideCategory,
  SubmitResponse,
  Trip,
  VehiclePayload,
} from './types'

export { API_BASE_URL }

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly details: Record<string, unknown>

  constructor(status: number, code: string, message: string, details?: Record<string, unknown>) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.details = details ?? {}
  }

  /** Numeric detail such as `attemptsLeft` or `retryAfterSeconds`. */
  detailNumber(key: string): number | null {
    const value = this.details[key]
    return typeof value === 'number' ? value : null
  }

  /** String-array detail such as `missing`. */
  detailStrings(key: string): string[] {
    const value = this.details[key]
    return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : []
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  /** Attach the bearer token (default true). */
  auth?: boolean
  /** Which stored session supplies the token (default: driver portal). */
  scope?: SessionScope
  /** Internal: whether a refresh has already been attempted for this call. */
  retried?: boolean
}

function isErrorBody(value: unknown): value is ApiErrorBody {
  return (
    typeof value === 'object' &&
    value !== null &&
    'error' in value &&
    typeof (value as { error: unknown }).error === 'object' &&
    (value as { error: unknown }).error !== null
  )
}

async function parseError(response: Response): Promise<ApiError> {
  let parsed: unknown = null
  try {
    parsed = await response.json()
  } catch {
    parsed = null
  }
  if (isErrorBody(parsed)) {
    const { code, message, details } = parsed.error
    return new ApiError(response.status, code, message, details)
  }
  const fallbackCode = response.status === 401 ? 'unauthorized' : `http_${response.status}`
  return new ApiError(response.status, fallbackCode, response.statusText || fallbackCode)
}

const refreshInFlight: Partial<Record<SessionScope, Promise<boolean>>> = {}

/** Rotate tokens via /auth/refresh. Resolves false (and clears the session) when it fails. */
function refreshSession(scope: SessionScope): Promise<boolean> {
  const pending = refreshInFlight[scope]
  if (pending) return pending
  const task = (async () => {
    const current = readSession(scope)
    if (!current) return false
    try {
      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept-Language': readStoredLanguage() },
        body: JSON.stringify({ refreshToken: current.refreshToken }),
      })
      if (!response.ok) {
        writeSession(null, scope)
        return false
      }
      const auth = (await response.json()) as AuthResponse
      writeSession(sessionFromAuth(auth), scope)
      return true
    } catch {
      return false
    } finally {
      delete refreshInFlight[scope]
    }
  })()
  refreshInFlight[scope] = task
  return task
}

/** Build `?a=1&b=2`, skipping empty values. */
export function queryString(params: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    search.set(key, String(value))
  }
  const text = search.toString()
  return text ? `?${text}` : ''
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = true, retried = false, scope = 'driver' } = options
  const headers = new Headers({ Accept: 'application/json', 'Accept-Language': readStoredLanguage() })

  const session = auth ? readSession(scope) : null
  if (session) headers.set('Authorization', `Bearer ${session.accessToken}`)

  let payload: BodyInit | undefined
  if (body instanceof FormData) {
    payload = body
  } else if (body !== undefined) {
    headers.set('Content-Type', 'application/json')
    payload = JSON.stringify(body)
  }

  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { method, headers, body: payload })
  } catch {
    throw new ApiError(0, 'network_error', 'network_error')
  }

  if (response.status === 401 && auth && session && !retried) {
    const refreshed = await refreshSession(scope)
    if (refreshed) return request<T>(path, { ...options, retried: true })
    writeSession(null, scope)
  }

  if (!response.ok) throw await parseError(response)
  if (response.status === 204) return undefined as T

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

// ---- Auth ------------------------------------------------------------------

export const authApi = {
  requestOtp: (payload: OtpRequestPayload) =>
    request<OtpRequestResponse>('/auth/otp/request', { method: 'POST', body: payload, auth: false }),
  verifyOtp: (payload: OtpVerifyPayload) =>
    request<AuthResponse>('/auth/otp/verify', { method: 'POST', body: payload, auth: false }),
  logout: (refreshToken: string, scope: SessionScope = 'driver') =>
    request<void>('/auth/logout', { method: 'POST', body: { refreshToken }, scope }),
}

// ---- Catalog (public) ------------------------------------------------------

export const catalogApi = {
  rideCategories: () => request<RideCategory[]>('/catalog/ride-categories', { auth: false }),
  cities: () => request<City[]>('/catalog/cities', { auth: false }),
  cancellationReasons: (stage?: string) =>
    request<CancellationReason[]>(`/catalog/cancellation-reasons${queryString({ actor: 'passenger', stage })}`, { auth: false }),
}

// ---- Driver application ----------------------------------------------------

export interface UploadDocumentInput {
  documentTypeId: string
  file: File
  expiresAt?: string
}

export const driverApi = {
  getApplication: () => request<DriverApplication>('/driver/application'),
  updateProfile: (payload: DriverProfilePayload) =>
    request<unknown>('/driver/application/profile', { method: 'PUT', body: payload }),
  updateVehicle: (payload: VehiclePayload) =>
    request<unknown>('/driver/application/vehicle', { method: 'PUT', body: payload }),
  uploadDocument: ({ documentTypeId, file, expiresAt }: UploadDocumentInput) => {
    const form = new FormData()
    form.append('documentTypeId', documentTypeId)
    form.append('file', file, file.name)
    if (expiresAt) form.append('expiresAt', expiresAt)
    return request<DriverDocument>('/driver/documents', { method: 'POST', body: form })
  },
  deleteDocument: (id: string) => request<void>(`/driver/documents/${id}`, { method: 'DELETE' }),
  submit: () => request<SubmitResponse>('/driver/application/submit', { method: 'POST' }),
}

/** Authenticated GET that returns the raw body (PDF, CSV, images) with the same refresh rules. */
async function fetchBlob(path: string, scope: SessionScope): Promise<{ blob: Blob; filename: string | null }> {
  const headers = new Headers({ 'Accept-Language': readStoredLanguage() })
  const session = readSession(scope)
  if (session) headers.set('Authorization', `Bearer ${session.accessToken}`)
  let response: Response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { headers })
    if (response.status === 401 && session && (await refreshSession(scope))) {
      const refreshed = readSession(scope)
      if (refreshed) headers.set('Authorization', `Bearer ${refreshed.accessToken}`)
      response = await fetch(`${API_BASE_URL}${path}`, { headers })
    }
  } catch {
    throw new ApiError(0, 'network_error', 'network_error')
  }
  if (!response.ok) throw await parseError(response)
  const disposition = response.headers.get('Content-Disposition') ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)
  return { blob: await response.blob(), filename: match ? decodeURIComponent(match[1]) : null }
}

/**
 * GET /files/{id} needs the bearer token, so a plain link cannot open it.
 * We open the tab synchronously (keeps popup blockers happy), then stream the
 * file into it as an object URL.
 */
export async function openFilePreview(fileId: string, scope: SessionScope = 'driver', path = `/files/${fileId}`): Promise<void> {
  const tab = window.open('', '_blank')
  try {
    const { blob } = await fetchBlob(path, scope)
    const url = URL.createObjectURL(blob)
    if (tab) tab.location.href = url
    else window.location.assign(url)
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (error) {
    tab?.close()
    throw error
  }
}

/** Download an authenticated resource as a file (PDF invoices, CSV exports). */
export async function downloadFile(path: string, fallbackName: string, scope: SessionScope = 'business'): Promise<void> {
  const { blob, filename } = await fetchBlob(path, scope)
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename ?? fallbackName
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

// ---- Public trip share (F12) -----------------------------------------------

export const publicShareApi = {
  get: (token: string) => request<PublicTripShare>(`/public/trip-shares/${encodeURIComponent(token)}`, { auth: false }),
}

// ---- Help center (F18) -----------------------------------------------------

export interface HelpArticleQuery {
  categoryId?: string
  q?: string
  audience?: HelpAudience
  page?: number
}

export const helpApi = {
  categories: (audience: HelpAudience) =>
    request<HelpCategory[]>(`/help/categories${queryString({ audience })}`, { auth: false }),
  articles: (query: HelpArticleQuery) =>
    request<Page<HelpArticleSummary>>(`/help/articles${queryString({ ...query })}`, { auth: false }),
  article: (slug: string) => request<HelpArticle>(`/help/articles/${encodeURIComponent(slug)}`, { auth: false }),
  feedback: (id: string, helpful: boolean) =>
    request<void>(`/help/articles/${id}/feedback`, { method: 'POST', body: { helpful }, auth: false }),
}

// ---- Corporate portal (F19, Policies.CorporateAdmin) -----------------------

const corp = { scope: 'business' } as const

export interface EmployeeQuery {
  status?: string
  department?: string
  costCenterId?: string
  search?: string
  page?: number
}

export interface BookingQuery {
  status?: string
  from?: string
  to?: string
  employeeId?: string
  isGuest?: boolean
  page?: number
}

export interface ReportTripsQuery {
  from?: string
  to?: string
  employeeId?: string
  department?: string
  costCenterId?: string
  page?: number
}

export const corporateApi = {
  account: () => request<CorporateAccount>('/corporate/account', corp),
  updateAccount: (payload: CorporateAccountPayload) =>
    request<CorporateAccount>('/corporate/account', { ...corp, method: 'PUT', body: payload }),
  dashboard: () => request<CorporateDashboard>('/corporate/dashboard', corp),

  employees: (query: EmployeeQuery) =>
    request<Page<CorporateEmployee>>(`/corporate/employees${queryString({ ...query })}`, corp),
  employee: (id: string) => request<CorporateEmployee>(`/corporate/employees/${id}`, corp),
  inviteEmployee: (payload: CorporateEmployeePayload) =>
    request<CorporateEmployee>('/corporate/employees', { ...corp, method: 'POST', body: payload }),
  updateEmployee: (id: string, payload: CorporateEmployeePayload) =>
    request<CorporateEmployee>(`/corporate/employees/${id}`, { ...corp, method: 'PUT', body: payload }),
  importEmployees: (file: File) => {
    const form = new FormData()
    form.append('file', file, file.name)
    return request<EmployeeImportResult>('/corporate/employees/import', { ...corp, method: 'POST', body: form })
  },
  disableEmployee: (id: string) => request<unknown>(`/corporate/employees/${id}/disable`, { ...corp, method: 'POST' }),
  enableEmployee: (id: string) => request<unknown>(`/corporate/employees/${id}/enable`, { ...corp, method: 'POST' }),
  resendInvitation: (id: string) =>
    request<unknown>(`/corporate/employees/${id}/resend-invitation`, { ...corp, method: 'POST' }),
  deleteEmployee: (id: string) => request<void>(`/corporate/employees/${id}`, { ...corp, method: 'DELETE' }),

  policies: () => request<CorporatePolicy[]>('/corporate/policies', corp),
  createPolicy: (payload: CorporatePolicyPayload) =>
    request<CorporatePolicy>('/corporate/policies', { ...corp, method: 'POST', body: payload }),
  updatePolicy: (id: string, payload: CorporatePolicyPayload) =>
    request<CorporatePolicy>(`/corporate/policies/${id}`, { ...corp, method: 'PUT', body: payload }),
  deletePolicy: (id: string) => request<void>(`/corporate/policies/${id}`, { ...corp, method: 'DELETE' }),
  setDefaultPolicy: (id: string) => request<unknown>(`/corporate/policies/${id}/default`, { ...corp, method: 'POST' }),

  costCenters: () => request<CostCenter[]>('/corporate/cost-centers', corp),
  createCostCenter: (payload: CostCenterPayload) =>
    request<CostCenter>('/corporate/cost-centers', { ...corp, method: 'POST', body: payload }),
  updateCostCenter: (id: string, payload: CostCenterPayload) =>
    request<CostCenter>(`/corporate/cost-centers/${id}`, { ...corp, method: 'PUT', body: payload }),
  deleteCostCenter: (id: string) => request<void>(`/corporate/cost-centers/${id}`, { ...corp, method: 'DELETE' }),

  quote: (payload: CorporateQuotePayload) =>
    request<CorporateQuote>('/corporate/bookings/quote', { ...corp, method: 'POST', body: payload }),
  book: (payload: CorporateBookingPayload) =>
    request<Trip>('/corporate/bookings', { ...corp, method: 'POST', body: payload }),
  bookings: (query: BookingQuery) =>
    request<Page<CorporateTripSummary>>(`/corporate/bookings${queryString({ ...query })}`, corp),
  booking: (tripId: string) => request<Trip>(`/corporate/bookings/${tripId}`, corp),
  cancelPreview: (tripId: string, reasonCode?: string) =>
    request<CancellationPreview>(`/corporate/bookings/${tripId}/cancel/preview`, {
      ...corp,
      method: 'POST',
      body: reasonCode ? { reasonCode } : {},
    }),
  cancelBooking: (tripId: string, payload: { reasonCode: string; note?: string }) =>
    request<Trip>(`/corporate/bookings/${tripId}/cancel`, { ...corp, method: 'POST', body: payload }),

  invoices: (query: { status?: string; page?: number }) =>
    request<Page<CorporateInvoice>>(`/corporate/invoices${queryString({ ...query })}`, corp),
  invoice: (id: string, page = 1) =>
    request<CorporateInvoiceDetail>(`/corporate/invoices/${id}${queryString({ page })}`, corp),
  invoicePdfPath: (id: string) => `/corporate/invoices/${id}/pdf`,
  invoiceCsvPath: (id: string) => `/corporate/invoices/${id}/export?format=csv`,

  reportSummary: (query: { from?: string; to?: string; groupBy: ReportGroupBy }) =>
    request<ReportSummary>(`/corporate/reports/summary${queryString({ ...query })}`, corp),
  reportTrips: (query: ReportTripsQuery) =>
    request<Page<ReportTripRow>>(`/corporate/reports/trips${queryString({ ...query })}`, corp),
  reportTripsCsvPath: (query: ReportTripsQuery) =>
    `/corporate/reports/trips/export${queryString({ format: 'csv', ...query, page: undefined })}`,

  apiKeys: () => request<CorporateApiKey[]>('/corporate/api-keys', corp),
  createApiKey: (name: string) =>
    request<CorporateApiKey>('/corporate/api-keys', { ...corp, method: 'POST', body: { name } }),
  revokeApiKey: (id: string) => request<void>(`/corporate/api-keys/${id}`, { ...corp, method: 'DELETE' }),
}
