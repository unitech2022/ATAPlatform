import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi } from '../lib/api'
import { AuthContext, BusinessAuthContext } from '../lib/auth'
import {
  readSession,
  SESSION_EVENT,
  sessionFromAuth,
  writeSession,
  type Session,
  type SessionEventDetail,
  type SessionScope,
} from '../lib/session'
import type { AuthResponse } from '../lib/types'

interface AuthProviderProps {
  children: ReactNode
  /** Which stored session this provider owns (default: the driver portal). */
  scope?: SessionScope
}

export function AuthProvider({ children, scope = 'driver' }: AuthProviderProps) {
  const [session, setSession] = useState<Session | null>(() => readSession(scope))
  const [expired, setExpired] = useState(false)

  useEffect(() => {
    const onChange = (event: Event) => {
      const detail = (event as CustomEvent<SessionEventDetail>).detail
      if (detail.scope !== scope) return
      const next = detail.session
      setSession((previous) => {
        // A null written by the API layer (refresh failed) while we had a session = expiry.
        if (previous && !next) setExpired(true)
        return next
      })
    }
    window.addEventListener(SESSION_EVENT, onChange)
    return () => window.removeEventListener(SESSION_EVENT, onChange)
  }, [scope])

  const login = useCallback(
    (response: AuthResponse) => {
      setExpired(false)
      writeSession(sessionFromAuth(response), scope)
    },
    [scope],
  )

  const logout = useCallback(async () => {
    const current = readSession(scope)
    if (current) {
      try {
        await authApi.logout(current.refreshToken, scope)
      } catch {
        // Best effort: the server may already have rotated/invalidated the token.
      }
    }
    writeSession(null, scope)
    setExpired(false)
  }, [scope])

  const value = useMemo(() => ({ session, expired, login, logout }), [session, expired, login, logout])

  const Context = scope === 'business' ? BusinessAuthContext : AuthContext
  return <Context value={value}>{children}</Context>
}
