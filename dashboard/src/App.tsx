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

/** Lazy routes of the finance (F11) and notifications (F13) sections. */
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
]

/** Short aliases that redirect to the canonical routes from docs/08 §F13.10. */
const ALIASES: { from: string; to: string }[] = [
  { from: 'notifications', to: '/notifications/templates' },
  { from: 'notification-templates', to: '/notifications/templates' },
  { from: 'campaigns', to: '/notifications/campaigns' },
  { from: 'notification-deliveries', to: '/notifications/deliveries' },
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
