import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import logo from '../assets/logo.png'
import { Action, Button } from './Button'
import { Icon, type IconName } from './Icon'
import { Keypad } from './Keypad'
import { LanguageToggle } from './LanguageToggle'
import { Notice } from './Notice'
import { OtpBoxes } from './OtpBoxes'
import { useI18n } from '../i18n'
import { authApi, isApiError } from '../lib/api'
import { describeError } from '../lib/errors'
import { formatPhone, isValidLocalPhone, normalizeLocalPhone } from '../lib/format'
import { getDeviceId } from '../lib/session'
import type { AuthResponse, OtpRequestResponse, Role } from '../lib/types'

const OTP_LENGTH = 4
const APP_VERSION = '1.0.0'

type Stage = 'phone' | 'otp'

export interface OtpLoginProps {
  role: Role
  /** Show the "session expired" notice on the phone step. */
  expired?: boolean
  eyebrow: string
  title: string
  copy: string
  icon?: IconName
  /** Where the header back link goes on the phone step (default: home). */
  backTo?: string
  backLabel?: string
  /**
   * Called with the verified session. Return an error message to stay on the
   * form (e.g. the account lacks the required role), or null when handled.
   */
  onVerified: (response: AuthResponse) => string | null
}

/** Phone (+966, keypad) → 4-digit OTP flow shared by the driver and corporate portals. */
export function OtpLogin({ role, expired = false, eyebrow, title, copy, icon = 'phone', backTo = '/', backLabel, onVerified }: OtpLoginProps) {
  const { t, lang } = useI18n()

  const [stage, setStage] = useState<Stage>('phone')
  const [phone, setPhone] = useState('')
  const [otp, setOtp] = useState('')
  const [otpRequest, setOtpRequest] = useState<OtpRequestResponse | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)
  const [attemptsLeft, setAttemptsLeft] = useState<number | null>(null)
  const [resendIn, setResendIn] = useState(0)

  useEffect(() => {
    if (resendIn <= 0) return
    const timer = window.setInterval(() => setResendIn((current) => Math.max(0, current - 1)), 1000)
    return () => window.clearInterval(timer)
  }, [resendIn])

  const phoneValid = isValidLocalPhone(phone)
  const e164 = `+966${phone}`

  const requestCode = async () => {
    if (!phoneValid || busy) return
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      const response = await authApi.requestOtp({ phoneNumber: e164, role, language: lang })
      setOtpRequest(response)
      setOtp('')
      setAttemptsLeft(null)
      setResendIn(response.resendAfterSeconds)
      if (stage === 'otp') setInfo(t('login.otp.resent'))
      setStage('otp')
    } catch (caught) {
      setError(describeError(caught, t))
      if (isApiError(caught) && caught.code === 'rate_limited') {
        const retry = caught.detailNumber('retryAfterSeconds')
        if (retry !== null) setResendIn(retry)
      }
    } finally {
      setBusy(false)
    }
  }

  const verifyCode = async () => {
    if (!otpRequest || otp.length !== OTP_LENGTH || busy) return
    setBusy(true)
    setError(null)
    setInfo(null)
    try {
      const response = await authApi.verifyOtp({
        requestId: otpRequest.requestId,
        phoneNumber: otpRequest.phoneNumber,
        code: otp,
        role,
        device: {
          deviceId: getDeviceId(),
          platform: 'web',
          deviceName: navigator.userAgent.slice(0, 200),
          appVersion: APP_VERSION,
        },
      })
      const rejection = onVerified(response)
      if (rejection) {
        setError(rejection)
        setOtp('')
      }
    } catch (caught) {
      setError(describeError(caught, t))
      if (isApiError(caught)) {
        setAttemptsLeft(caught.detailNumber('attemptsLeft'))
        if (caught.code === 'otp_invalid') setOtp('')
      }
    } finally {
      setBusy(false)
    }
  }

  const goBack = () => {
    setStage('phone')
    setOtp('')
    setError(null)
    setInfo(null)
    setAttemptsLeft(null)
  }

  return (
    <main className="relative min-h-screen overflow-hidden bg-canvas text-ink">
      <div className="pointer-events-none absolute -end-28 -top-28 size-96 rounded-full bg-brand/10" />
      <div className="pointer-events-none absolute -bottom-32 -start-24 size-96 rounded-full bg-ink/5" />

      <header className="relative z-10 mx-auto flex h-20 max-w-6xl items-center justify-between px-4 sm:px-5 lg:px-10">
        <Link to="/" aria-label="ATA">
          <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
        </Link>
        <div className="flex items-center gap-3">
          <LanguageToggle />
          {stage === 'otp' ? (
            <Action onClick={goBack} className="flex items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-bold shadow-soft">
              <Icon name="arrow" className="size-4 rtl:rotate-180" />
              {t('action.back')}
            </Action>
          ) : (
            <Link to={backTo} className="flex items-center gap-2 rounded-full bg-white px-4 py-2 text-sm font-bold shadow-soft">
              <Icon name="arrow" className="size-4 rtl:rotate-180" />
              <span className="hidden sm:inline">{backLabel ?? t('login.backHome')}</span>
              <span className="sm:hidden">{t('action.back')}</span>
            </Link>
          )}
        </div>
      </header>

      <div className="app-shell relative z-10 mx-auto flex max-w-6xl items-center justify-center px-4 pb-10 sm:px-5">
        {stage === 'phone' && (
          <form
            className="w-full max-w-md rounded-3xl bg-white p-6 shadow-panel sm:p-8"
            onSubmit={(event) => {
              event.preventDefault()
              void requestCode()
            }}
          >
            <div className="mb-7 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name={icon} className="size-7" />
            </div>
            <p className="mb-2 text-sm font-bold text-brand">{eyebrow}</p>
            <h1 className="text-3xl font-bold">{title}</h1>
            <p className="mt-3 leading-7 text-muted">{copy}</p>

            {expired && (
              <Notice tone="info" className="mt-5">
                {t('session.expired')}
              </Notice>
            )}

            <div className="my-7 flex h-16 items-center overflow-hidden rounded-2xl border-2 border-brand bg-white shadow-brand" dir="ltr">
              <div className="grid h-full place-items-center border-r border-line bg-cloud px-4 font-bold">+966</div>
              <input
                aria-label={t('login.phone.label')}
                autoComplete="tel-national"
                autoFocus
                className="h-full min-w-0 flex-1 bg-transparent px-4 text-lg font-bold tracking-wider outline-none placeholder:text-line"
                inputMode="numeric"
                onChange={(event) => setPhone(normalizeLocalPhone(event.target.value))}
                placeholder={t('login.phone.placeholder')}
                type="tel"
                value={phone}
              />
            </div>

            <Keypad
              disabled={busy}
              onDigit={(digit) => setPhone((current) => (current.length < 9 ? current + digit : current))}
              onDelete={() => setPhone((current) => current.slice(0, -1))}
            />

            {phone.length === 9 && !phoneValid && (
              <p className="mt-4 text-center text-xs font-bold text-danger" role="alert">
                {t('login.phone.invalid')}
              </p>
            )}
            {error && (
              <Notice tone="error" className="mt-5">
                {error}
              </Notice>
            )}

            <Button type="submit" block className="mt-7" disabled={!phoneValid || busy || resendIn > 0}>
              {busy ? t('login.phone.sending') : resendIn > 0 ? t('login.otp.resendIn', { n: resendIn }) : t('login.phone.send')}
            </Button>
            <p className="mt-5 text-center text-xs leading-6 text-muted">
              {t('login.terms')}{' '}
              <span className="inline-flex gap-2 font-bold">
                <Link to="/terms" className="text-brand hover:underline">
                  {t('footer.terms')}
                </Link>
                ·
                <Link to="/privacy" className="text-brand hover:underline">
                  {t('footer.privacy')}
                </Link>
              </span>
            </p>
          </form>
        )}

        {stage === 'otp' && otpRequest && (
          <form
            className="w-full max-w-md rounded-3xl bg-white p-6 text-center shadow-panel sm:p-8"
            onSubmit={(event) => {
              event.preventDefault()
              void verifyCode()
            }}
          >
            <div className="mx-auto mb-6 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand">
              <Icon name="shield" className="size-7" />
            </div>
            <h1 className="text-3xl font-bold">{t('login.otp.title')}</h1>
            <p className="mt-3 leading-7 text-muted">
              {t('login.otp.copy')}{' '}
              <span dir="ltr" className="font-bold text-ink">
                {formatPhone(phone)}
              </span>
            </p>

            <div className="my-7">
              <OtpBoxes value={otp} onChange={setOtp} length={OTP_LENGTH} disabled={busy} autoFocus onComplete={() => void verifyCode()} />
            </div>

            {otpRequest.devCode && (
              <Action
                onClick={() => setOtp(otpRequest.devCode ?? '')}
                className="mb-6 inline-flex items-center gap-2 rounded-full bg-cloud px-4 py-2 text-xs font-bold text-muted hover:bg-brand-soft hover:text-brand"
              >
                <Icon name="document" className="size-4" />
                <span dir="auto">{t('login.otp.devCode', { code: otpRequest.devCode })}</span>
              </Action>
            )}

            <Keypad
              disabled={busy}
              onDigit={(digit) => setOtp((current) => (current.length < OTP_LENGTH ? current + digit : current))}
              onDelete={() => setOtp((current) => current.slice(0, -1))}
            />

            {error && (
              <Notice tone="error" className="mt-5 text-start">
                {error}
                {attemptsLeft !== null && <span className="block text-xs">{t('login.otp.attemptsLeft', { n: attemptsLeft })}</span>}
              </Notice>
            )}
            {info && (
              <Notice tone="success" className="mt-5 text-start">
                {info}
              </Notice>
            )}

            <Button type="submit" block className="mt-7" disabled={otp.length !== OTP_LENGTH || busy}>
              {busy ? t('login.otp.verifying') : t('login.otp.verify')}
            </Button>

            <div className="mt-5 flex flex-col items-center gap-2">
              <Action
                disabled={busy || resendIn > 0}
                onClick={() => void requestCode()}
                className="text-sm font-bold text-brand disabled:text-muted"
              >
                {resendIn > 0 ? t('login.otp.resendIn', { n: resendIn }) : t('login.otp.resend')}
              </Action>
              <p className="text-xs text-muted">{t('login.otp.expiresNote')}</p>
            </div>
          </form>
        )}
      </div>
    </main>
  )
}
