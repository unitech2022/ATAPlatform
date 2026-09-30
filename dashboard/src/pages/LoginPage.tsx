import { useEffect, useEffectEvent, useState, type FormEvent, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import logo from '../assets/logo.png'
import { Button } from '../components/Button'
import { Input } from '../components/Field'
import { Icon, type IconName } from '../components/Icon'
import { MfaEnrollmentPanel } from '../components/MfaEnrollmentPanel'
import { OtpInput } from '../components/OtpInput'
import { RecoveryCodesPanel } from '../components/RecoveryCodesPanel'
import { PageSpinner } from '../components/Spinner'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { useNow } from '../hooks/useNow'
import { auth as authApi } from '../lib/admin'
import { isApiError } from '../lib/api'
import { formatCountdownSeconds, normalizeRecoveryCode, retryAfterOf, TOTP_LENGTH } from '../lib/rbac'
import type { AuthResponse, MfaEnrollment, MfaMethod } from '../lib/types'

interface LocationState {
  from?: string
}

type Step =
  | { kind: 'credentials' }
  | { kind: 'mfa'; mfaToken: string; methods: MfaMethod[] }
  | { kind: 'enroll'; mfaToken: string; enrollment: MfaEnrollment | null }
  | { kind: 'codes'; codes: string[]; auth: AuthResponse }

/** Error with an optional lock deadline (`429 account_locked` / `mfa_locked` → `retryAfterSeconds`). */
interface LoginError {
  message: string
  lockedUntil?: number | null
  /** Seconds announced by the server; caps the countdown so a stale clock tick never shows more. */
  lockSeconds?: number | null
}

/**
 * Admin sign-in (docs/12 §F20.5/§F20.9): username + password, then — when the server asks — the TOTP step
 * (6 digits or a recovery code) or the mandatory enrollment (QR + confirm + one-time recovery codes).
 * A temporary password continues to the forced change screen (RequireAuth).
 */
export function LoginPage() {
  const { t, toggleLang } = useLang()
  const { login, completeLogin, isAuthenticated } = useAuth()
  const location = useLocation()
  const [step, setStep] = useState<Step>({ kind: 'credentials' })
  const [error, setError] = useState<LoginError | null>(null)

  const from = (location.state as LocationState | null)?.from ?? '/'

  if (isAuthenticated) return <Navigate to={from} replace />

  const restart = (next: LoginError | null) => {
    setError(next)
    setStep({ kind: 'credentials' })
  }

  let body: ReactNode
  let icon: IconName = 'shield'
  let eyebrow = t('loginEyebrow')
  let title = t('loginTitle')
  let copy = t('loginCopy')

  if (step.kind === 'credentials') {
    body = (
      <CredentialsStep
        error={error}
        onError={setError}
        onSubmit={async (username, password) => {
          const outcome = await login(username, password)
          setError(null)
          if (outcome.kind === 'mfa') setStep({ kind: 'mfa', mfaToken: outcome.mfaToken, methods: outcome.methods })
          else if (outcome.kind === 'enroll') setStep({ kind: 'enroll', mfaToken: outcome.mfaToken, enrollment: null })
        }}
      />
    )
  } else if (step.kind === 'mfa') {
    icon = 'lock'
    eyebrow = t('mfEyebrow')
    title = t('mfTitle')
    copy = t('mfCopy')
    body = <MfaStep mfaToken={step.mfaToken} methods={step.methods} onRestart={restart} onDone={completeLogin} />
  } else if (step.kind === 'enroll') {
    icon = 'key'
    eyebrow = t('mfEyebrow')
    title = t('mfEnrollTitle')
    copy = t('mfEnrollCopy')
    body = (
      <EnrollStep
        mfaToken={step.mfaToken}
        enrollment={step.enrollment}
        onEnrollment={(enrollment) => setStep({ kind: 'enroll', mfaToken: step.mfaToken, enrollment })}
        onRestart={restart}
        onConfirmed={(codes, auth) => setStep({ kind: 'codes', codes, auth })}
      />
    )
  } else {
    icon = 'key'
    eyebrow = t('mfEyebrow')
    title = t('mfCodesTitle')
    copy = t('mfCodesCopy')
    body = <CodesStep codes={step.codes} username={step.auth.user?.fullName ?? null} onContinue={() => completeLogin(step.auth)} />
  }

  const wide = step.kind === 'enroll' || step.kind === 'codes'

  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden px-4 py-10">
      <div className="pointer-events-none absolute -end-24 -top-24 size-72 rounded-full bg-brand/10" />
      <div className="pointer-events-none absolute -bottom-32 -start-20 size-80 rounded-full bg-ink/5" />

      <div className={`relative w-full ${wide ? 'max-w-xl' : 'max-w-md'}`}>
        <div className="mb-6 flex items-center justify-between">
          <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
          <Button variant="secondary" size="sm" icon="globe" onClick={toggleLang} className="rounded-full">
            {t('switchLanguage')}
          </Button>
        </div>

        <div className="rounded-3xl bg-white p-6 shadow-soft sm:p-8">
          <div className="mb-7 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand">
            <Icon name={icon} className="size-7" />
          </div>
          <p className="mb-2 text-sm font-bold text-brand">{eyebrow}</p>
          <h1 className="text-2xl font-bold leading-tight sm:text-3xl">{title}</h1>
          <p className="mt-2 text-muted">{copy}</p>
          <div className="mt-8">{body}</div>
        </div>

        <p className="mt-6 text-center text-xs text-muted">{t('loginFooter')}</p>
      </div>
    </main>
  )
}

