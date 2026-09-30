import { Link, useLocation } from 'react-router'
import logo from '../assets/logo.png'
import { useLang } from '../context/lang'
import { useAuth } from '../context/auth'
import { visibleNavGroups } from '../nav'
import { Icon } from './Icon'

export interface SidebarProps {
  open: boolean
  onClose: () => void
}

/** Ink sidebar: static on desktop, off-canvas drawer on smaller screens. */
export function Sidebar({ open, onClose }: SidebarProps) {
  const { t } = useLang()
  const { pathname, search } = useLocation()
  const { can } = useAuth()
  // F20: only the items the admin's permissions allow (`GET /admin/me`); the server still answers 403 for the rest.
  const groups = visibleNavGroups(can)

  return (
    <>
      {open && <button type="button" aria-label={t('closeMenu')} onClick={onClose} className="fixed inset-0 z-30 bg-ink/50 lg:hidden" />}
      <aside
        className={`fixed inset-y-0 start-0 z-40 flex w-72 max-w-[85vw] flex-col bg-ink text-white transition-transform duration-200 lg:sticky lg:top-0 lg:h-screen lg:max-w-none ${
          open ? 'translate-x-0 shadow-panel' : 'max-lg:ltr:-translate-x-full max-lg:rtl:translate-x-full'
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

        <nav aria-label={t('appName')} className="flex-1 overflow-y-auto px-3 py-4">
          {groups.map((group, index) => (
            <div key={group.key ?? `group-${index}`} className={index > 0 ? 'mt-5 border-t border-white/10 pt-4' : ''}>
              {group.key && <p className="mb-2 px-4 text-[11px] font-bold uppercase tracking-wide text-white/40">{t(group.key)}</p>}
              <div className="space-y-1">
                {group.items.map((item) => {
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
              </div>
            </div>
          ))}
        </nav>

        <div className="border-t border-white/10 px-5 py-4 text-xs text-white/50">
          <p className="font-bold text-white/70">ATA</p>
          <p className="ltr-nums">v0.1 · Step 1</p>
        </div>
      </aside>
    </>
  )
}
