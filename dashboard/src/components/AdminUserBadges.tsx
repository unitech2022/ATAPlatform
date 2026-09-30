import { useLang } from '../context/lang'
import { isLocked } from '../lib/rbac'
import type { AdminUser } from '../lib/types'
import { Badge } from './Badge'

/** Status badges of an admin user (active/disabled, locked, MFA, temporary password) — list and detail. */
export function AdminUserBadges({ user }: { user: AdminUser }) {
  const { t } = useLang()
  return (
    <span className="flex flex-wrap gap-1.5">
      <Badge tone={user.isActive ? 'brand' : 'danger'}>{user.isActive ? t('auActive') : t('auDisabled')}</Badge>
      {isLocked(user) && <Badge tone="warning">{t('auLocked')}</Badge>}
      <Badge tone={user.mfaEnabled ? 'ink' : 'muted'}>{user.mfaEnabled ? t('auMfaOn') : t('auMfaOff')}</Badge>
      {user.mustChangePassword && <Badge tone="warning">{t('auMustChange')}</Badge>}
    </span>
  )
}
