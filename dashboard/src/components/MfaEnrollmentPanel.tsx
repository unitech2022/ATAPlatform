import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'
import { copyText, groupSecret, TOTP_LENGTH } from '../lib/rbac'
import type { MfaEnrollment } from '../lib/types'
import { Button } from './Button'
import { OtpInput } from './OtpInput'
import { Spinner } from './Spinner'

/** QR image of the `otpauth://` URI (rendered locally with `qrcode`; the secret never leaves the browser). */
function OtpauthQr({ uri }: { uri: string }) {
  const { t } = useLang()
  const [result, setResult] = useState<{ uri: string; src: string | null } | null>(null)

  useEffect(() => {
    let active = true
    // Loaded on demand so the login bundle stays small; only the enrollment step needs it.
    import('qrcode')
      .then((module) => module.default.toDataURL(uri, { width: 208, margin: 1, errorCorrectionLevel: 'M', color: { dark: '#123650', light: '#ffffff' } }))
      .then((src) => {
        if (active) setResult({ uri, src })
      })
      .catch(() => {
        if (active) setResult({ uri, src: null })
      })
    return () => {
      active = false
    }
  }, [uri])

  const current = result?.uri === uri ? result : null
  return (
    <div className="grid size-52 shrink-0 place-items-center rounded-2xl border border-line bg-white p-2">
      {!current ? (
        <Spinner className="size-7 text-brand" />
      ) : current.src ? (
        <img src={current.src} alt={t('mfQrAlt')} className="size-48" data-testid="mfa-qr" />
      ) : (
        <p className="px-3 text-center text-xs text-muted">{t('mfQrFailed')}</p>
      )}
    </div>
  )
}

/**
 * Enrollment step: scan the QR (or type the secret), then confirm with the first 6-digit code.
 * `onConfirm` rejects with the API error; the caller shows it through `error`.
 */
export function MfaEnrollmentPanel({
  enrollment,
  onConfirm,
  error,
  footer,
}: {
  enrollment: MfaEnrollment
  onConfirm: (code: string) => Promise<void>
  error?: ReactNode
  footer?: ReactNode
}) {
  const { t } = useLang()
  const toast = useToast()
  const [code, setCode] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [localError, setLocalError] = useState<string | null>(null)

  const submit = async (event?: FormEvent) => {
    event?.preventDefault()
    if (code.length !== TOTP_LENGTH) {
      setLocalError(t('mfCodeIncomplete'))
      return
    }
    setLocalError(null)
    setSubmitting(true)
    try {
      await onConfirm(code)
    } catch {
      setCode('')
    } finally {
      setSubmitting(false)
    }
  }

  const copySecret = async () => {
    if (await copyText(enrollment.secret)) toast.success(t('mfSecretCopied'))
  }

  const shownError = localError ?? error

  return (
    <form onSubmit={submit} noValidate>
      <ol className="mb-5 list-decimal space-y-1 ps-5 text-sm text-muted">
        <li>{t('mfEnrollStep1')}</li>
        <li>{t('mfEnrollStep2')}</li>
        <li>{t('mfEnrollStep3')}</li>
      </ol>
      <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-start">
        <OtpauthQr uri={enrollment.otpauthUri} />
        <div className="w-full min-w-0">
          <p className="text-sm font-bold">{t('mfManualSecret')}</p>
          <p className="mt-1 text-xs text-muted">{t('mfManualSecretCopy')}</p>
          <p dir="ltr" data-testid="mfa-secret" className="ltr-nums mt-2 break-all rounded-2xl bg-cloud px-3 py-2 font-mono text-sm font-bold tracking-wider">
            {groupSecret(enrollment.secret)}
          </p>
          <Button variant="secondary" size="sm" icon="copy" className="mt-2" onClick={copySecret}>
            {t('mfCopySecret')}
          </Button>
        </div>
      </div>
      <div className="mt-6">
        <p className="mb-3 text-center text-sm font-bold">{t('mfConfirmCode')}</p>
        <OtpInput idPrefix="enroll-otp" value={code} onChange={setCode} invalid={Boolean(shownError)} disabled={submitting} />
        {shownError && (
          <p role="alert" className="mt-3 text-center text-sm font-bold text-danger">
            {shownError}
          </p>
        )}
      </div>
      <Button type="submit" size="lg" className="mt-6 w-full" loading={submitting}>
        {t('mfEnrollConfirm')}
      </Button>
      {footer}
    </form>
  )
}
