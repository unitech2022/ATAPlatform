import { useState } from 'react'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { adminUsers } from '../lib/admin'
import { isApiError } from '../lib/api'
import { conflictReasonKey, generateTemporaryPassword, passwordMeetsPolicy, roleLabel } from '../lib/rbac'
import type { AdminUser, AdminUserCreateResult, Role } from '../lib/types'
import { Button } from './Button'
import { ChipGroup } from './ChipGroup'
import { Input } from './Field'
import { ChoiceField } from './FormSection'
import { Icon } from './Icon'
import { Modal } from './Modal'
import { PasswordPolicyHints } from './PasswordChangeForm'

const USERNAME = /^[a-zA-Z0-9._-]{3,50}$/
const E164 = /^\+[1-9]\d{7,14}$/

type Errors = Partial<Record<'username' | 'fullName' | 'phoneNumber' | 'roleIds' | 'temporaryPassword' | 'form', string>>

/**
 * Create (`POST /admin/admin-users`) or edit (`PUT /admin/admin-users/{id}` — full name + roles) an admin user.
 * A created user gets `must_change_password`; the temporary password is returned once and shown by the caller.
 */
export function AdminUserFormModal({
  open,
  user,
  roles,
  rolesError,
  onClose,
  onCreated,
  onUpdated,
}: {
  open: boolean
  user: AdminUser | null
  roles: Role[]
  rolesError?: string | null
  onClose: () => void
  onCreated?: (result: AdminUserCreateResult) => void
  onUpdated?: () => void
}) {
  // Mounted per opening so the fields start from the current user.
  return open ? <AdminUserForm user={user} roles={roles} rolesError={rolesError} onClose={onClose} onCreated={onCreated} onUpdated={onUpdated} /> : null
}

function AdminUserForm({
  user,
  roles,
  rolesError,
  onClose,
  onCreated,
  onUpdated,
}: {
  user: AdminUser | null
  roles: Role[]
  rolesError?: string | null
  onClose: () => void
  onCreated?: (result: AdminUserCreateResult) => void
  onUpdated?: () => void
}) {
  const { t, lang } = useLang()
  const describe = useApiErrorMessage()
  const editing = user !== null
  const [username, setUsername] = useState('')
  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [roleIds, setRoleIds] = useState<string[]>(user?.roles.map((role) => role.id) ?? [])
  const [temporaryPassword, setTemporaryPassword] = useState('')
  const [errors, setErrors] = useState<Errors>({})
  const [submitting, setSubmitting] = useState(false)

  const submit = async () => {
    const found: Errors = {}
    if (!editing && !USERNAME.test(username.trim())) found.username = t('auErrUsername')
    if (!fullName.trim()) found.fullName = t('auErrFullName')
    if (!editing && phoneNumber.trim() && !E164.test(phoneNumber.trim())) found.phoneNumber = t('auErrPhone')
    if (roleIds.length === 0) found.roleIds = t('auErrRoles')
    if (!editing && temporaryPassword && !passwordMeetsPolicy(temporaryPassword)) found.temporaryPassword = t('pwErrPolicy')
    setErrors(found)
    if (Object.keys(found).length > 0) return
    setSubmitting(true)
    try {
      if (editing) {
        await adminUsers.update(user.id, { fullName: fullName.trim(), roleIds })
        onUpdated?.()
      } else {
        const result = await adminUsers.create({
          username: username.trim(),
          fullName: fullName.trim(),
          phoneNumber: phoneNumber.trim() || null,
          roleIds,
          temporaryPassword: temporaryPassword || undefined,
        })
        onCreated?.(result)
      }
    } catch (error) {
      const reason = isApiError(error) ? conflictReasonKey(error.details) : null
      if (reason) setErrors({ roleIds: t(reason) })
      else if (isApiError(error) && error.code === 'password_policy_violation') setErrors({ temporaryPassword: t('pwErrPolicyServer') })
      else if (isApiError(error) && error.status === 409) setErrors({ form: `${t('auErrConflict')} ${error.message && error.message !== 'Conflict' ? `(${error.message})` : ''}`.trim() })
      else setErrors({ form: describe(error) })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      size="lg"
      title={editing ? t('auEditTitle') : t('auCreateTitle')}
      description={editing ? t('auEditCopy') : t('auCreateCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button onClick={submit} loading={submitting}>
            {editing ? t('save') : t('auCreate')}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        {!editing && (
          <Input
            id="au-username"
            label={t('username')}
            dir="ltr"
            autoComplete="off"
            value={username}
            error={errors.username}
            hint={t('auUsernameHint')}
            onChange={(event) => setUsername(event.target.value)}
          />
        )}
        <Input id="au-fullname" label={t('auFullName')} value={fullName} error={errors.fullName} onChange={(event) => setFullName(event.target.value)} />
        {!editing && (
          <Input
            id="au-phone"
            label={t('auPhoneOptional')}
            dir="ltr"
            inputMode="tel"
            placeholder="+9665XXXXXXXX"
            value={phoneNumber}
            error={errors.phoneNumber}
            hint={t('auPhoneHint')}
            onChange={(event) => setPhoneNumber(event.target.value)}
          />
        )}
        <ChoiceField label={t('auRoles')} error={errors.roleIds}>
          {rolesError ? (
            <p className="text-sm text-danger">{rolesError}</p>
          ) : (
            <ChipGroup options={roles.map((role) => ({ value: role.id, label: roleLabel(role, lang) }))} value={roleIds} onChange={setRoleIds} />
          )}
        </ChoiceField>
        {editing && <p className="text-xs text-muted">{t('auRolesChangeNote')}</p>}
        {!editing && (
          <div className="space-y-3">
            <div className="flex items-start gap-2">
              <Input
                id="au-temp-password"
                label={t('auTempPasswordOptional')}
                dir="ltr"
                autoComplete="new-password"
                value={temporaryPassword}
                error={errors.temporaryPassword}
                hint={t('auTempPasswordHint')}
                wrapperClassName="min-w-0 flex-1"
                onChange={(event) => setTemporaryPassword(event.target.value)}
              />
              <Button variant="secondary" icon="refresh" className="mt-[30px] h-12" onClick={() => setTemporaryPassword(generateTemporaryPassword())}>
                {t('auGenerate')}
              </Button>
            </div>
            {temporaryPassword && <PasswordPolicyHints value={temporaryPassword} />}
          </div>
        )}
        {errors.form && (
          <div role="alert" className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger">
            <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
            <span>{errors.form}</span>
          </div>
        )}
      </div>
    </Modal>
  )
}
