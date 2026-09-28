import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import logo from '../assets/logo.png'
import { Button } from '../components/Button'
import { Input } from '../components/Field'
import { Icon } from '../components/Icon'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useApiErrorMessage } from '../hooks/useApiErrorMessage'
import { isApiError } from '../lib/api'

interface LocationState {
  from?: string
}

export function LoginPage() {
  const { t, toggleLang } = useLang()
  const { login, isAuthenticated } = useAuth()
  const describe = useApiErrorMessage()
  const navigate = useNavigate()
  const location = useLocation()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const from = (location.state as LocationState | null)?.from ?? '/'

  if (isAuthenticated) return <Navigate to={from} replace />

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!username.trim() || !password) {
      setError(t('loginRequired'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await login(username.trim(), password)
      navigate(from, { replace: true })
    } catch (caught) {
      setError(isApiError(caught) && (caught.status === 401 || caught.status === 400) ? t('loginFailed') : describe(caught))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden px-4 py-10">
      <div className="pointer-events-none absolute -end-24 -top-24 size-72 rounded-full bg-brand/10" />
      <div className="pointer-events-none absolute -bottom-32 -start-20 size-80 rounded-full bg-ink/5" />

      <div className="relative w-full max-w-md">
        <div className="mb-6 flex items-center justify-between">
          <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
          <Button variant="secondary" size="sm" icon="globe" onClick={toggleLang} className="rounded-full">
            {t('switchLanguage')}
          </Button>
        </div>

        <div className="rounded-3xl bg-white p-6 shadow-soft sm:p-8">
          <div className="mb-7 grid size-14 place-items-center rounded-2xl bg-brand-soft text-brand">
            <Icon name="shield" className="size-7" />
          </div>
          <p className="mb-2 text-sm font-bold text-brand">{t('loginEyebrow')}</p>
          <h1 className="text-2xl font-bold leading-tight sm:text-3xl">{t('loginTitle')}</h1>
          <p className="mt-2 text-muted">{t('loginCopy')}</p>

          <form onSubmit={submit} className="mt-8 space-y-4" noValidate>
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
            {error && (
              <div role="alert" className="flex items-start gap-3 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger">
                <Icon name="alert" className="mt-0.5 size-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}
            <Button type="submit" size="lg" className="w-full" loading={submitting}>
              {t('loginButton')}
            </Button>
          </form>
        </div>

        <p className="mt-6 text-center text-xs text-muted">{t('loginFooter')}</p>
      </div>
    </main>
  )
}
