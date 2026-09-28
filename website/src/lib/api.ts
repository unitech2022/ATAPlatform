import { readStoredLanguage } from '../i18n'
import { readSession, sessionFromAuth, writeSession } from './session'
import type {
  ApiErrorBody,
  AuthResponse,
  City,
  DriverApplication,
  DriverDocument,
  DriverProfilePayload,
  OtpRequestPayload,
  OtpRequestResponse,
  OtpVerifyPayload,
  RideCategory,
  SubmitResponse,
  VehiclePayload,
} from './types'

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api/v1').replace(/\/+$/, '')

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

let refreshInFlight: Promise<boolean> | null = null

/** Rotate tokens via /auth/refresh. Resolves false (and clears the session) when it fails. */
function refreshSession(): Promise<boolean> {
  if (refreshInFlight) return refreshInFlight
  refreshInFlight = (async () => {
    const current = readSession()
    if (!current) return false
    try {
      const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Accept-Language': readStoredLanguage() },
        body: JSON.stringify({ refreshToken: current.refreshToken }),
      })
      if (!response.ok) {
        writeSession(null)
        return false
      }
      const auth = (await response.json()) as AuthResponse
      writeSession(sessionFromAuth(auth))
      return true
    } catch {
      return false
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = true, retried = false } = options
  const headers = new Headers({ Accept: 'application/json', 'Accept-Language': readStoredLanguage() })

  const session = auth ? readSession() : null
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
    const refreshed = await refreshSession()
    if (refreshed) return request<T>(path, { ...options, retried: true })
    writeSession(null)
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
  logout: (refreshToken: string) =>
    request<void>('/auth/logout', { method: 'POST', body: { refreshToken } }),
}

// ---- Catalog (public) ------------------------------------------------------

export const catalogApi = {
  rideCategories: () => request<RideCategory[]>('/catalog/ride-categories', { auth: false }),
  cities: () => request<City[]>('/catalog/cities', { auth: false }),
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

/**
 * GET /files/{id} needs the bearer token, so a plain link cannot open it.
 * We open the tab synchronously (keeps popup blockers happy), then stream the
 * file into it as an object URL.
 */
export async function openFilePreview(fileId: string): Promise<void> {
  const tab = window.open('', '_blank')
  const session = readSession()
  const headers = new Headers({ 'Accept-Language': readStoredLanguage() })
  if (session) headers.set('Authorization', `Bearer ${session.accessToken}`)
  try {
    let response = await fetch(`${API_BASE_URL}/files/${fileId}`, { headers })
    if (response.status === 401 && session && (await refreshSession())) {
      const refreshed = readSession()
      if (refreshed) headers.set('Authorization', `Bearer ${refreshed.accessToken}`)
      response = await fetch(`${API_BASE_URL}/files/${fileId}`, { headers })
    }
    if (!response.ok) throw await parseError(response)
    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    if (tab) tab.location.href = url
    else window.location.assign(url)
    window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
  } catch (error) {
    tab?.close()
    throw error
  }
}
