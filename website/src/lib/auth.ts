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

export const AuthContext = createContext<AuthContextValue>({
  session: null,
  expired: false,
  login: () => {},
  logout: async () => {},
})

export function useAuth(): AuthContextValue {
  return useContext(AuthContext)
}
