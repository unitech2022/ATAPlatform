import { useCallback, useEffect, useEffectEvent, useMemo, useRef, useState, type ReactNode } from 'react'
import { auth as authApi, me as meApi } from '../lib/admin'
import { isApiError } from '../lib/api'
import { hasPermission, isAuthResponse, isEnrollmentChallenge, isMfaChallenge } from '../lib/rbac'
import { PASSWORD_CHANGE_REQUIRED_EVENT, SESSION_EXPIRED_EVENT, session } from '../lib/session'
import type { AdminMe, AuthResponse, User } from '../lib/types'
import { AuthContext, type AuthContextValue, type LoginOutcome, type PermissionRequirement, type PermissionsStatus } from './auth'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => (session.getAccessToken() ? session.getUser() : null))
  const [me, setMe] = useState<AdminMe | null>(() => (session.getAccessToken() ? session.getMe() : null))
  const [status, setStatus] = useState<PermissionsStatus>(() => (session.getAccessToken() && session.getMe() ? 'ready' : 'loading'))
  const [mustChangePassword, setMustChangePassword] = useState(() => session.getMe()?.mustChangePassword ?? false)
  const requestId = useRef(0)

  useEffect(() => {
    const onExpired = () => {
      setUser(null)
      setMe(null)
      setStatus('loading')
      setMustChangePassword(false)
    }
    const onPasswordRequired = () => setMustChangePassword(true)
    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired)
    window.addEventListener(PASSWORD_CHANGE_REQUIRED_EVENT, onPasswordRequired)
    return () => {
      window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired)
      window.removeEventListener(PASSWORD_CHANGE_REQUIRED_EVENT, onPasswordRequired)
    }
  }, [])

  /** Loads `/admin/me`; stale answers (logout / a newer load) are ignored. */
  const loadMe = useCallback((): Promise<void> => {
    const id = ++requestId.current
    return meApi.get().then(
      (result) => {
        if (id !== requestId.current) return
        const normalized: AdminMe = { ...result, roles: result.roles ?? [], permissions: Array.isArray(result.permissions) ? result.permissions : [] }
        setMe(normalized)
        session.saveMe(normalized)
        setStatus('ready')
        setMustChangePassword(Boolean(normalized.mustChangePassword))
      },
      (error: unknown) => {
        if (id !== requestId.current) return
        if (isApiError(error) && error.code === 'password_change_required') {
          setMustChangePassword(true)
          return
        }
        if (isApiError(error) && error.status === 401) return
        // Older backend / network trouble: keep a cached copy if any, otherwise stop hiding things.
        setStatus((current) => (current === 'ready' ? current : 'unknown'))
      },
    )
  }, [])

  // Permissions are (re)loaded after every sign-in and on page reload (docs/12 §F20.3).
  const userId = user?.id ?? null
  const syncMe = useEffectEvent(() => {
    void loadMe()
  })
  useEffect(() => {
    if (userId) syncMe()
  }, [userId])

  const completeLogin = useCallback((response: AuthResponse) => {
    requestId.current += 1
    session.saveMe(null)
    session.save({ accessToken: response.accessToken, refreshToken: response.refreshToken, user: response.user })
    setMe(null)
    setStatus('loading')
    setMustChangePassword(Boolean(response.mustChangePassword))
    setUser(response.user)
  }, [])

  const login = useCallback(
    async (username: string, password: string): Promise<LoginOutcome> => {
      const response = await authApi.login(username, password)
      if (isMfaChallenge(response)) {
        return { kind: 'mfa', mfaToken: response.mfaToken, methods: response.methods?.length ? response.methods : ['totp', 'recovery_code'] }
      }
      if (isEnrollmentChallenge(response)) return { kind: 'enroll', mfaToken: response.mfaToken }
      if (!isAuthResponse(response)) throw new Error('Unexpected login response')
      completeLogin(response)
      return { kind: 'authenticated' }
    },
    [completeLogin],
  )

  const passwordChanged = useCallback(async () => {
    setMustChangePassword(false)
    await loadMe()
  }, [loadMe])

  const logout = useCallback(async () => {
    const refreshToken = session.getRefreshToken()
    requestId.current += 1
    session.clear()
    setUser(null)
    setMe(null)
    setStatus('loading')
    setMustChangePassword(false)
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken)
      } catch {
        // the local session is already gone; a failed server-side revoke is not actionable here
      }
    }
  }, [])

  const can = useCallback(
    (required: PermissionRequirement) => {
      if (!required) return true
      if (status === 'unknown') return true
      if (!me) return false
      return hasPermission(me.permissions, required)
    },
    [me, status],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      me,
      permissionsStatus: status,
      mustChangePassword,
      can,
      login,
      completeLogin,
      refreshMe: loadMe,
      passwordChanged,
      logout,
    }),
    [user, me, status, mustChangePassword, can, login, completeLogin, loadMe, passwordChanged, logout],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