function ErrorBox({ error }: { error: LoginError | null }) {
  const { t } = useLang()
  const now = useNow(1000)
  if (!error) return null
  const remaining = error.lockedUntil ? Math.max(0, Math.min(error.lockSeconds ?? Infinity, Math.ceil((error.lockedUntil - now) / 1000))) : 0
  return (
    <div role="alert" className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger">
      <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
      <span>
        {error.message}
        {remaining > 0 && (
          <span className="mt-1 block font-normal">
            {t('lgRetryIn')} <span className="ltr-nums font-bold" data-testid="lock-countdown">{formatCountdownSeconds(remaining)}</span>
          </span>
        )}
      </span>
    </div>
  )
}

function lockOf(details: Record<string, unknown> | undefined): Pick<LoginError, 'lockedUntil' | 'lockSeconds'> {
  const seconds = retryAfterOf(details)
  return seconds ? { lockedUntil: Date.now() + seconds * 1000, lockSeconds: seconds } : {}
}

function CredentialsStep({
  error,
  onError,
  onSubmit,
}: {
  error: LoginError | null
  onError: (error: LoginError | null) => void
  onSubmit: (username: string, password: string) => Promise<void>
}) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const now = useNow(1000)
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const locked = Boolean(error?.lockedUntil && error.lockedUntil > now)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (locked) return
    if (!username.trim() || !password) {
      onError({ message: t('loginRequired') })
      return
    }
    setSubmitting(true)
    onError(null)
    try {
      await onSubmit(username.trim(), password)
    } catch (caught) {
      setPassword('')
      if (isApiError(caught) && caught.code === 'account_locked') {
        onError({ message: t('lgAccountLocked'), ...lockOf(caught.details) })
      } else if (isApiError(caught) && caught.code === 'account_suspended') {
        onError({ message: t('lgAccountSuspended') })
      } else if (isApiError(caught) && (caught.status === 401 || caught.status === 400)) {
        onError({ message: t('loginFailed') })
      } else {
        onError({ message: describe(caught) })
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <Input
        id="username"
        label={t('username')}
        placeholder={t('usernamePlaceholder')}
        autoComplete="username"
        dir="ltr"
        value={username}
        onChange={(event) => setUsername(event.target.value)}
      />
      <Input
        id="password"
        type="password"
        label={t('password')}
        placeholder={t('passwordPlaceholder')}
        autoComplete="current-password"
        dir="ltr"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
      />
      <ErrorBox error={error} />
      <Button type="submit" size="lg" className="w-full" loading={submitting} disabled={locked}>
        {t('loginButton')}
      </Button>
    </form>
  )
}

