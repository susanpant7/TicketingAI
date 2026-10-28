import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'

export default function UserRoute() {
  const { user } = useAuth()
  return user?.role === 'Admin' ? <Navigate to="/users" replace /> : <Outlet />
}
