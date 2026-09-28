import type { IconName } from './components/Icon'
import type { TranslationKey } from './i18n'

export interface NavItem {
  key: TranslationKey
  icon: IconName
  to: string
  /** Returns true when this item should be highlighted for the given location. */
  isActive: (pathname: string, search: string) => boolean
}

const statusOf = (search: string) => new URLSearchParams(search).get('status') ?? ''

export const NAV_ITEMS: NavItem[] = [
  { key: 'navDashboard', icon: 'home', to: '/', isActive: (pathname) => pathname === '/' },
  {
    key: 'navDriverApplications',
    icon: 'document',
    to: '/drivers',
    isActive: (pathname, search) => pathname.startsWith('/drivers') && statusOf(search) !== 'approved',
  },
  {
    key: 'navDrivers',
    icon: 'car',
    to: '/drivers?status=approved',
    isActive: (pathname, search) => pathname === '/drivers' && statusOf(search) === 'approved',
  },
  { key: 'navPassengers', icon: 'users', to: '/passengers', isActive: (pathname) => pathname.startsWith('/passengers') },
  { key: 'navRideCategories', icon: 'layers', to: '/ride-categories', isActive: (pathname) => pathname.startsWith('/ride-categories') },
  { key: 'navAuditLogs', icon: 'list', to: '/audit-logs', isActive: (pathname) => pathname.startsWith('/audit-logs') },
]

/** Title shown in the top bar for the current path. */
export function pageTitleKey(pathname: string): TranslationKey {
  if (pathname === '/') return 'dashboardTitle'
  if (/^\/drivers\/[^/]+/.test(pathname)) return 'driverDetailTitle'
  if (pathname.startsWith('/drivers')) return 'driversTitle'
  if (pathname.startsWith('/passengers')) return 'passengersTitle'
  if (pathname.startsWith('/ride-categories')) return 'rideCategoriesTitle'
  if (pathname.startsWith('/audit-logs')) return 'auditLogsTitle'
  return 'appName'
}