function MfaStep({
  mfaToken,
  methods,
  onRestart,
  onDone,
}: {
  mfaToken: string
  methods: MfaMethod[]
  onRestart: (error: LoginError | null) => void
  onDone: (auth: AuthResponse) => void
}) {
  const { t } = useLang()
  const toast = useToast()
  const describe = useApiErrorMessage()
  const allowRecovery = methods.includes('recovery_code')
  const [mode, setMode] = useState<'totp' | 'recovery'>(methods.includes('totp') || !allowRecovery ? 'totp' : 'recovery')
  const [code, setCode] = useState('')
  const [recovery, setRecovery] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const verify = async (totp = code) => {
    if (submitting) return
    if (mode === 'totp' && totp.length !== TOTP_LENGTH) {
      setError(t('mfCodeIncomplete'))
      return
    }
    if (mode === 'recovery' && !recovery.trim()) {
      setError(t('mfRecoveryRequired'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const auth = await authApi.verifyMfa(mfaToken, mode === 'totp' ? { code: totp } : { recoveryCode: normalizeRecoveryCode(recovery) })
      if (typeof auth.recoveryCodesRemaining === 'number' && mode === 'recovery') {
        toast.success(t('mfRecoveryUsed'), `${t('mfCodesRemaining')}: ${auth.recoveryCodesRemaining}`)
      }
      onDone(auth)
    } catch (caught) {
      setCode('')
      if (isApiError(caught) && caught.code === 'mfa_locked') {
        onRestart({ message: t('lgMfaLocked'), ...lockOf(caught.details) })
      } else if (isApiError(caught) && caught.status === 401) {
        onRestart({ message: t('lgMfaExpired') })
      } else if (isApiError(caught) && caught.code === 'mfa_invalid') {
        const left = caught.details?.attemptsLeft
        setError(typeof left === 'number' ? `${t('mfInvalid')} ${t('mfAttemptsLeft')}: ${left}` : t('mfInvalid'))
      } else {
        setError(describe(caught))
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form
      noValidate
      onSubmit={(event) => {
        event.preventDefault()
        void verify()
      }}
      className="space-y-5"
    >
      {mode === 'totp' ? (
        <div>
          <p className="mb-3 text-center text-sm font-bold">{t('mfCodeLabel')}</p>
          <OtpInput value={code} onChange={setCode} onComplete={(value) => void verify(value)} invalid={Boolean(error)} disabled={submitting} autoFocus />
        </div>
      ) : (
        <Input
          id="recovery-code"
          label={t('mfRecoveryLabel')}
          placeholder="abcd-efgh"
          autoComplete="off"
          dir="ltr"
          value={recovery}
          onChange={(event) => setRecovery(event.target.value)}
          autoFocus
        />
      )}
      {error && (
        <p role="alert" className="text-center text-sm font-bold text-danger">
          {error}
        </p>
      )}
      <Button type="submit" size="lg" className="w-full" loading={submitting}>
        {t('mfVerify')}
      </Button>
      <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
        {allowRecovery && methods.includes('totp') && (
          <button
            type="button"
            className="font-bold text-brand"
            onClick={() => {
              setMode(mode === 'totp' ? 'recovery' : 'totp')
              setError(null)
            }}
          >
            {mode === 'totp' ? t('mfUseRecovery') : t('mfUseTotp')}
          </button>
        )}
        <button type="button" className="font-bold text-muted" onClick={() => onRestart(null)}>
          {t('mfBackToLogin')}
        </button>
      </div>
    </form>
  )
}

function EnrollStep({
  mfaToken,
  enrollment,
  onEnrollment,
  onRestart,
  onConfirmed,
}: {
  mfaToken: string
  enrollment: MfaEnrollment | null
  onEnrollment: (enrollment: MfaEnrollment) => void
  onRestart: (error: LoginError | null) => void
  onConfirmed: (codes: string[], auth: AuthResponse) => void
}) {
  const { t } = useLang()
  const describe = useApiErrorMessage()
  const [error, setError] = useState<string | null>(null)
  const [startError, setStartError] = useState<string | null>(null)
  const [attempt, setAttempt] = useState(0)

  const onStarted = useEffectEvent((result: MfaEnrollment) => onEnrollment(result))
  const onStartFailed = useEffectEvent((caught: unknown) => {
    if (isApiError(caught) && caught.status === 401) onRestart({ message: t('lgMfaExpired') })
    else setStartError(describe(caught))
  })
  const waiting = enrollment === null

  useEffect(() => {
    if (!waiting) return
    let active = true
    authApi
      .startEnrollment(mfaToken)
      .then((result) => {
        if (active) onStarted(result)
      })
      .catch((caught: unknown) => {
        if (active) onStartFailed(caught)
      })
    return () => {
      active = false
    }
    // `attempt` re-runs the request after a failure.
  }, [mfaToken, waiting, attempt])

  if (!enrollment) {
    if (startError) {
      return (
        <div className="space-y-4">
          <ErrorBox error={{ message: startError }} />
          <div className="flex flex-wrap gap-2">
            <Button
              variant="secondary"
              icon="refresh"
              onClick={() => {
                setStartError(null)
                setAttempt((value) => value + 1)
              }}
            >
              {t('retry')}
            </Button>
            <Button variant="ghost" onClick={() => onRestart(null)}>
              {t('mfBackToLogin')}
            </Button>
          </div>
        </div>
      )
    }
    return <PageSpinner />
  }

  return (
    <MfaEnrollmentPanel
      enrollment={enrollment}
      error={error}
      onConfirm={async (code) => {
        setError(null)
        try {
          const result = await authApi.confirmEnrollment(mfaToken, code)
          onConfirmed(result.recoveryCodes ?? [], result.auth)
        } catch (caught) {
          if (isApiError(caught) && caught.code === 'mfa_locked') onRestart({ message: t('lgMfaLocked'), ...lockOf(caught.details) })
          else if (isApiError(caught) && caught.status === 401) onRestart({ message: t('lgMfaExpired') })
          else setError(isApiError(caught) && caught.code === 'mfa_invalid' ? t('mfInvalid') : describe(caught))
          throw caught
        }
      }}
      footer={
        <button type="button" className="mt-4 text-sm font-bold text-muted" onClick={() => onRestart(null)}>
          {t('mfBackToLogin')}
        </button>
      }
    />
  )
}

function CodesStep({ codes, username, onContinue }: { codes: string[]; username: string | null; onContinue: () => void }) {
  const { t } = useLang()
  const [saved, setSaved] = useState(false)
  return (
    <div>
      <RecoveryCodesPanel codes={codes} username={username} />
      <label className="mt-5 flex cursor-pointer items-start gap-3 rounded-2xl border border-line p-4 text-sm font-bold">
        <input type="checkbox" className="mt-1 size-4 accent-[var(--color-brand)]" checked={saved} onChange={(event) => setSaved(event.target.checked)} />
        <span>{t('mfCodesSavedConfirm')}</span>
      </label>
      <Button size="lg" className="mt-5 w-full" disabled={!saved} onClick={onContinue}>
        {t('mfContinue')}
      </Button>
    </div>
  )
}
