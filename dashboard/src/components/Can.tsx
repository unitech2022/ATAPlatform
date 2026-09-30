import type { ReactNode } from 'react'
import { usePermission, type PermissionRequirement } from '../context/auth'

/** Renders `children` only when the signed-in admin holds `permission` (`*` passes); `fallback` otherwise. */
export function Can({ permission, children, fallback = null }: { permission: PermissionRequirement; children: ReactNode; fallback?: ReactNode }) {
  return usePermission(permission) ? children : fallback
}
