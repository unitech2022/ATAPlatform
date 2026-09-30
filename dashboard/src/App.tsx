import { lazy, Suspense, type ComponentType, type ReactNode } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { PageSpinner } from './components/Spinner'
import { AuthProvider } from './context/AuthProvider'
import { LangProvider } from './context/LangProvider'
import { ToastProvider } from './context/ToastProvider'
import { AppLayout } from './layouts/AppLayout'
import { RequireAuth } from './layouts/RequireAuth'
import { HomeRoute, RequirePermission } from './layouts/RequirePermission'
import type { PermissionRequirement } from './context/auth'
import { AuditLogsPage } from './pages/AuditLogsPage'
import { DashboardPage } from './pages/DashboardPage'
import { DriverDetailPage } from './pages/DriverDetailPage'
import { DriversPage } from './pages/DriversPage'
import { LoginPage } from './pages/LoginPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { PassengersPage } from './pages/PassengersPage'
import { RideCategoriesPage } from './pages/RideCategoriesPage'
import { TripsPage } from './pages/TripsPage'

// Map pages pull in Leaflet + SignalR, so they load on demand.
const TripDetailPage = lazy(() => import('./pages/TripDetailPage').then((module) => ({ default: module.TripDetailPage })))
const LiveMapPage = lazy(() => import('./pages/LiveMapPage').then((module) => ({ default: module.LiveMapPage })))
const ZonesPage = lazy(() => import('./pages/ZonesPage').then((module) => ({ default: module.ZonesPage })))
const PricingRulesPage = lazy(() => import('./pages/PricingRulesPage').then((module) => ({ default: module.PricingRulesPage })))
const DemandPage = lazy(() => import('./pages/DemandPage').then((module) => ({ default: module.DemandPage })))
const MatchingSettingsPage = lazy(() => import('./pages/MatchingSettingsPage').then((module) => ({ default: module.MatchingSettingsPage })))
// F11 finance + F13 notifications
const PaymentsPage = lazy(() => import('./pages/PaymentsPage').then((module) => ({ default: module.PaymentsPage })))
const PaymentDetailPage = lazy(() => import('./pages/PaymentDetailPage').then((module) => ({ default: module.PaymentDetailPage })))
const RefundsPage = lazy(() => import('./pages/RefundsPage').then((module) => ({ default: module.RefundsPage })))
const PayoutsPage = lazy(() => import('./pages/PayoutsPage').then((module) => ({ default: module.PayoutsPage })))
const PayoutBatchesPage = lazy(() => import('./pages/PayoutBatchesPage').then((module) => ({ default: module.PayoutBatchesPage })))
const PayoutBatchDetailPage = lazy(() => import('./pages/PayoutBatchDetailPage').then((module) => ({ default: module.PayoutBatchDetailPage })))
const SettlementsPage = lazy(() => import('./pages/SettlementsPage').then((module) => ({ default: module.SettlementsPage })))
const SettlementBatchDetailPage = lazy(() => import('./pages/SettlementBatchDetailPage').then((module) => ({ default: module.SettlementBatchDetailPage })))
const WalletsPage = lazy(() => import('./pages/WalletsPage').then((module) => ({ default: module.WalletsPage })))
const WalletDetailPage = lazy(() => import('./pages/WalletDetailPage').then((module) => ({ default: module.WalletDetailPage })))
const LedgerPage = lazy(() => import('./pages/LedgerPage').then((module) => ({ default: module.LedgerPage })))
const NotificationTemplatesPage = lazy(() => import('./pages/NotificationTemplatesPage').then((module) => ({ default: module.NotificationTemplatesPage })))
const CampaignsPage = lazy(() => import('./pages/CampaignsPage').then((module) => ({ default: module.CampaignsPage })))
const CampaignDetailPage = lazy(() => import('./pages/CampaignDetailPage').then((module) => ({ default: module.CampaignDetailPage })))
const NotificationDeliveriesPage = lazy(() => import('./pages/NotificationDeliveriesPage').then((module) => ({ default: module.NotificationDeliveriesPage })))

