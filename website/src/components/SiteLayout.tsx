import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { useI18n } from '../i18n'
import { Footer } from './Footer'
import { Header, type NavItem } from './Header'
import { Icon } from './Icon'

interface SiteLayoutProps {
  children: ReactNode
  /** Extra header actions; defaults to the driver-portal pill. */
  actions?: ReactNode
  nav?: NavItem[]
}

/** Public page chrome (help center, business landing, legal pages): header + footer. */
export function SiteLayout({ children, actions, nav }: SiteLayoutProps) {
  const { t } = useI18n()
  const items = nav ?? [
    { label: t('nav.home'), to: '/' },
    { label: t('nav.help'), to: '/help' },
    { label: t('nav.business'), to: '/business' },
    { label: t('nav.driverPortal'), to: '/driver' },
  ]
  return (
    <div className="flex min-h-screen flex-col bg-canvas text-ink">
      <Header
        nav={items}
        actions={
          actions ?? (
            <Link
              to="/driver"
              className="inline-flex items-center gap-2 rounded-full bg-ink px-4 py-2.5 text-sm font-bold text-white shadow-soft hover:bg-ink-soft"
            >
              <Icon name="car" className="size-4" />
              <span className="hidden sm:inline">{t('nav.driverPortal')}</span>
            </Link>
          )
        }
      />
      <main className="flex-1">{children}</main>
      <Footer />
    </div>
  )
}
