import { createContext, useContext } from 'react'
import type { Session } from './session'
import type { AuthResponse } from './types'

export interface AuthContextValue {
  session: Session | null
  /** True when the session was cleared because the refresh token was rejected. */
  expired: boolean
  login: (response: AuthResponse) => void
  logout: () => Promise<void>
}

const defaults: AuthContextValue = {
  session: null,
  expired: false,
  login: () => {},
  logout: async () => {},
}

/** Driver portal session (`ata-session`). */
export const AuthContext = createContext<AuthContextValue>(defaults)

/** Corporate portal session (`ata-business-session`). */
export const BusinessAuthContext = createContext<AuthContextValue>(defaults)

export function useAuth(): AuthContextValue {
  return useContext(AuthContext)
}

export function useBusinessAuth(): AuthContextValue {
  return useContext(BusinessAuthContext)
}
