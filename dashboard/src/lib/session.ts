import type { AdminMe, User } from './types'

const ACCESS_KEY = 'ata.accessToken'
const REFRESH_KEY = 'ata.refreshToken'
const USER_KEY = 'ata.user'
/** F20: cached `GET /admin/me` so the sidebar can be filtered on reload before the refetch answers. */
const ME_KEY = 'ata.adminMe'

/** Fired on window whenever the stored session is cleared because it could not be refreshed. */
export const SESSION_EXPIRED_EVENT = 'ata:session-expired'

/** Fired on window when any request answers `403 password_change_required` (docs/12 §F20.4). */
export const PASSWORD_CHANGE_REQUIRED_EVENT = 'ata:password-change-required'

function read(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

function write(key: string, value: string | null) {
  try {
    if (value === null) localStorage.removeItem(key)
    else localStorage.setItem(key, value)
  } catch {
    // storage unavailable — the session simply will not persist across reloads
  }
}

export const session = {
  getAccessToken: () => read(ACCESS_KEY),
  getRefreshToken: () => read(REFRESH_KEY),
  getUser(): User | null {
    const raw = read(USER_KEY)
    if (!raw) return null
    try {
      return JSON.parse(raw) as User
    } catch {
      return null
    }
  },
  save(tokens: { accessToken: string; refreshToken: string; user?: User }) {
    write(ACCESS_KEY, tokens.accessToken)
    write(REFRESH_KEY, tokens.refreshToken)
    if (tokens.user) write(USER_KEY, JSON.stringify(tokens.user))
  },
  getMe(): AdminMe | null {
    const raw = read(ME_KEY)
    if (!raw) return null
    try {
      const value = JSON.parse(raw) as AdminMe
      return Array.isArray(value?.permissions) ? value : null
    } catch {
      return null
    }
  },
  saveMe(me: AdminMe | null) {
    write(ME_KEY, me ? JSON.stringify(me) : null)
  },
  clear() {
    write(ACCESS_KEY, null)
    write(REFRESH_KEY, null)
    write(USER_KEY, null)
    write(ME_KEY, null)
  },
  expire() {
    session.clear()
    window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
  },
}
