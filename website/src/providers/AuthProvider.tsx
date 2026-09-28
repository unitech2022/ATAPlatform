import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi } from '../lib/api'
import { AuthContext } from '../lib/auth'
import { readSession, SESSION_EVENT, sessionFromAuth, writeSession, type Session } from '../lib/session'
import type { AuthResponse } from '../lib/types'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => readSession())
  const [expired, setExpired] = useState(false)

  useEffect(() => {
    const onChange = (event: Event) => {
      const next = (event as CustomEvent<Session | null>).detail
      setSession((previous) => {
        // A null written by the API layer (refresh failed) while we had a session = expiry.
        if (previous && !next) setExpired(true)
        return next
      })
    }
    window.addEventListener(SESSION_EVENT, onChange)
    return () => window.removeEventListener(SESSION_EVENT, onChange)
  }, [])

  const login = useCallback((response: AuthResponse) => {
    setExpired(false)
    writeSession(sessionFromAuth(response))
  }, [])

  const logout = useCallback(async () => {
    const current = readSession()
    if (current) {
      try {
        await authApi.logout(current.refreshToken)
      } catch {
        // Best effort: the server may already have rotated/invalidated the token.
      }
    }
    writeSession(null)
    setExpired(false)
  }, [])

  const value = useMemo(() => ({ session, expired, login, logout }), [session, expired, login, logout])

  return <AuthContext value={value}>{children}</AuthContext>
}
