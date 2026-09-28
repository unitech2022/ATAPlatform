import { useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import logo from '../assets/logo.png'
import { useI18n } from '../i18n'
import { Action } from './Button'
import { Icon } from './Icon'
import { LanguageToggle } from './LanguageToggle'

export interface NavItem {
  label: string
  to: string
}

interface HeaderProps {
  title?: string
  nav?: NavItem[]
  actions?: ReactNode
}

function NavLink({ item, className, onClick }: { item: NavItem; className: string; onClick?: () => void }) {
  if (item.to.startsWith('#')) {
    return (
      <a href={item.to} className={className} onClick={onClick}>
        {item.label}
      </a>
    )
  }
  return (
    <Link to={item.to} className={className} onClick={onClick}>
      {item.label}
    </Link>
  )
}

export function Header({ title, nav = [], actions }: HeaderProps) {
  const { t } = useI18n()
  const [open, setOpen] = useState(false)

  return (
    <header className="relative z-30 border-b border-line bg-white shadow-soft">
      <div className="mx-auto flex h-20 max-w-7xl items-center justify-between gap-3 px-4 sm:px-5 lg:px-10">
        <div className="flex min-w-0 items-center gap-3">
          <Link to="/" className="shrink-0" aria-label="ATA">
            <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
          </Link>
          {title && (
            <>
              <div className="hidden h-8 border-e border-line sm:block" />
              <p className="hidden truncate font-bold sm:block">{title}</p>
            </>
          )}
        </div>

        {nav.length > 0 && (
          <nav className="hidden items-center gap-6 text-sm font-bold lg:flex xl:gap-8">
            {nav.map((item) => (
              <NavLink key={item.to} item={item} className="text-muted transition hover:text-ink" />
            ))}
          </nav>
        )}

        <div className="flex items-center gap-2 sm:gap-3">
          {/* With a nav, phones get the toggle inside the menu instead. */}
          <LanguageToggle className={nav.length > 0 ? 'hidden sm:flex' : 'flex'} />
          {actions}
          {nav.length > 0 && (
            <Action
              aria-label={t('nav.menu')}
              aria-expanded={open}
              onClick={() => setOpen((current) => !current)}
              className={`grid size-11 place-items-center rounded-full transition lg:hidden ${
                open ? 'bg-brand text-white' : 'bg-ink text-white'
              }`}
            >
              <Icon name="menu" />
            </Action>
          )}
        </div>
      </div>

      {open && nav.length > 0 && (
        <div className="absolute inset-x-4 top-[calc(100%+0.5rem)] rounded-3xl border border-line bg-white p-3 shadow-panel lg:hidden">
          {nav.map((item) => (
            <NavLink
              key={item.to}
              item={item}
              onClick={() => setOpen(false)}
              className="flex items-center gap-3 rounded-xl px-3 py-3 text-sm font-bold text-ink hover:bg-cloud"
            />
          ))}
          <div className="mt-2 flex items-center justify-between rounded-2xl bg-cloud p-3 sm:hidden">
            <span className="text-sm font-bold text-muted">{t('lang.label')}</span>
            <LanguageToggle className="bg-white" />
          </div>
        </div>
      )}
    </header>
  )
}
