import { useEffect, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router'
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
  const { user, me } = useAuth()
  const { pathname } = useLocation()
  const adminName = me?.fullName?.trim() || user?.fullName?.trim() || me?.username || t('admin')

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
        <UserMenu name={adminName} onLogout={onLogout} />
      </div>
    </header>
  )
}

/** F20 — account menu: who is signed in, their roles, "My account security" and logout. */
function UserMenu({ name, onLogout }: { name: string; onLogout: () => void }) {
  const { t, lang } = useLang()
  const { me } = useAuth()
  const { pathname } = useLocation()
  const [open, setOpen] = useState(false)
  const [openedAt, setOpenedAt] = useState(pathname)
  const rootRef = useRef<HTMLDivElement>(null)

  // Navigating away closes the menu.
  if (open && openedAt !== pathname) {
    setOpen(false)
    setOpenedAt(pathname)
  }

  useEffect(() => {
    if (!open) return
    const onPointer = (event: PointerEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) setOpen(false)
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }
    document.addEventListener('pointerdown', onPointer)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('pointerdown', onPointer)
      document.removeEventListener('keydown', onKey)
    }
  }, [open])

  const roles = me?.roles?.map((role) => role.name || role.code).join(lang === 'ar' ? '، ' : ', ') ?? ''

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t('umOpen')}
        onClick={() => {
          setOpenedAt(pathname)
          setOpen((current) => !current)
        }}
        className="flex items-center gap-3 rounded-full bg-ink py-1.5 pe-1.5 ps-1.5 text-white sm:pe-3"
      >
        <span className="grid size-8 place-items-center rounded-full bg-brand">
          <Icon name="user" className="size-4" />
        </span>
        <span className="hidden max-w-40 truncate text-sm font-bold sm:block">{name}</span>
        <Icon name="chevron" className="hidden size-4 rotate-90 sm:block" />
      </button>

      {open && (
        <div role="menu" className="absolute end-0 top-full z-30 mt-2 w-72 max-w-[calc(100vw-2rem)] overflow-hidden rounded-2xl border border-line bg-white shadow-float">
          <div className="border-b border-line px-4 py-3">
            <p className="truncate font-bold">{name}</p>
            {me?.username && <p className="ltr-nums truncate text-xs text-muted">{me.username}</p>}
            {roles && <p className="mt-1 truncate text-xs text-muted">{roles}</p>}
          </div>
          <Link role="menuitem" to="/account/security" onClick={() => setOpen(false)} className="flex items-center gap-3 px-4 py-3 text-sm font-bold transition hover:bg-cloud">
            <Icon name="lock" className="size-4 text-brand" />
            {t('umAccountSecurity')}
          </Link>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false)
              onLogout()
            }}
            className="flex w-full items-center gap-3 border-t border-line px-4 py-3 text-start text-sm font-bold text-danger transition hover:bg-danger-soft"
          >
            <Icon name="logout" className="size-4 rtl:rotate-180" />
            {t('logout')}
          </button>
        </div>
      )}
    </div>
  )
}
