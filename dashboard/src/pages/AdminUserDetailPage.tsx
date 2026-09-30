import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { AdminUserBadges } from '../components/AdminUserBadges'
import { AdminUserFormModal } from '../components/AdminUserFormModal'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { DefinitionList } from '../components/DefinitionList'
import { EmptyState } from '../components/EmptyState'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { PermissionError } from '../components/PermissionError'
import { SecretRevealModal } from '../components/SecretRevealModal'
import { SessionsList } from '../components/SessionsList'
import { PageSpinner } from '../components/Spinner'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import type { TranslationKey } from '../i18n'
import { adminUsers, roles as rolesApi } from '../lib/admin'
import { isApiError } from '../lib/api'
import { formatDateTime } from '../lib/format'
import { conflictReasonKey, isLocked } from '../lib/rbac'

type Action = 'disable' | 'enable' | 'resetPassword' | 'resetMfa' | 'unlock' | 'revokeSessions'

const ACTION_COPY: Record<Action, { title: TranslationKey; copy: TranslationKey; confirm: TranslationKey; done: TranslationKey; danger: boolean }> = {
  disable: { title: 'auDisableTitle', copy: 'auDisableCopy', confirm: 'auDisable', done: 'auDisabledToast', danger: true },
  enable: { title: 'auEnableTitle', copy: 'auEnableCopy', confirm: 'auEnable', done: 'auEnabledToast', danger: false },
  resetPassword: { title: 'auResetPasswordTitle', copy: 'auResetPasswordCopy', confirm: 'auResetPassword', done: 'auResetPasswordToast', danger: true },
  resetMfa: { title: 'auResetMfaTitle', copy: 'auResetMfaCopy', confirm: 'auResetMfa', done: 'auResetMfaToast', danger: true },
  unlock: { title: 'auUnlockTitle', copy: 'auUnlockCopy', confirm: 'auUnlock', done: 'auUnlockedToast', danger: false },
  revokeSessions: { title: 'auRevokeSessionsTitle', copy: 'auRevokeSessionsCopy', confirm: 'auRevokeSessions', done: 'auRevokedSessionsToast', danger: true },
}

