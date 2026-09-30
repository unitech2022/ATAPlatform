import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from '../context/auth'
import { ForcePasswordChangePage } from '../pages/ForcePasswordChangePage'

/** Signed-in guard; a temporary password (F20 `mustChangePassword`) blocks everything until it is changed. */
export function RequireAuth() {
  const { isAuthenticated, mustChangePassword } = useAuth()
  const location = useLocation()
  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />
  }
  if (mustChangePassword) return <ForcePasswordChangePage />
  return <Outlet />
}
