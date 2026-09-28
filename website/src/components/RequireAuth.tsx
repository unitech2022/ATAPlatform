import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import { useAuth } from '../lib/auth'

/** Redirects to the phone/OTP screen when there is no stored session. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { session } = useAuth()
  if (!session) return <Navigate to="/driver" replace />
  return children
}