/** Admin user detail (`/admin-users/:id`, §F20.5): roles, disable/enable, reset password/MFA, unlock, sessions. */
export function AdminUserDetailPage() {
  const { id = '' } = useParams()
  const { t, lang } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const { me } = useAuth()
  const query = useQuery(() => adminUsers.get(id), `admin-user:${id}`)
  // Sessions come embedded in the detail answer; the separate list endpoint is only a fallback.
  const sessionSource = query.data ? (Array.isArray(query.data.sessions) ? 'embedded' : 'fetch') : 'wait'
  const sessions = useQuery(() => (sessionSource === 'fetch' ? adminUsers.sessions(id) : Promise.resolve(null)), `admin-user-sessions:${id}:${sessionSource}`)
  const roleList = useQuery(() => rolesApi.list(), 'roles-list')
  const [editing, setEditing] = useState(false)
  const [action, setAction] = useState<Action | null>(null)
  const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null)

  if (query.loading && !query.data) return <PageSpinner />
  if (query.error || !query.data) {
    return (
      <Card>
        <PermissionError error={query.error} permission="admin.users.manage" onRetry={query.reload} />
      </Card>
    )
  }
  const user = query.data
  const isSelf = me?.adminAccountId === user.id
  const locked = isLocked(user)
  const embedded = Array.isArray(user.sessions)
  const sessionRows = embedded ? (user.sessions ?? []) : (sessions.data ?? [])
  const sessionsUnsupported = isApiError(sessions.error) && sessions.error.status === 404

  const errorMessage = (error: unknown) => {
    const key = isApiError(error) ? conflictReasonKey(error.details) : null
    return key ? t(key) : describe(error)
  }

  const run = async (current: Action) => {
    try {
      if (current === 'resetPassword') {
        const result = await adminUsers.resetPassword(user.id)
        setTemporaryPassword(result.temporaryPassword)
      } else if (current === 'disable') await adminUsers.disable(user.id)
      else if (current === 'enable') await adminUsers.enable(user.id)
      else if (current === 'resetMfa') await adminUsers.resetMfa(user.id)
      else if (current === 'unlock') await adminUsers.unlock(user.id)
      else await adminUsers.revokeSessions(user.id)
      toast.success(t(ACTION_COPY[current].done))
      setAction(null)
      query.reload()
      sessions.reload()
    } catch (error) {
      toast.error(t('errorTitle'), errorMessage(error))
    }
  }

  const copy = action ? ACTION_COPY[action] : null

  return (
    <>
      <Link to="/admin-users" className="mb-4 inline-flex items-center gap-2 text-sm font-bold text-brand">
        <Icon name="arrow" className="size-4 ltr:rotate-180" />
        {t('auBack')}
      </Link>

      <Card className="mb-6">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="user" className="size-7" />
            </span>
            <div className="min-w-0">
              <p className="ltr-nums text-sm font-bold text-brand">{user.username}</p>
              <h2 className="break-words text-2xl font-bold leading-tight">{user.fullName || user.username}</h2>
              <div className="mt-2">
                <AdminUserBadges user={user} />
              </div>
              {isSelf && <p className="mt-2 text-xs text-muted">{t('auThisIsYou')}</p>}
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            <Button variant="secondary" icon="edit" onClick={() => setEditing(true)}>
              {t('auEditRoles')}
            </Button>
            <Button variant="secondary" icon="key" onClick={() => setAction('resetPassword')}>
              {t('auResetPassword')}
            </Button>
            {user.mfaEnabled && (
              <Button variant="secondary" icon="shield" onClick={() => setAction('resetMfa')}>
                {t('auResetMfa')}
              </Button>
            )}
            {locked && (
              <Button variant="brand" icon="lock" onClick={() => setAction('unlock')}>
                {t('auUnlock')}
              </Button>
            )}
            {user.isActive ? (
              <Button variant="danger-outline" icon="pause" disabled={isSelf} title={isSelf ? t('auErrSelfDisable') : undefined} onClick={() => setAction('disable')}>
                {t('auDisable')}
              </Button>
            ) : (
              <Button variant="brand" icon="play" onClick={() => setAction('enable')}>
                {t('auEnable')}
              </Button>
            )}
          </div>
        </div>
        {locked && (
          <div className="mt-5 flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
            <Icon name="lock" className="mt-0.5 size-4 shrink-0" />
            <p>
              {t('auLockedUntil')} <span className="font-bold">{formatDateTime(user.lockedUntil, lang)}</span>
            </p>
          </div>
        )}
      </Card>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card title={t('auProfileTitle')}>
          <DefinitionList
            items={[
              { label: t('username'), value: user.username, ltr: true },
              { label: t('phoneNumber'), value: user.phoneNumber ?? '—', ltr: true },
              { label: t('auLastLogin'), value: formatDateTime(user.lastLoginAt, lang) },
              { label: t('createdAt'), value: formatDateTime(user.createdAt ?? null, lang) },
              { label: t('onDuty'), value: user.onDuty ? t('yes') : t('no') },
              { label: t('auMfa'), value: user.mfaEnabled ? t('auMfaOn') : t('auMfaOff') },
            ]}
          />
        </Card>

        <Card
          title={t('auRoles')}
          description={t('auRolesCopy')}
          action={
            <Button variant="secondary" size="sm" icon="edit" onClick={() => setEditing(true)}>
              {t('edit')}
            </Button>
          }
        >
          {user.roles.length === 0 ? (
            <EmptyState icon="key" title={t('auNoRoles')} />
          ) : (
            <div className="flex flex-wrap gap-2">
              {user.roles.map((role) => (
                <Link key={role.id} to={`/roles/${role.id}`} className="rounded-full focus-visible:outline-2 focus-visible:outline-brand">
                  <Badge tone="ink" className="gap-1.5">
                    <span>{role.name || role.code}</span>
                    <span className="ltr-nums opacity-60">{role.code}</span>
                  </Badge>
                </Link>
              ))}
            </div>
          )}
        </Card>
      </div>

      <Card
        className="mt-6"
        title={t('asSessionsTitle')}
        description={t('auSessionsCopy')}
        action={
          <Button variant="danger-outline" size="sm" icon="logout" onClick={() => setAction('revokeSessions')}>
            {t('auRevokeSessions')}
          </Button>
        }
      >
        {!embedded && sessions.loading && !sessions.data ? (
          <PageSpinner />
        ) : sessionsUnsupported ? (
          <p className="text-sm text-muted">{t('auSessionsUnavailable')}</p>
        ) : !embedded && sessions.error ? (
          <ErrorState error={sessions.error} onRetry={sessions.reload} />
        ) : (
          <SessionsList
            sessions={sessionRows.map((item) => ({ ...item, current: isSelf ? item.current : false }))}
            // Per-session revoke only exists with the (assumed) list endpoint; otherwise "revoke all" above.
            onRevoke={embedded ? undefined : async (session) => {
              try {
                await adminUsers.revokeSession(user.id, session.id)
                toast.success(t('ssRevoked'))
                sessions.reload()
              } catch (error) {
                toast.error(t('errorTitle'), describe(error))
              }
            }}
          />
        )}
      </Card>

      <AdminUserFormModal
        open={editing}
        user={user}
        roles={roleList.data ?? []}
        rolesError={roleList.error ? describe(roleList.error) : null}
        onClose={() => setEditing(false)}
        onUpdated={() => {
          setEditing(false)
          toast.success(t('auUpdated'), t('auRolesChangeNote'))
          query.reload()
          sessions.reload()
        }}
      />

      <ConfirmModal
        open={copy !== null}
        title={copy ? t(copy.title) : ''}
        description={copy ? t(copy.copy) : undefined}
        confirmLabel={copy ? t(copy.confirm) : ''}
        confirmVariant={copy?.danger ? 'danger' : 'brand'}
        onClose={() => setAction(null)}
        onConfirm={async () => {
          if (action) await run(action)
        }}
      />

      <SecretRevealModal
        open={temporaryPassword !== null}
        title={t('auResetPasswordDoneTitle')}
        description={`${user.fullName ?? ''} · ${user.username}`}
        secret={temporaryPassword ?? ''}
        onClose={() => setTemporaryPassword(null)}
      />
    </>
  )
}
