import type { AuthResponse, DriverSummary, User } from './types'

/**
 * Two independent sessions live side by side: the driver portal (`/driver/*`)
 * and the corporate portal (`/business/app/*`). Each has its own storage key so
 * signing in or out of one never touches the other.
 */
export type SessionScope = 'driver' | 'business'

const SESSION_KEYS: Record<SessionScope, string> = {
  driver: 'ata-session',
  business: 'ata-business-session',
}
const DEVICE_KEY = 'ata-device-id'

/** Fired on `window` whenever a stored session changes (login, logout, refresh, expiry). */
export const SESSION_EVENT = 'ata:session'

export interface SessionEventDetail {
  scope: SessionScope
  session: Session | null
}

export interface Session {
  accessToken: string
  refreshToken: string
  user: User
  driver: DriverSummary | null
}

export function sessionFromAuth(response: AuthResponse): Session {
  return {
    accessToken: response.accessToken,
    refreshToken: response.refreshToken,
    user: response.user,
    driver: response.driver ?? null,
  }
}

export function readSession(scope: SessionScope = 'driver'): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEYS[scope])
    if (!raw) return null
    const parsed: unknown = JSON.parse(raw)
    if (
      typeof parsed === 'object' &&
      parsed !== null &&
      'accessToken' in parsed &&
      'refreshToken' in parsed &&
      'user' in parsed
    ) {
      return parsed as Session
    }
    return null
  } catch {
    return null
  }
}

export function writeSession(session: Session | null, scope: SessionScope = 'driver'): void {
  try {
    if (session) localStorage.setItem(SESSION_KEYS[scope], JSON.stringify(session))
    else localStorage.removeItem(SESSION_KEYS[scope])
  } catch {
    // Storage may be unavailable (private mode); the in-memory state still updates.
  }
  window.dispatchEvent(new CustomEvent<SessionEventDetail>(SESSION_EVENT, { detail: { scope, session } }))
}

/** True when the session belongs to a corporate admin (guard for `/business/app/*`). */
export function isCorporateAdmin(session: Session | null): boolean {
  return Boolean(session?.user.roles.includes('corporate_admin'))
}

/** Stable per-browser device id sent with OTP verification. */
export function getDeviceId(): string {
  try {
    const existing = localStorage.getItem(DEVICE_KEY)
    if (existing) return existing
    const created = crypto.randomUUID()
    localStorage.setItem(DEVICE_KEY, created)
    return created
  } catch {
    return crypto.randomUUID()
  }
}
