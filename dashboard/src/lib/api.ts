import { LANG_STORAGE_KEY } from '../i18n'
import { session } from './session'
import type { ApiErrorBody, AuthResponse } from './types'

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api/v1').replace(
  /\/$/,
  '',
)

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly details: Record<string, unknown> | undefined

  constructor(status: number, code: string, message: string, details?: Record<string, unknown>) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.details = details
  }
}

export type QueryParams = Record<string, string | number | boolean | null | undefined>

interface RequestOptions {
  query?: QueryParams
  body?: unknown
  /** Skip the Bearer header and the 401 refresh dance (login, refresh). */
  anonymous?: boolean
}

function buildUrl(path: string, query?: QueryParams) {
  const url = new URL(`${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`)
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null || value === '') continue
      url.searchParams.set(key, String(value))
    }
  }
  return url.toString()
}

function currentLang() {
  try {
    return localStorage.getItem(LANG_STORAGE_KEY) === 'en' ? 'en' : 'ar'
  } catch {
    return 'ar'
  }
}

function baseHeaders(anonymous: boolean, hasBody: boolean) {
  const headers = new Headers({ Accept: 'application/json', 'Accept-Language': currentLang() })
  if (hasBody) headers.set('Content-Type', 'application/json')
  if (!anonymous) {
    const token = session.getAccessToken()
    if (token) headers.set('Authorization', `Bearer ${token}`)
  }
  return headers
}

async function parseError(response: Response): Promise<ApiError> {
  let body: Partial<ApiErrorBody> | null = null
  try {
    body = (await response.json()) as Partial<ApiErrorBody>
  } catch {
    body = null
  }
  const error = body?.error
  return new ApiError(
    response.status,
    error?.code ?? `http_${response.status}`,
    error?.message ?? response.statusText ?? 'Request failed',
    error?.details,
  )
}

let refreshInFlight: Promise<boolean> | null = null

/** Rotates the refresh token once even when several requests fail at the same time. */
function refreshSession(): Promise<boolean> {
  if (refreshInFlight) return refreshInFlight
  const refreshToken = session.getRefreshToken()
  if (!refreshToken) return Promise.resolve(false)

  refreshInFlight = (async () => {
    try {
      const response = await fetch(buildUrl('/auth/refresh'), {
        method: 'POST',
        headers: baseHeaders(true, true),
        body: JSON.stringify({ refreshToken }),
      })
      if (!response.ok) return false
      const auth = (await response.json()) as AuthResponse
      session.save({ accessToken: auth.accessToken, refreshToken: auth.refreshToken, user: auth.user })
      return true
    } catch {
      return false
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

async function send(method: string, path: string, options: RequestOptions, retried = false): Promise<Response> {
  const anonymous = options.anonymous ?? false
  const hasBody = options.body !== undefined
  let response: Response
  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers: baseHeaders(anonymous, hasBody),
      body: hasBody ? JSON.stringify(options.body) : undefined,
    })
  } catch {
    throw new ApiError(0, 'network_error', 'network_error')
  }

  if (response.status === 401 && !anonymous) {
    if (!retried && (await refreshSession())) {
      return send(method, path, options, true)
    }
    session.expire()
  }

  if (!response.ok) throw await parseError(response)
  return response
}

async function request<T>(method: string, path: string, options: RequestOptions = {}): Promise<T> {
  const response = await send(method, path, options)
  if (response.status === 204) return undefined as T
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const api = {
  get: <T>(path: string, query?: QueryParams) => request<T>('GET', path, { query }),
  post: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'body'>) =>
    request<T>('POST', path, { ...options, body }),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, { body }),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, { body }),
  delete: <T>(path: string) => request<T>('DELETE', path),
  /** Fetches a protected binary (e.g. /files/{id}) with the Bearer header. */
  blob: async (path: string, query?: QueryParams) => {
    const response = await send('GET', path, { query })
    return response.blob()
  },
  /** Like `blob`, plus the file name the server suggests in `Content-Disposition` (CSV exports). */
  download: async (path: string, query?: QueryParams): Promise<{ blob: Blob; fileName: string | null }> => {
    const response = await send('GET', path, { query })
    return { blob: await response.blob(), fileName: fileNameFrom(response.headers.get('Content-Disposition')) }
  },
}

function fileNameFrom(disposition: string | null): string | null {
  if (!disposition) return null
  const encoded = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(disposition)
  if (encoded) {
    try {
      return decodeURIComponent(encoded[1].trim().replace(/^"|"$/g, ''))
    } catch {
      // fall through to the plain filename
    }
  }
  const plain = /filename="?([^";]+)"?/i.exec(disposition)
  return plain ? plain[1].trim() : null
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError
}
