import { Link, useLocation } from 'react-router'
import logo from '../assets/logo.png'
import { useLang } from '../context/lang'
import { NAV_ITEMS } from '../nav'
import { Icon } from './Icon'

export interface SidebarProps {
  open: boolean
  onClose: () => void
}

/** Ink sidebar: static on desktop, off-canvas drawer on smaller screens. */
export function Sidebar({ open, onClose }: SidebarProps) {
  const { t } = useLang()
  const { pathname, search } = useLocation()

  return (
    <>
      {open && <button type="button" aria-label={t('closeMenu')} onClick={onClose} className="fixed inset-0 z-30 bg-ink/50 lg:hidden" />}
      <aside
        className={`fixed inset-y-0 start-0 z-40 flex w-72 max-w-[85vw] flex-col bg-ink text-white transition-transform duration-200 lg:sticky lg:top-0 lg:h-screen lg:max-w-none lg:translate-x-0 ${
          open ? 'translate-x-0 shadow-panel' : 'ltr:-translate-x-full rtl:translate-x-full'
        }`}
      >
        <div className="flex h-20 items-center justify-between gap-3 border-b border-white/10 px-5">
          <Link to="/" onClick={onClose} className="flex items-center gap-3">
            <span className="grid h-11 w-24 place-items-center rounded-xl bg-white px-2">
              <img src={logo} alt="ATA" className="h-9 w-full object-contain" />
            </span>
            <span className="text-sm font-bold text-white/80">{t('appName')}</span>
          </Link>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('closeMenu')}
            className="grid size-10 place-items-center rounded-full bg-white/10 lg:hidden"
          >
            <Icon name="x" className="size-4" />
          </button>
        </div>

        <nav className="flex-1 space-y-1 overflow-y-auto px-3 py-4">
          {NAV_ITEMS.map((item) => {
            const active = item.isActive(pathname, search)
            return (
              <Link
                key={item.key}
                to={item.to}
                onClick={onClose}
                aria-current={active ? 'page' : undefined}
                className={`flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-bold transition ${
                  active ? 'bg-brand text-white shadow-brand' : 'text-white/70 hover:bg-white/10 hover:text-white'
                }`}
              >
                <Icon name={item.icon} className="size-5 shrink-0" />
                <span className="truncate">{t(item.key)}</span>
              </Link>
            )
          })}
        </nav>

        <div className="border-t border-white/10 px-5 py-4 text-xs text-white/50">
          <p className="font-bold text-white/70">ATA</p>
          <p className="ltr-nums">v0.1 · Step 1</p>
        </div>
      </aside>
    </>
  )
}
