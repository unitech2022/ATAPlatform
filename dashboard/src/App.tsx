import { lazy, Suspense, type ReactNode } from 'react'
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
