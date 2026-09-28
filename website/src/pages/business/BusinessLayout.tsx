import { Link, NavLink, Outlet } from 'react-router'
import logo from '../../assets/logo.png'
import { Button } from '../../components/Button'
import { Icon, type IconName } from '../../components/Icon'
import { LanguageToggle } from '../../components/LanguageToggle'
import { useI18n, type TranslationKey } from '../../i18n'
import { corporateApi } from '../../lib/api'
import { useBusinessAuth } from '../../lib/auth'
import { useResource } from '../../lib/useResource'

const items: { to: string; label: TranslationKey; icon: IconName; end?: boolean }[] = [
  { to: '/business/app', label: 'biz.nav.dashboard', icon: 'grid', end: true },
  { to: '/business/app/bookings', label: 'biz.nav.bookings', icon: 'car' },
  { to: '/business/app/employees', label: 'biz.nav.employees', icon: 'users' },
  { to: '/business/app/policies', label: 'biz.nav.policies', icon: 'shield' },
  { to: '/business/app/cost-centers', label: 'biz.nav.costCenters', icon: 'wallet' },
  { to: '/business/app/invoices', label: 'biz.nav.invoices', icon: 'receipt' },
  { to: '/business/app/reports', label: 'biz.nav.reports', icon: 'chart' },
  { to: '/business/app/settings', label: 'biz.nav.settings', icon: 'settings' },
]

/** Chrome for `/business/app/*`: header, sidebar (desktop) / scrollable tabs (mobile). */
export function BusinessLayout() {
  const { t, lang } = useI18n()
  const { session, logout } = useBusinessAuth()
  const account = useResource(() => corporateApi.account(), [lang])
  const company = account.data?.displayName ?? null

  return (
    <div className="min-h-screen bg-canvas text-ink">
      <meta name="robots" content="noindex" />
      <header className="sticky top-0 z-[1100] border-b border-line bg-white shadow-soft">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-3 px-4 sm:px-5 lg:px-8">
          <div className="flex min-w-0 items-center gap-3">
            <Link to="/business/app" aria-label="ATA" className="shrink-0">
              <img src={logo} alt="ATA" className="h-10 w-20 object-contain" />
            </Link>
            <div className="hidden h-8 border-e border-line sm:block" />
            <div className="hidden min-w-0 sm:block">
              <p className="truncate text-sm font-bold">{company ?? t('biz.portal')}</p>
              <p className="truncate text-xs text-muted" dir="auto">
                {session?.user.fullName ?? session?.user.phoneNumber}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <LanguageToggle />
            <Button variant="secondary" size="sm" className="rounded-full" onClick={() => void logout()}>
              <Icon name="user" className="size-4" />
              <span className="hidden sm:inline">{t('action.logout')}</span>
            </Button>
          </div>
        </div>
        <nav aria-label={t('biz.portal')} className="overflow-x-auto border-t border-line lg:hidden">
          <div className="flex w-max gap-1 px-3 py-2">
            {items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  `flex items-center gap-2 whitespace-nowrap rounded-full px-3 py-2 text-xs font-bold transition ${
                    isActive ? 'bg-ink text-white' : 'text-muted hover:bg-cloud hover:text-ink'
                  }`
                }
              >
                <Icon name={item.icon} className="size-4" />
                {t(item.label)}
              </NavLink>
            ))}
          </div>
        </nav>
      </header>

      <div className="mx-auto flex max-w-7xl gap-8 px-4 py-6 sm:px-5 lg:px-8 lg:py-8">
        <aside className="hidden w-60 shrink-0 lg:block">
          <nav aria-label={t('biz.portal')} className="sticky top-24 space-y-1 rounded-3xl bg-white p-3 shadow-soft">
            {items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  `flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-bold transition ${
                    isActive ? 'bg-ink text-white' : 'text-muted hover:bg-cloud hover:text-ink'
                  }`
                }
              >
                <Icon name={item.icon} className="size-5" />
                {t(item.label)}
              </NavLink>
            ))}
          </nav>
        </aside>
        <main className="min-w-0 flex-1">
          {account.data && account.data.status !== 'active' && (
            <div className="mb-6 rounded-2xl bg-danger-soft p-4 text-sm font-bold text-danger" role="alert">
              {t('biz.accountInactive', { status: t(`biz.accountStatus.${account.data.status}`) })}
            </div>
          )}
          <Outlet />
        </main>
      </div>
    </div>
  )
}