// F12 safety + F14 cancellation & reliability
const SafetyCasesPage = lazy(() => import('./pages/SafetyCasesPage').then((module) => ({ default: module.SafetyCasesPage })))
const SafetyCaseDetailPage = lazy(() => import('./pages/SafetyCaseDetailPage').then((module) => ({ default: module.SafetyCaseDetailPage })))
const SafetyAlertsPage = lazy(() => import('./pages/SafetyAlertsPage').then((module) => ({ default: module.SafetyAlertsPage })))
const LostItemsPage = lazy(() => import('./pages/LostItemsPage').then((module) => ({ default: module.LostItemsPage })))
const CancellationReasonsPage = lazy(() => import('./pages/CancellationReasonsPage').then((module) => ({ default: module.CancellationReasonsPage })))
const CancellationRulesPage = lazy(() => import('./pages/CancellationRulesPage').then((module) => ({ default: module.CancellationRulesPage })))
const CancellationExcusesPage = lazy(() => import('./pages/CancellationExcusesPage').then((module) => ({ default: module.CancellationExcusesPage })))
const CancellationEventsPage = lazy(() => import('./pages/CancellationEventsPage').then((module) => ({ default: module.CancellationEventsPage })))
const ReliabilityPage = lazy(() => import('./pages/ReliabilityPage').then((module) => ({ default: module.ReliabilityPage })))
const ReliabilityProfilePage = lazy(() => import('./pages/ReliabilityProfilePage').then((module) => ({ default: module.ReliabilityProfilePage })))

// F15 ratings, promotions, driver tiers, incentives
const RatingsPage = lazy(() => import('./pages/RatingsPage').then((module) => ({ default: module.RatingsPage })))
const RatingFlagsPage = lazy(() => import('./pages/RatingFlagsPage').then((module) => ({ default: module.RatingFlagsPage })))
const PromotionsPage = lazy(() => import('./pages/PromotionsPage').then((module) => ({ default: module.PromotionsPage })))
const PromotionDetailPage = lazy(() => import('./pages/PromotionDetailPage').then((module) => ({ default: module.PromotionDetailPage })))
const DriverTiersPage = lazy(() => import('./pages/DriverTiersPage').then((module) => ({ default: module.DriverTiersPage })))
const IncentivesPage = lazy(() => import('./pages/IncentivesPage').then((module) => ({ default: module.IncentivesPage })))
const IncentiveDetailPage = lazy(() => import('./pages/IncentiveDetailPage').then((module) => ({ default: module.IncentiveDetailPage })))

// F16 favorite driver
const FavoritesPage = lazy(() => import('./pages/FavoritesPage').then((module) => ({ default: module.FavoritesPage })))

// F17 scheduled rides and airports
const ScheduledTripsPage = lazy(() => import('./pages/ScheduledTripsPage').then((module) => ({ default: module.ScheduledTripsPage })))
const ScheduledRulesPage = lazy(() => import('./pages/ScheduledRulesPage').then((module) => ({ default: module.ScheduledRulesPage })))
const AirportsPage = lazy(() => import('./pages/AirportsPage').then((module) => ({ default: module.AirportsPage })))
const AirportDetailPage = lazy(() => import('./pages/AirportDetailPage').then((module) => ({ default: module.AirportDetailPage })))

// F18 support and help center (the help-center editor pulls in react-markdown, so it stays lazy)
const SupportTicketsPage = lazy(() => import('./pages/SupportTicketsPage').then((module) => ({ default: module.SupportTicketsPage })))
const SupportTicketDetailPage = lazy(() => import('./pages/SupportTicketDetailPage').then((module) => ({ default: module.SupportTicketDetailPage })))
const SupportDisputesPage = lazy(() => import('./pages/SupportDisputesPage').then((module) => ({ default: module.SupportDisputesPage })))
const CannedResponsesPage = lazy(() => import('./pages/CannedResponsesPage').then((module) => ({ default: module.CannedResponsesPage })))
const SlaPoliciesPage = lazy(() => import('./pages/SlaPoliciesPage').then((module) => ({ default: module.SlaPoliciesPage })))
const HelpCenterPage = lazy(() => import('./pages/HelpCenterPage').then((module) => ({ default: module.HelpCenterPage })))

// F19 corporate accounts
const CorporateAccountsPage = lazy(() => import('./pages/CorporateAccountsPage').then((module) => ({ default: module.CorporateAccountsPage })))
const CorporateAccountDetailPage = lazy(() => import('./pages/CorporateAccountDetailPage').then((module) => ({ default: module.CorporateAccountDetailPage })))
const CorporateInvoicesPage = lazy(() => import('./pages/CorporateInvoicesPage').then((module) => ({ default: module.CorporateInvoicesPage })))

// F20 — reports (Recharts), admin users, roles & permissions, my account security
const ReportsPage = lazy(() => import('./pages/ReportsPage').then((module) => ({ default: module.ReportsPage })))
const ReportExportsPage = lazy(() => import('./pages/ReportExportsPage').then((module) => ({ default: module.ReportExportsPage })))
const AdminUsersPage = lazy(() => import('./pages/AdminUsersPage').then((module) => ({ default: module.AdminUsersPage })))
const AdminUserDetailPage = lazy(() => import('./pages/AdminUserDetailPage').then((module) => ({ default: module.AdminUserDetailPage })))
const RolesPage = lazy(() => import('./pages/RolesPage').then((module) => ({ default: module.RolesPage })))
const RoleDetailPage = lazy(() => import('./pages/RoleDetailPage').then((module) => ({ default: module.RoleDetailPage })))
const AccountSecurityPage = lazy(() => import('./pages/AccountSecurityPage').then((module) => ({ default: module.AccountSecurityPage })))

