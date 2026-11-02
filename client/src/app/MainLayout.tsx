import { Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import ChatWidget from '@/features/chat/ChatWidget'
import Navbar from '@/shared/components/Navbar'
import ScrollToTop from '@/shared/components/ScrollToTop'

export default function MainLayout() {
  const { isAuthenticated } = useAuth()

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <Navbar />
      <Outlet />
      <ScrollToTop />
      {isAuthenticated && <ChatWidget />}
    </div>
  )
}
