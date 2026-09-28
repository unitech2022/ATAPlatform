import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useBusinessAuth } from '../lib/auth'
import { isCorporateAdmin } from '../lib/session'

/** Guards `/business/app/*`: needs a corporate-portal session carrying the `corporate_admin` role. */
export function RequireCorporateAuth({ children }: { children: ReactNode }) {
  const { session } = useBusinessAuth()
  const location = useLocation()
  if (!isCorporateAdmin(session)) {
    return <Navigate to="/business/login" replace state={{ from: `${location.pathname}${location.search}` }} />
  }
  return children
}
