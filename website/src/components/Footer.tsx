import { Link } from 'react-router'
import logo from '../assets/logo.png'
import { useI18n, type TranslationKey } from '../i18n'
import { SUPPORT_EMAIL, SUPPORT_PHONE, SUPPORT_PHONE_HREF } from '../lib/catalog'
import { currentYear } from '../lib/format'
import { Icon } from './Icon'

const links: { to: string; label: TranslationKey }[] = [
  { to: '/', label: 'nav.home' },
  { to: '/driver', label: 'nav.driverPortal' },
  { to: '/help', label: 'nav.help' },
  { to: '/business', label: 'nav.business' },
  { to: '/privacy', label: 'footer.privacy' },
  { to: '/terms', label: 'footer.terms' },
]

export function Footer() {
  const { t } = useI18n()
  const year = currentYear()

  return (
    <footer className="border-t border-line bg-white">
      <div className="mx-auto grid max-w-7xl gap-10 px-4 py-12 sm:px-5 lg:grid-cols-3 lg:px-10">
        <div>
          <img src={logo} alt="ATA" className="h-11 w-24 object-contain" />
          <p className="mt-4 max-w-sm leading-7 text-muted">{t('footer.tagline')}</p>
        </div>

        <div>
          <p className="mb-4 text-sm font-bold text-brand">{t('footer.contact')}</p>
          <div className="space-y-3">
            <a href={SUPPORT_PHONE_HREF} className="flex items-center gap-3 rounded-2xl border border-line p-3 hover:border-brand">
              <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand">
                <Icon name="phone" />
              </span>
              <span className="min-w-0">
                <span className="block font-bold" dir="ltr">
                  {SUPPORT_PHONE}
                </span>
                <span className="block text-xs text-muted">{t('footer.phoneNote')}</span>
              </span>
            </a>
            <a href={`mailto:${SUPPORT_EMAIL}`} className="flex items-center gap-3 rounded-2xl border border-line p-3 hover:border-brand">
              <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-brand-soft text-brand">
                <Icon name="document" />
              </span>
              <span className="min-w-0">
                <span className="block font-bold" dir="ltr">
                  {SUPPORT_EMAIL}
                </span>
                <span className="block text-xs text-muted">{t('footer.emailNote')}</span>
              </span>
            </a>
          </div>
        </div>

        <div>
          <p className="mb-4 text-sm font-bold text-brand">{t('footer.links')}</p>
          <ul className="grid grid-cols-2 gap-3 text-sm font-bold text-ink">
            {links.map((link) => (
              <li key={link.to}>
                <Link to={link.to} className="hover:text-brand">
                  {t(link.label)}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </div>
      <div className="border-t border-line">
        <p className="mx-auto max-w-7xl px-4 py-5 text-xs text-muted sm:px-5 lg:px-10">{t('footer.rights', { year })}</p>
      </div>
    </footer>
  )
}
