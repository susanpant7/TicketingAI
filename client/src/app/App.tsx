import { BrowserRouter, Route, Routes } from 'react-router-dom'
import MainLayout from '@/app/MainLayout'
import AdminRoute from '@/features/auth/AdminRoute'
import AuthProvider from '@/features/auth/AuthProvider'
import LoginPage from '@/features/auth/LoginPage'
import ProtectedRoute from '@/features/auth/ProtectedRoute'
import HomePage from '@/features/dashboard/HomePage'
import TicketsPage from '@/features/tickets/TicketsPage'
import UsersPage from '@/features/users/UsersPage'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<MainLayout />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route element={<ProtectedRoute />}>
              <Route path="/tickets" element={<TicketsPage />} />
              <Route element={<AdminRoute />}>
                <Route path="/users" element={<UsersPage />} />
              </Route>
            </Route>
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
