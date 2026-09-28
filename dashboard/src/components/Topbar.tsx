import { useLocation } from 'react-router'
import { useAuth } from '../context/auth'
import { useLang } from '../context/lang'
import { pageTitleKey } from '../nav'
import { Button } from './Button'
import { DutyToggle } from './DutyToggle'
import { Icon } from './Icon'

export interface TopbarProps {
  onOpenMenu: () => void
  onLogout: () => void
}

export function Topbar({ onOpenMenu, onLogout }: TopbarProps) {
  const { t, toggleLang } = useLang()
  const { user } = useAuth()
  const { pathname } = useLocation()
  const adminName = user?.fullName?.trim() || t('admin')

  return (
    <header className="sticky top-0 z-20 flex h-20 items-center justify-between gap-3 border-b border-line bg-white/95 px-4 shadow-soft backdrop-blur sm:px-6 lg:px-10">
      <div className="flex min-w-0 items-center gap-3">
        <button
          type="button"
          onClick={onOpenMenu}
          aria-label={t('openMenu')}
          className="grid size-11 shrink-0 place-items-center rounded-full bg-cloud text-ink lg:hidden"
        >
          <Icon name="menu" />
        </button>
        <h1 className="truncate text-lg font-bold sm:text-xl">{t(pageTitleKey(pathname))}</h1>
      </div>

      <div className="flex shrink-0 items-center gap-2 sm:gap-3">
        <DutyToggle />
        <Button variant="secondary" size="sm" icon="globe" onClick={toggleLang} className="rounded-full">
          {t('switchLanguage')}
        </Button>
        <div className="flex items-center gap-3 rounded-full bg-ink py-1.5 pe-4 ps-1.5 text-white">
          <span className="grid size-8 place-items-center rounded-full bg-brand">
            <Icon name="user" className="size-4" />
          </span>
          <span className="hidden max-w-40 truncate text-sm font-bold sm:block">{adminName}</span>
        </div>
        <button
          type="button"
          onClick={onLogout}
          aria-label={t('logout')}
          title={t('logout')}
          className="grid size-11 place-items-center rounded-full border border-line bg-white text-ink transition hover:bg-danger-soft hover:text-danger"
        >
          <Icon name="logout" className="size-5 rtl:rotate-180" />
        </button>
      </div>
    </header>
  )
}
