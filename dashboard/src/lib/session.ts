import type { User } from './types'

const ACCESS_KEY = 'ata.accessToken'
const REFRESH_KEY = 'ata.refreshToken'
const USER_KEY = 'ata.user'

/** Fired on window whenever the stored session is cleared because it could not be refreshed. */
export const SESSION_EXPIRED_EVENT = 'ata:session-expired'

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
  clear() {
    write(ACCESS_KEY, null)
    write(REFRESH_KEY, null)
    write(USER_KEY, null)
  },
  expire() {
    session.clear()
    window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
  },
}
