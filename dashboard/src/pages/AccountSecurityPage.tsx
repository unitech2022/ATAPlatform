import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Badge } from '../components/Badge'
import { Button } from '../components/Button'
import { Card } from '../components/Card'
import { ConfirmModal } from '../components/ConfirmModal'
import { ErrorState } from '../components/ErrorState'
import { Icon } from '../components/Icon'
import { MfaEnrollmentPanel } from '../components/MfaEnrollmentPanel'
import { Modal } from '../components/Modal'
import { OtpInput } from '../components/OtpInput'
import { PageHeader } from '../components/PageHeader'
import { PasswordChangeForm } from '../components/PasswordChangeForm'
import { RecoveryCodesPanel } from '../components/RecoveryCodesPanel'
import { SessionsList } from '../components/SessionsList'
import { PageSpinner } from '../components/Spinner'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useQuery } from '../hooks/useQuery'
import { me as meApi } from '../lib/admin'
import { isApiError } from '../lib/api'
import { ALL_PERMISSIONS, TOTP_LENGTH } from '../lib/rbac'
import type { AdminSession, MfaEnrollment } from '../lib/types'

/** My account security (`/account/security`, docs/12 §F20.9): password, MFA + recovery codes, active sessions. */
export function AccountSecurityPage() {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const { me, user, refreshMe, logout } = useAuth()
  const sessions = useQuery(() => meApi.sessions(), 'me-sessions')
  const [regenerating, setRegenerating] = useState(false)
  const [enrolling, setEnrolling] = useState<MfaEnrollment | null>(null)
  const [startingEnroll, setStartingEnroll] = useState(false)
  const [codes, setCodes] = useState<string[] | null>(null)
  const [revokeOthers, setRevokeOthers] = useState(false)

  const name = me?.fullName?.trim() || user?.fullName?.trim() || me?.username || t('admin')
  const fullAccess = me?.permissions.includes(ALL_PERMISSIONS) ?? false

  const revoke = async (session: AdminSession) => {
    try {
      await meApi.revokeSession(session.id)
      if (session.current) {
        await logout()
        navigate('/login', { replace: true })
        return
      }
      toast.success(t('ssRevoked'))
      sessions.reload()
    } catch (error) {
      toast.error(t('errorTitle'), describe(error))
    }
  }

  const startEnrollment = async () => {
    setStartingEnroll(true)
    try {
      setEnrolling(await meApi.startEnrollment())
    } catch (error) {
      toast.error(t('errorTitle'), isApiError(error) && error.status === 404 ? t('asEnrollAtLogin') : describe(error))
    } finally {
      setStartingEnroll(false)
    }
  }

  return (
    <>
      <PageHeader title={t('asTitle')} description={t('asCopy')} />

      <Card className="mb-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 items-center gap-4">
            <span className="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="user" className="size-7" />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-xl font-bold">{name}</h2>
              {me?.username && <p className="ltr-nums truncate text-sm text-muted">{me.username}</p>}
              <div className="mt-2 flex flex-wrap gap-2">
                {me?.roles.map((role) => (
                  <Badge key={role.id} tone="ink">
                    {role.name || role.code}
                  </Badge>
                ))}
                {fullAccess && <Badge tone="brand">{t('asFullAccess')}</Badge>}
              </div>
            </div>
          </div>
          {me && (
            <Badge tone={me.mfaEnabled ? 'brand' : 'warning'} className="self-start sm:self-auto">
              {me.mfaEnabled ? t('asMfaOn') : t('asMfaOff')}
            </Badge>
          )}
        </div>
      </Card>

      <div className="grid gap-6 xl:grid-cols-2">
        <Card title={t('asPasswordTitle')} description={t('asPasswordCopy')}>
          <PasswordChangeForm
            onChanged={() => {
              toast.success(t('pwChanged'), t('pwChangedCopy'))
              sessions.reload()
            }}
          />
        </Card>

        <Card title={t('asMfaTitle')} description={t('asMfaCopy')}>
          {!me ? (
            <PageSpinner />
          ) : me.mfaEnabled ? (
            <div className="space-y-4">
              <div className="flex items-start gap-3 rounded-2xl bg-brand-soft p-4 text-sm text-ink">
                <Icon name="shield" className="mt-0.5 size-5 shrink-0 text-brand" />
                <p>{t('asMfaEnabledCopy')}</p>
              </div>
              <Button variant="secondary" icon="refresh" onClick={() => setRegenerating(true)}>
                {t('asRegenerateCodes')}
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex items-start gap-3 rounded-2xl bg-amber-50 p-4 text-sm text-amber-700">
                <Icon name="alert" className="mt-0.5 size-5 shrink-0" />
                <p>{t('asMfaDisabledCopy')}</p>
              </div>
              <Button icon="key" loading={startingEnroll} onClick={startEnrollment}>
                {t('asEnableMfa')}
              </Button>
            </div>
          )}
        </Card>
      </div>

      <Card
        className="mt-6"
        title={t('asSessionsTitle')}
        description={t('asSessionsCopy')}
        action={
          sessions.data && sessions.data.filter((item) => !item.current).length > 0 ? (
            <Button variant="danger-outline" size="sm" icon="logout" onClick={() => setRevokeOthers(true)}>
              {t('ssRevokeOthers')}
            </Button>
          ) : undefined
        }
      >
        {sessions.loading && !sessions.data ? (
          <PageSpinner />
        ) : sessions.error ? (
          <ErrorState error={sessions.error} onRetry={sessions.reload} />
        ) : (
          <SessionsList sessions={sessions.data ?? []} onRevoke={revoke} />
        )}
      </Card>

      {regenerating && (
        <RegenerateCodesModal
          onClose={() => setRegenerating(false)}
          onDone={(next) => {
            setRegenerating(false)
            setCodes(next)
          }}
        />
      )}

      <Modal open={enrolling !== null} title={t('mfEnrollTitle')} description={t('mfEnrollCopy')} size="lg" onClose={() => setEnrolling(null)}>
        {enrolling && (
          <EnrollInSession
            enrollment={enrolling}
            onDone={async (next) => {
              setEnrolling(null)
              setCodes(next)
              toast.success(t('asMfaEnabledToast'))
              await refreshMe()
            }}
          />
        )}
      </Modal>

      <Modal
        open={codes !== null}
        title={t('mfCodesTitle')}
        description={t('mfCodesCopy')}
        size="lg"
        onClose={() => setCodes(null)}
        footer={
          <Button icon="check" onClick={() => setCodes(null)}>
            {t('auSecretDone')}
          </Button>
        }
      >
        {codes && <RecoveryCodesPanel codes={codes} username={me?.username} />}
      </Modal>

      <ConfirmModal
        open={revokeOthers}
        title={t('ssRevokeOthers')}
        description={t('ssRevokeOthersCopy')}
        confirmLabel={t('ssRevokeOthers')}
        onClose={() => setRevokeOthers(false)}
        onConfirm={async () => {
          try {
            await meApi.revokeOtherSessions()
            toast.success(t('ssRevokedOthers'))
            setRevokeOthers(false)
            sessions.reload()
          } catch (error) {
            toast.error(t('errorTitle'), describe(error))
          }
        }}
      />
    </>
  )
}

