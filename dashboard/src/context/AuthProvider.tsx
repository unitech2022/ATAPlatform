import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { auth as authApi } from '../lib/admin'
import { SESSION_EXPIRED_EVENT, session } from '../lib/session'
import type { User } from '../lib/types'
import { AuthContext, type AuthContextValue } from './auth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => (session.getAccessToken() ? session.getUser() : null))

  useEffect(() => {
    const onExpired = () => setUser(null)
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired)
  }, [])

  const login = useCallback(async (username: string, password: string) => {
    const response = await authApi.login(username, password)
    session.save({ accessToken: response.accessToken, refreshToken: response.refreshToken, user: response.user })
    setUser(response.user)
  }, [])

  const logout = useCallback(async () => {
    const refreshToken = session.getRefreshToken()
    session.clear()
    setUser(null)
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken)
      } catch {
        // the local session is already gone; a failed server-side revoke is not actionable here
      }
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, login, logout }),
    [user, login, logout],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