/** Lazy routes of the finance (F11), notifications (F13), safety (F12), cancellation (F14), ratings/marketing (F15), favorites (F16), scheduled rides/airports (F17) support/help center (F18) and corporate accounts (F19) sections. */
const LAZY_ROUTES: { path: string; permission?: PermissionRequirement; Page: ComponentType }[] = [
  { path: 'payments', permission: 'payments.view', Page: PaymentsPage },
  { path: 'payments/:id', permission: 'payments.view', Page: PaymentDetailPage },
  { path: 'refunds', permission: 'payments.view', Page: RefundsPage },
  { path: 'payouts', permission: 'payments.view', Page: PayoutsPage },
  { path: 'payout-batches', permission: 'payments.view', Page: PayoutBatchesPage },
  { path: 'payout-batches/:id', permission: 'payments.view', Page: PayoutBatchDetailPage },
  { path: 'settlements', permission: 'settlements.manage', Page: SettlementsPage },
  { path: 'settlements/:batchId', permission: 'settlements.manage', Page: SettlementBatchDetailPage },
  { path: 'wallets', permission: 'payments.view', Page: WalletsPage },
  { path: 'wallets/:id', permission: 'payments.view', Page: WalletDetailPage },
  { path: 'ledger', permission: 'payments.view', Page: LedgerPage },
  { path: 'notifications/templates', permission: 'notifications.view', Page: NotificationTemplatesPage },
  { path: 'notifications/campaigns', permission: 'notifications.manage', Page: CampaignsPage },
  { path: 'notifications/campaigns/:id', permission: 'notifications.manage', Page: CampaignDetailPage },
  { path: 'notifications/deliveries', permission: 'notifications.view', Page: NotificationDeliveriesPage },
  { path: 'safety', permission: 'safety.manage', Page: SafetyCasesPage },
  { path: 'safety/cases/:id', permission: 'safety.manage', Page: SafetyCaseDetailPage },
  { path: 'safety/alerts', permission: 'safety.manage', Page: SafetyAlertsPage },
  { path: 'lost-items', permission: 'safety.manage', Page: LostItemsPage },
  { path: 'cancellation/reasons', permission: 'cancellation.manage', Page: CancellationReasonsPage },
  { path: 'cancellation/rules', permission: 'cancellation.manage', Page: CancellationRulesPage },
  { path: 'cancellation/excuses', permission: 'cancellation.review', Page: CancellationExcusesPage },
  { path: 'cancellation/events', permission: 'trips.view', Page: CancellationEventsPage },
  { path: 'reliability', permission: ['reliability.manage', 'cancellation.manage'], Page: ReliabilityPage },
  { path: 'reliability/:userId', permission: 'reliability.manage', Page: ReliabilityProfilePage },
  { path: 'ratings', permission: 'ratings.manage', Page: RatingsPage },
  { path: 'ratings/flags', permission: 'ratings.manage', Page: RatingFlagsPage },
  { path: 'promotions', permission: 'promotions.manage', Page: PromotionsPage },
  { path: 'promotions/:id', permission: 'promotions.manage', Page: PromotionDetailPage },
  { path: 'driver-tiers', permission: 'incentives.manage', Page: DriverTiersPage },
  { path: 'incentives', permission: 'incentives.manage', Page: IncentivesPage },
  { path: 'incentives/:id', permission: 'incentives.manage', Page: IncentiveDetailPage },
  { path: 'favorites', permission: 'favorites.manage', Page: FavoritesPage },
  { path: 'scheduled', permission: 'scheduling.manage', Page: ScheduledTripsPage },
  { path: 'scheduled/rules', permission: 'scheduling.manage', Page: ScheduledRulesPage },
  { path: 'airports', permission: 'airport.manage', Page: AirportsPage },
  { path: 'airports/:id', permission: 'airport.manage', Page: AirportDetailPage },
  { path: 'support', permission: 'support.view', Page: SupportTicketsPage },
  { path: 'support/tickets/:id', permission: 'support.view', Page: SupportTicketDetailPage },
  { path: 'support/disputes', permission: 'support.view', Page: SupportDisputesPage },
  { path: 'support/canned-responses', permission: 'support.manage', Page: CannedResponsesPage },
  { path: 'support/sla', permission: 'support.manage', Page: SlaPoliciesPage },
  // Alias of `support/tickets/:id`; the static sub-pages above outrank it.
  { path: 'support/:id', permission: 'support.view', Page: SupportTicketDetailPage },
  { path: 'help-center', permission: 'help.manage', Page: HelpCenterPage },
  { path: 'corporate', permission: 'corporate.manage', Page: CorporateAccountsPage },
  // Static sub-page; outranks `corporate/:id`.
  { path: 'corporate/invoices', permission: 'corporate.manage', Page: CorporateInvoicesPage },
  { path: 'corporate/:id', permission: 'corporate.manage', Page: CorporateAccountDetailPage },
  // F20 reports, admin users, roles and the account page
  { path: 'reports', permission: 'reports.view', Page: ReportsPage },
  { path: 'reports/exports', permission: 'reports.export', Page: ReportExportsPage },
  { path: 'admin-users', permission: 'admin.users.manage', Page: AdminUsersPage },
  { path: 'admin-users/:id', permission: 'admin.users.manage', Page: AdminUserDetailPage },
  { path: 'roles', permission: 'admin.roles.manage', Page: RolesPage },
  // `roles/new` is handled by the detail page (create mode).
  { path: 'roles/:id', permission: 'admin.roles.manage', Page: RoleDetailPage },
  { path: 'account/security', Page: AccountSecurityPage },
]

