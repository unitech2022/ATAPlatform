import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
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
                  <Route path="ride-categories" element={<RideCategoriesPage />} />
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
