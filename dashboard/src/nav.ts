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
  { key: 'navTrips', icon: 'route', to: '/trips', isActive: (pathname) => pathname.startsWith('/trips') },
  { key: 'navLiveMap', icon: 'map', to: '/live', isActive: (pathname) => pathname.startsWith('/live') },
  { key: 'navRideCategories', icon: 'layers', to: '/ride-categories', isActive: (pathname) => pathname.startsWith('/ride-categories') },
  { key: 'navAuditLogs', icon: 'list', to: '/audit-logs', isActive: (pathname) => pathname.startsWith('/audit-logs') },
]

/** F9/F10 — pricing & operations group, rendered under its own heading in the sidebar. */
export const PRICING_OPS_ITEMS: NavItem[] = [
  { key: 'navZones', icon: 'polygon', to: '/zones', isActive: (pathname) => pathname.startsWith('/zones') },
  { key: 'navPricingRules', icon: 'tag', to: '/pricing-rules', isActive: (pathname) => pathname.startsWith('/pricing-rules') },
  { key: 'navDemand', icon: 'activity', to: '/demand', isActive: (pathname) => pathname.startsWith('/demand') },
  { key: 'navMatchingSettings', icon: 'sliders', to: '/matching-settings', isActive: (pathname) => pathname.startsWith('/matching-settings') },
]

export interface NavGroup {
  /** Heading key; omitted for the ungrouped top section. */
  key?: TranslationKey
  items: NavItem[]
}

export const NAV_GROUPS: NavGroup[] = [
  { items: NAV_ITEMS },
  { key: 'navGroupPricingOps', items: PRICING_OPS_ITEMS },
]

/** Title shown in the top bar for the current path. */
export function pageTitleKey(pathname: string): TranslationKey {
  if (pathname === '/') return 'dashboardTitle'
  if (/^\/drivers\/[^/]+/.test(pathname)) return 'driverDetailTitle'
  if (pathname.startsWith('/drivers')) return 'driversTitle'
  if (pathname.startsWith('/passengers')) return 'passengersTitle'
  if (/^\/trips\/[^/]+/.test(pathname)) return 'tripDetailTitle'
  if (pathname.startsWith('/trips')) return 'tripsTitle'
  if (pathname.startsWith('/live')) return 'liveMapTitle'
  if (pathname.startsWith('/ride-categories')) return 'rideCategoriesTitle'
  if (pathname.startsWith('/audit-logs')) return 'auditLogsTitle'
  if (pathname.startsWith('/zones')) return 'zonesTitle'
  if (pathname.startsWith('/pricing-rules')) return 'pricingRulesTitle'
  if (pathname.startsWith('/demand')) return 'demandTitle'
  if (pathname.startsWith('/matching-settings')) return 'matchingSettingsTitle'
  return 'appName'
}
