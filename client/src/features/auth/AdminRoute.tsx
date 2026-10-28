import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'

export default function AdminRoute() {
  const { user } = useAuth()
  return user?.role === 'Admin' ? <Outlet /> : <Navigate to="/" replace />
}
