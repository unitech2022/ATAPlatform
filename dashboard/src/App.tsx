import { lazy, Suspense, type ComponentType, type ReactNode } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { PageSpinner } from './components/Spinner'
import { AuthProvider } from './context/AuthProvider'
import { LangProvider } from './context/LangProvider'
import { ToastProvider } from './context/ToastProvider'
import { AppLayout } from './layouts/AppLayout'
import { RequireAuth } from './layouts/RequireAuth'
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

/** Lazy routes of the finance (F11), notifications (F13), safety (F12), cancellation (F14), ratings/marketing (F15), favorites (F16) and scheduled rides/airports (F17) sections. */
const LAZY_ROUTES: { path: string; Page: ComponentType }[] = [
  { path: 'payments', Page: PaymentsPage },
  { path: 'payments/:id', Page: PaymentDetailPage },
  { path: 'refunds', Page: RefundsPage },
  { path: 'payouts', Page: PayoutsPage },
  { path: 'payout-batches', Page: PayoutBatchesPage },
  { path: 'payout-batches/:id', Page: PayoutBatchDetailPage },
  { path: 'settlements', Page: SettlementsPage },
  { path: 'settlements/:batchId', Page: SettlementBatchDetailPage },
  { path: 'wallets', Page: WalletsPage },
  { path: 'wallets/:id', Page: WalletDetailPage },
  { path: 'ledger', Page: LedgerPage },
  { path: 'notifications/templates', Page: NotificationTemplatesPage },
  { path: 'notifications/campaigns', Page: CampaignsPage },
  { path: 'notifications/campaigns/:id', Page: CampaignDetailPage },
  { path: 'notifications/deliveries', Page: NotificationDeliveriesPage },
  { path: 'safety', Page: SafetyCasesPage },
  { path: 'safety/cases/:id', Page: SafetyCaseDetailPage },
  { path: 'safety/alerts', Page: SafetyAlertsPage },
  { path: 'lost-items', Page: LostItemsPage },
  { path: 'cancellation/reasons', Page: CancellationReasonsPage },
  { path: 'cancellation/rules', Page: CancellationRulesPage },
  { path: 'cancellation/excuses', Page: CancellationExcusesPage },
  { path: 'cancellation/events', Page: CancellationEventsPage },
  { path: 'reliability', Page: ReliabilityPage },
  { path: 'reliability/:userId', Page: ReliabilityProfilePage },
  { path: 'ratings', Page: RatingsPage },
  { path: 'ratings/flags', Page: RatingFlagsPage },
  { path: 'promotions', Page: PromotionsPage },
  { path: 'promotions/:id', Page: PromotionDetailPage },
  { path: 'driver-tiers', Page: DriverTiersPage },
  { path: 'incentives', Page: IncentivesPage },
  { path: 'incentives/:id', Page: IncentiveDetailPage },
  { path: 'favorites', Page: FavoritesPage },
  { path: 'scheduled', Page: ScheduledTripsPage },
  { path: 'scheduled/rules', Page: ScheduledRulesPage },
  { path: 'airports', Page: AirportsPage },
  { path: 'airports/:id', Page: AirportDetailPage },
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
]

function Lazy({ children }: { children: ReactNode }) {
  return <Suspense fallback={<PageSpinner />}>{children}</Suspense>
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
                  <Route index element={<DashboardPage />} />
                  <Route path="drivers" element={<DriversPage />} />
                  <Route path="drivers/:id" element={<DriverDetailPage />} />
                  <Route path="passengers" element={<PassengersPage />} />
                  <Route path="trips" element={<TripsPage />} />
                  <Route
                    path="trips/:id"
                    element={
                      <Suspense fallback={<PageSpinner />}>
                        <TripDetailPage />
                      </Suspense>
                    }
                  />
                  <Route
                    path="live"
                    element={
                      <Suspense fallback={<PageSpinner />}>
                        <LiveMapPage />
                      </Suspense>
                    }
                  />
                  <Route path="ride-categories" element={<RideCategoriesPage />} />
                  <Route
                    path="zones"
                    element={
                      <Lazy>
                        <ZonesPage />
                      </Lazy>
                    }
                  />
                  <Route
                    path="pricing-rules"
                    element={
                      <Lazy>
                        <PricingRulesPage />
                      </Lazy>
                    }
                  />
                  <Route
                    path="demand"
                    element={
                      <Lazy>
                        <DemandPage />
                      </Lazy>
                    }
                  />
                  <Route
                    path="matching-settings"
                    element={
                      <Lazy>
                        <MatchingSettingsPage />
                      </Lazy>
                    }
                  />
                  {LAZY_ROUTES.map(({ path, Page }) => (
                    <Route
                      key={path}
                      path={path}
                      element={
                        <Lazy>
                          <Page />
                        </Lazy>
                      }
                    />
                  ))}
                  {ALIASES.map((alias) => (
                    <Route key={alias.from} path={alias.from} element={<Navigate to={alias.to} replace />} />
                  ))}
                  <Route path="audit-logs" element={<AuditLogsPage />} />
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
