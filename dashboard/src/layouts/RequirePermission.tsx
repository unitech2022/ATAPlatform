import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import { Card } from '../components/Card'
import { PermissionError } from '../components/PermissionError'
import { PageSpinner } from '../components/Spinner'
import { useAuth, type PermissionRequirement } from '../context/auth'
import { ApiError } from '../lib/api'
import { firstAllowedPath } from '../nav'

function codesOf(permission: PermissionRequirement) {
  if (!permission) return ''
  return typeof permission === 'string' ? permission : permission.join(' | ')
}

/**
 * Route guard (docs/12 §F20.9): renders the page only when `/admin/me` grants `permission`; otherwise the same
 * "no permission" view the pages show for a server `403` (`PermissionError`).
 */
export function RequirePermission({ permission, children }: { permission: PermissionRequirement; children: ReactNode }) {
  const { can, permissionsStatus } = useAuth()
  if (!permission) return children
  if (permissionsStatus === 'loading') return <PageSpinner />
  if (!can(permission)) {
    const codes = codesOf(permission)
    return (
      <Card>
        <PermissionError error={new ApiError(403, 'forbidden', 'forbidden', { permission: codes })} permission={codes} />
      </Card>
    )
  }
  return children
}

/** `/` needs `dashboard.view`; without it the admin lands on the first page they may open. */
export function HomeRoute({ children }: { children: ReactNode }) {
  const { can, permissionsStatus } = useAuth()
  if (permissionsStatus === 'loading') return <PageSpinner />
  if (can('dashboard.view')) return children
  return <Navigate to={firstAllowedPath(can) ?? '/account/security'} replace />
}
