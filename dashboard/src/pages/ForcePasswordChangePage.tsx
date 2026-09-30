import logo from '../assets/logo.png'
import { Button } from '../components/Button'
import { Icon } from '../components/Icon'
import { PasswordChangeForm } from '../components/PasswordChangeForm'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { useToast } from '../context/toast'

/**
 * Forced password change (docs/12 §F20.4): shown instead of every page while the account carries a temporary
 * password (`mustChangePassword`, or any `403 password_change_required`). Only `/admin/me/password` works until then.
 */
export function ForcePasswordChangePage() {
  const { t, toggleLang } = useLang()
  const toast = useToast()
  const { me, user, passwordChanged, logout } = useAuth()
  const name = me?.fullName?.trim() || user?.fullName?.trim() || me?.username || ''

  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden px-4 py-10">
      <div className="pointer-events-none absolute -end-24 -top-24 size-72 rounded-full bg-brand/10" />
      <div className="relative w-full max-w-lg">
        <div className="mb-6 flex items-center justify-between">
          <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
          <Button variant="secondary" size="sm" icon="globe" onClick={toggleLang} className="rounded-full">
            {t('switchLanguage')}
          </Button>
        </div>
        <div className="rounded-3xl bg-white p-6 shadow-soft sm:p-8">
          <div className="mb-7 grid size-14 place-items-center rounded-2xl bg-amber-50 text-amber-700">
            <Icon name="lock" className="size-7" />
          </div>
          <p className="mb-2 text-sm font-bold text-brand">{name ? `${t('dashboardWelcome')} ${name}` : t('loginEyebrow')}</p>
          <h1 className="text-2xl font-bold leading-tight sm:text-3xl">{t('fpTitle')}</h1>
          <p className="mt-2 text-muted">{t('fpCopy')}</p>
          <div className="mt-8">
            <PasswordChangeForm
              submitLabel={t('fpSubmit')}
              onChanged={async () => {
                toast.success(t('pwChanged'), t('pwChangedCopy'))
                await passwordChanged()
              }}
            />
          </div>
          <button type="button" onClick={() => void logout()} className="mt-6 inline-flex items-center gap-2 text-sm font-bold text-muted hover:text-danger">
            <Icon name="logout" className="size-4 rtl:rotate-180" />
            {t('logout')}
          </button>
        </div>
      </div>
    </main>
  )
}
