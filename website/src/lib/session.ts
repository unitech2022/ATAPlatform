import type { AuthResponse, DriverSummary, User } from './types'

const SESSION_KEY = 'ata-session'
const DEVICE_KEY = 'ata-device-id'

/** Fired on `window` whenever the stored session changes (login, logout, refresh, expiry). */
export const SESSION_EVENT = 'ata:session'

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
    driver: response.driver,
  }
}

export function readSession(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY)
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

export function writeSession(session: Session | null): void {
  try {
    if (session) localStorage.setItem(SESSION_KEY, JSON.stringify(session))
    else localStorage.removeItem(SESSION_KEY)
  } catch {
    // Storage may be unavailable (private mode); the in-memory state still updates.
  }
  window.dispatchEvent(new CustomEvent<Session | null>(SESSION_EVENT, { detail: session }))
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