function EnrollInSession({ enrollment, onDone }: { enrollment: MfaEnrollment; onDone: (codes: string[]) => Promise<void> }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [error, setError] = useState<string | null>(null)
  return (
    <MfaEnrollmentPanel
      enrollment={enrollment}
      error={error}
      onConfirm={async (code) => {
        setError(null)
        try {
          const result = await meApi.confirmEnrollment(code)
          await onDone(result.recoveryCodes ?? [])
        } catch (caught) {
          setError(isApiError(caught) && caught.code === 'mfa_invalid' ? t('mfInvalid') : describe(caught))
          throw caught
        }
      }}
    />
  )
}

/** `POST /admin/me/mfa/recovery-codes { code }` — needs a current TOTP code; replaces the old codes. */
function RegenerateCodesModal({ onClose, onDone }: { onClose: () => void; onDone: (codes: string[]) => void }) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [code, setCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const submit = async (value = code) => {
    if (value.length !== TOTP_LENGTH) {
      setError(t('mfCodeIncomplete'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const result = await meApi.regenerateRecoveryCodes(value)
      onDone(result.recoveryCodes ?? [])
    } catch (caught) {
      setCode('')
      setError(isApiError(caught) && caught.code === 'mfa_invalid' ? t('mfInvalid') : describe(caught))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      open
      title={t('asRegenerateCodes')}
      description={t('asRegenerateCopy')}
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={submitting}>
            {t('cancel')}
          </Button>
          <Button onClick={() => void submit()} loading={submitting}>
            {t('asRegenerateConfirm')}
          </Button>
        </>
      }
    >
      <p className="mb-3 text-center text-sm font-bold">{t('mfCodeLabel')}</p>
      <OtpInput idPrefix="regen-otp" value={code} onChange={setCode} onComplete={(value) => void submit(value)} invalid={Boolean(error)} disabled={submitting} autoFocus />
      {error && (
        <p role="alert" className="mt-3 text-center text-sm font-bold text-danger">
          {error}
        </p>
      )}
    </Modal>
  )
}
