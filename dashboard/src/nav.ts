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
  // F15 — ratings sit with the operational screens.
  { key: 'navRatings', icon: 'star', to: '/ratings', isActive: (pathname) => pathname === '/ratings' },
  { key: 'navRatingFlags', icon: 'flag', to: '/ratings/flags', isActive: (pathname) => pathname.startsWith('/ratings/flags') },
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

/** F11 — finance group ("المالية"). */
export const FINANCE_ITEMS: NavItem[] = [
  { key: 'navPayments', icon: 'card', to: '/payments', isActive: (pathname) => pathname.startsWith('/payments') },
  { key: 'navRefunds', icon: 'receipt', to: '/refunds', isActive: (pathname) => pathname.startsWith('/refunds') },
  { key: 'navPayouts', icon: 'upload', to: '/payouts', isActive: (pathname) => pathname.startsWith('/payouts') },
  { key: 'navPayoutBatches', icon: 'bank', to: '/payout-batches', isActive: (pathname) => pathname.startsWith('/payout-batches') },
  { key: 'navSettlements', icon: 'document', to: '/settlements', isActive: (pathname) => pathname.startsWith('/settlements') },
  { key: 'navWallets', icon: 'wallet', to: '/wallets', isActive: (pathname) => pathname.startsWith('/wallets') },
  { key: 'navLedger', icon: 'book', to: '/ledger', isActive: (pathname) => pathname.startsWith('/ledger') },
]

/** F13 — notifications group ("الإشعارات"). */
export const NOTIFICATION_ITEMS: NavItem[] = [
  {
    key: 'navTemplates',
    icon: 'edit',
    to: '/notifications/templates',
    isActive: (pathname) => pathname.startsWith('/notifications/templates'),
  },
  {
    key: 'navCampaigns',
    icon: 'send',
    to: '/notifications/campaigns',
    isActive: (pathname) => pathname.startsWith('/notifications/campaigns'),
  },
  {
    key: 'navDeliveries',
    icon: 'bell',
    to: '/notifications/deliveries',
    isActive: (pathname) => pathname.startsWith('/notifications/deliveries'),
  },
]

const tabOf = (search: string) => new URLSearchParams(search).get('tab') ?? ''

/** F12 — safety group ("السلامة"). */
export const SAFETY_ITEMS: NavItem[] = [
  {
    key: 'navSafetyCases',
    icon: 'siren',
    to: '/safety',
    isActive: (pathname) => pathname === '/safety' || pathname.startsWith('/safety/cases'),
  },
  { key: 'navSafetyAlerts', icon: 'bell', to: '/safety/alerts', isActive: (pathname) => pathname.startsWith('/safety/alerts') },
  { key: 'navLostItems', icon: 'box', to: '/lost-items', isActive: (pathname) => pathname.startsWith('/lost-items') },
]

/** F14 — cancellation & reliability group ("الإلغاء والموثوقية"). */
export const CANCELLATION_ITEMS: NavItem[] = [
  { key: 'navCancellationEvents', icon: 'activity', to: '/cancellation/events', isActive: (pathname) => pathname.startsWith('/cancellation/events') },
  { key: 'navExcuses', icon: 'clock', to: '/cancellation/excuses', isActive: (pathname) => pathname.startsWith('/cancellation/excuses') },
  { key: 'navCancellationReasons', icon: 'list', to: '/cancellation/reasons', isActive: (pathname) => pathname.startsWith('/cancellation/reasons') },
  { key: 'navCancellationRules', icon: 'sliders', to: '/cancellation/rules', isActive: (pathname) => pathname.startsWith('/cancellation/rules') },
  {
    key: 'navReliability',
    icon: 'gauge',
    to: '/reliability',
    isActive: (pathname, search) => pathname.startsWith('/reliability') && tabOf(search) !== 'thresholds',
  },
  {
    key: 'navReliabilityThresholds',
    icon: 'layers',
    to: '/reliability?tab=thresholds',
    isActive: (pathname, search) => pathname === '/reliability' && tabOf(search) === 'thresholds',
  },
]

