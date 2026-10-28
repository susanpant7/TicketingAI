import { BrowserRouter, Route, Routes } from 'react-router-dom'
import MainLayout from '@/app/MainLayout'
import AuthProvider from '@/features/auth/AuthProvider'
import LoginPage from '@/features/auth/LoginPage'
import ProtectedRoute from '@/features/auth/ProtectedRoute'
import HomePage from '@/features/dashboard/HomePage'
import TicketsPage from '@/features/tickets/TicketsPage'

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
            </Route>
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