/** Short aliases that redirect to the canonical routes from docs/08 §F13.10 and docs/09 "لوحة الإدارة". */
const ALIASES: { from: string; to: string }[] = [
  { from: 'notifications', to: '/notifications/templates' },
  { from: 'notification-templates', to: '/notifications/templates' },
  { from: 'campaigns', to: '/notifications/campaigns' },
  { from: 'notification-deliveries', to: '/notifications/deliveries' },
  { from: 'safety/cases', to: '/safety' },
  { from: 'safety/lost-items', to: '/lost-items' },
  { from: 'cancellation', to: '/cancellation/events' },
  { from: 'cancellations', to: '/cancellation/events' },
  { from: 'reliability-thresholds', to: '/reliability?tab=thresholds' },
  { from: 'rating-flags', to: '/ratings/flags' },
  { from: 'promo-codes', to: '/promotions' },
  { from: 'tiers', to: '/driver-tiers' },
  { from: 'account', to: '/account/security' },
  { from: 'reports/kpis', to: '/reports' },
  { from: 'admin/users', to: '/admin-users' },
]

function Lazy({ children }: { children: ReactNode }) {
  return <Suspense fallback={<PageSpinner />}>{children}</Suspense>
}

/** F20 — permission-guarded element (docs/12 §F20.9); the page's own `PermissionError` stays the server-side fallback. */
function guard(permission: PermissionRequirement, element: ReactNode, lazyLoad = false) {
  return <RequirePermission permission={permission}>{lazyLoad ? <Lazy>{element}</Lazy> : element}</RequirePermission>
}

export default function App() {
  return (
    <LangProvider>
      <ToastProvider>
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route element={<RequireAuth />}>
                <Route element={<AppLayout />}>
                  <Route
                    index
                    element={
                      <HomeRoute>
                        <DashboardPage />
                      </HomeRoute>
                    }
                  />
                  <Route path="drivers" element={guard('drivers.view', <DriversPage />)} />
                  <Route path="drivers/:id" element={guard('drivers.view', <DriverDetailPage />)} />
                  <Route path="passengers" element={guard('passengers.view', <PassengersPage />)} />
                  <Route path="trips" element={guard('trips.view', <TripsPage />)} />
                  <Route path="trips/:id" element={guard('trips.view', <TripDetailPage />, true)} />
                  <Route path="live" element={guard('live.view', <LiveMapPage />, true)} />
                  <Route path="ride-categories" element={<RideCategoriesPage />} />
                  <Route path="zones" element={guard('pricing.view', <ZonesPage />, true)} />
                  <Route path="pricing-rules" element={guard('pricing.view', <PricingRulesPage />, true)} />
                  <Route path="demand" element={guard('pricing.view', <DemandPage />, true)} />
                  <Route path="matching-settings" element={guard('pricing.view', <MatchingSettingsPage />, true)} />
                  {LAZY_ROUTES.map(({ path, permission, Page }) => (
                    <Route key={path} path={path} element={guard(permission, <Page />, true)} />
                  ))}
                  {ALIASES.map((alias) => (
                    <Route key={alias.from} path={alias.from} element={<Navigate to={alias.to} replace />} />
                  ))}
                  <Route path="audit-logs" element={guard('audit.view', <AuditLogsPage />)} />
                  <Route path="*" element={<NotFoundPage />} />
                </Route>
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </ToastProvider>
    </LangProvider>
  )
}
