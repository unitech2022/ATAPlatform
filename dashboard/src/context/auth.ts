import { createContext, useContext } from 'react'
import type { PermissionCode } from '../lib/rbac'
import type { AdminMe, AuthResponse, MfaMethod, User } from '../lib/types'

/** What `login` resolved to (docs/12 §F20.5): signed in, or a second step with its short-lived `mfaToken`. */
export type LoginOutcome =
  | { kind: 'authenticated' }
  | { kind: 'mfa'; mfaToken: string; methods: MfaMethod[] }
  | { kind: 'enroll'; mfaToken: string }

/**
 * `loading` — `/admin/me` has not answered yet (and nothing is cached); `ready` — permissions known;
 * `unknown` — `/admin/me` failed (e.g. an older backend): nothing is hidden and the server's `403` stays the guard.
 */
export type PermissionsStatus = 'loading' | 'ready' | 'unknown'

export type PermissionRequirement = PermissionCode | readonly PermissionCode[] | undefined

export interface AuthContextValue {
  user: User | null
  isAuthenticated: boolean
  /** `GET /admin/me` (F20). */
  me: AdminMe | null
  permissionsStatus: PermissionsStatus
  /** Forced password change (temporary password, §F20.4); every other admin call answers `403 password_change_required`. */
  mustChangePassword: boolean
  /** `*` passes everything; an array means "any of". */
  can: (required: PermissionRequirement) => boolean
  login: (username: string, password: string) => Promise<LoginOutcome>
  /** Stores the tokens of a finished login (password, MFA or enrollment step) and loads `/admin/me`. */
  completeLogin: (auth: AuthResponse) => void
  refreshMe: () => Promise<void>
  /** Called after `POST /admin/me/password` succeeds. */
  passwordChanged: () => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside <AuthProvider>')
  return value
}

/** Boolean permission check for buttons and panels (the server remains the authority). */
export function usePermission(required: PermissionRequirement): boolean {
  return useAuth().can(required)
}

