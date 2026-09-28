import { lazy, Suspense, type ComponentType } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { RequireAuth } from './components/RequireAuth'
import { RequireCorporateAuth } from './components/RequireCorporateAuth'
import { DriverLogin } from './pages/DriverLogin'
import { DriverPortal } from './pages/DriverPortal'
import { Landing } from './pages/Landing'
import { LoadingState } from './components/States'

/** Route-level code splitting for named page exports (Leaflet, Markdown and the portal load on demand). */
function page<K extends string>(load: () => Promise<Record<K, ComponentType>>, name: K) {
  return lazy(async () => ({ default: (await load())[name] }))
}

const BookingDetail = page(() => import('./pages/business/app/BookingDetail'), 'BookingDetail')
const Bookings = page(() => import('./pages/business/app/Bookings'), 'Bookings')
const CostCenters = page(() => import('./pages/business/app/CostCenters'), 'CostCenters')
const Dashboard = page(() => import('./pages/business/app/Dashboard'), 'Dashboard')
const EmployeeDetail = page(() => import('./pages/business/app/EmployeeDetail'), 'EmployeeDetail')
const Employees = page(() => import('./pages/business/app/Employees'), 'Employees')
const InvoiceDetail = page(() => import('./pages/business/app/InvoiceDetail'), 'InvoiceDetail')
const Invoices = page(() => import('./pages/business/app/Invoices'), 'Invoices')
const NewBooking = page(() => import('./pages/business/app/NewBooking'), 'NewBooking')
const Policies = page(() => import('./pages/business/app/Policies'), 'Policies')
const Reports = page(() => import('./pages/business/app/Reports'), 'Reports')
const Settings = page(() => import('./pages/business/app/Settings'), 'Settings')
const BusinessJoin = page(() => import('./pages/business/BusinessJoin'), 'BusinessJoin')
const BusinessLanding = page(() => import('./pages/business/BusinessLanding'), 'BusinessLanding')
const BusinessLayout = page(() => import('./pages/business/BusinessLayout'), 'BusinessLayout')
const BusinessLogin = page(() => import('./pages/business/BusinessLogin'), 'BusinessLogin')
const HelpArticle = page(() => import('./pages/help/HelpArticle'), 'HelpArticle')
const HelpCategory = page(() => import('./pages/help/HelpCategory'), 'HelpCategory')
const HelpHome = page(() => import('./pages/help/HelpHome'), 'HelpHome')
const TripShare = page(() => import('./pages/TripShare'), 'TripShare')
const PrivacyPolicy = page(() => import('./pages/legal/LegalPage'), 'PrivacyPolicy')
const TermsOfUse = page(() => import('./pages/legal/LegalPage'), 'TermsOfUse')

export default function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<LoadingState className="min-h-screen" />}>
      <Routes>
        <Route path="/" element={<Landing />} />

        <Route path="/driver" element={<DriverLogin />} />
        <Route
          path="/driver/portal"
          element={
            <RequireAuth>
              <DriverPortal />
            </RequireAuth>
          }
        />

        <Route path="/t/:token" element={<TripShare />} />

        <Route path="/help" element={<HelpHome />} />
        <Route path="/help/categories/:categoryId" element={<HelpCategory />} />
        <Route path="/help/:slug" element={<HelpArticle />} />

        <Route path="/privacy" element={<PrivacyPolicy />} />
        <Route path="/terms" element={<TermsOfUse />} />

        <Route path="/business" element={<BusinessLanding />} />
        <Route path="/business/login" element={<BusinessLogin />} />
        <Route path="/business/join/:token" element={<BusinessJoin />} />
        <Route
          path="/business/app"
          element={
            <RequireCorporateAuth>
              <BusinessLayout />
            </RequireCorporateAuth>
          }
        >
          <Route index element={<Dashboard />} />
          <Route path="bookings" element={<Bookings />} />
          <Route path="bookings/new" element={<NewBooking />} />
          <Route path="bookings/:tripId" element={<BookingDetail />} />
          <Route path="employees" element={<Employees />} />
          <Route path="employees/:id" element={<EmployeeDetail />} />
          <Route path="policies" element={<Policies />} />
          <Route path="cost-centers" element={<CostCenters />} />
          <Route path="invoices" element={<Invoices />} />
          <Route path="invoices/:id" element={<InvoiceDetail />} />
          <Route path="reports" element={<Reports />} />
          <Route path="settings" element={<Settings />} />
          <Route path="*" element={<Navigate to="/business/app" replace />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