/** F15 — marketing & loyalty group ("التسويق والولاء"). */
export const MARKETING_ITEMS: NavItem[] = [
  { key: 'navPromotions', icon: 'gift', to: '/promotions', isActive: (pathname) => pathname.startsWith('/promotions') },
  { key: 'navIncentives', icon: 'target', to: '/incentives', isActive: (pathname) => pathname.startsWith('/incentives') },
  { key: 'navDriverTiers', icon: 'trophy', to: '/driver-tiers', isActive: (pathname) => pathname.startsWith('/driver-tiers') },
  // F16 — favorite driver discount rules and stats.
  { key: 'navFavorites', icon: 'heart', to: '/favorites', isActive: (pathname) => pathname.startsWith('/favorites') },
]

export interface NavGroup {
  /** Heading key; omitted for the ungrouped top section. */
  key?: TranslationKey
  items: NavItem[]
}

export const NAV_GROUPS: NavGroup[] = [
  { items: NAV_ITEMS },
  { key: 'navGroupSafety', items: SAFETY_ITEMS },
  { key: 'navGroupCancellation', items: CANCELLATION_ITEMS },
  { key: 'navGroupPricingOps', items: PRICING_OPS_ITEMS },
  { key: 'navGroupMarketing', items: MARKETING_ITEMS },
  { key: 'navGroupFinance', items: FINANCE_ITEMS },
  { key: 'navGroupNotifications', items: NOTIFICATION_ITEMS },
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
  if (/^\/payments\/[^/]+/.test(pathname)) return 'paymentDetailTitle'
  if (pathname.startsWith('/payments')) return 'paymentsTitle'
  if (pathname.startsWith('/refunds')) return 'refundsTitle'
  if (/^\/payout-batches\/[^/]+/.test(pathname)) return 'payoutBatchDetailTitle'
  if (pathname.startsWith('/payout-batches')) return 'payoutBatchesTitle'
  if (pathname.startsWith('/payouts')) return 'payoutsTitle'
  if (/^\/settlements\/[^/]+/.test(pathname)) return 'settlementDetailTitle'
  if (pathname.startsWith('/settlements')) return 'settlementsTitle'
  if (/^\/wallets\/[^/]+/.test(pathname)) return 'walletDetailTitle'
  if (pathname.startsWith('/wallets')) return 'walletsTitle'
  if (pathname.startsWith('/ledger')) return 'ledgerTitle'
  if (pathname.startsWith('/notifications/templates')) return 'templatesTitle'
  if (/^\/notifications\/campaigns\/[^/]+/.test(pathname)) return 'campaignDetailTitle'
  if (pathname.startsWith('/notifications/campaigns')) return 'campaignsTitle'
  if (pathname.startsWith('/notifications/deliveries')) return 'deliveriesTitle'
  if (/^\/safety\/cases\/[^/]+/.test(pathname)) return 'sfCaseDetailTitle'
  if (pathname.startsWith('/safety/alerts')) return 'sfAlertsTitle'
  if (pathname.startsWith('/safety')) return 'sfCenterTitle'
  if (pathname.startsWith('/lost-items')) return 'liTitle'
  if (pathname.startsWith('/cancellation/reasons')) return 'cxReasonsTitle'
  if (pathname.startsWith('/cancellation/rules')) return 'cxRulesTitle'
  if (pathname.startsWith('/cancellation/excuses')) return 'cxExcusesTitle'
  if (pathname.startsWith('/cancellation')) return 'cxEventsTitle'
  if (pathname.startsWith('/ratings/flags')) return 'rtFlagsTitle'
  if (pathname.startsWith('/ratings')) return 'rtTitle'
  if (/^\/promotions\/[^/]+/.test(pathname)) return 'prDetailTitle'
  if (pathname.startsWith('/promotions')) return 'prTitle'
  if (pathname.startsWith('/driver-tiers')) return 'tierTitle'
  if (/^\/incentives\/[^/]+/.test(pathname)) return 'icDetailTitle'
  if (pathname.startsWith('/incentives')) return 'icTitle'
  if (pathname.startsWith('/favorites')) return 'fvTitle'
  if (/^\/reliability\/[^/]+/.test(pathname)) return 'rlProfileTitle'
  if (pathname.startsWith('/reliability')) return 'rlTitle'
  return 'appName'
}
