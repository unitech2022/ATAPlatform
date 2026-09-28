import { useState } from 'react'
import { Outlet, useNavigate } from 'react-router'
import { Sidebar } from '../components/Sidebar'
import { Topbar } from '../components/Topbar'
import { useAuth } from '../context/auth'

export function AppLayout() {
  const [menuOpen, setMenuOpen] = useState(false)
  const { logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-h-screen overflow-x-clip">
      <Sidebar open={menuOpen} onClose={() => setMenuOpen(false)} />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar onOpenMenu={() => setMenuOpen(true)} onLogout={handleLogout} />
        <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6 sm:px-6 sm:py-8 lg:px-10">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
