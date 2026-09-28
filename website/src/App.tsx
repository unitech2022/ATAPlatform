import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { RequireAuth } from './components/RequireAuth'
import { DriverLogin } from './pages/DriverLogin'
import { DriverPortal } from './pages/DriverPortal'
import { Landing } from './pages/Landing'

export default function App() {
  return (
    <BrowserRouter>
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
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
