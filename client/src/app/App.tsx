import { BrowserRouter, Route, Routes } from 'react-router-dom'
import MainLayout from '@/app/MainLayout'
import AdminRoute from '@/features/auth/AdminRoute'
import AuthProvider from '@/features/auth/AuthProvider'
import ChatPage from '@/features/chat/ChatPage'
import LoginPage from '@/features/auth/LoginPage'
import UserRoute from '@/features/auth/UserRoute'
import ProtectedRoute from '@/features/auth/ProtectedRoute'
import HomePage from '@/features/dashboard/HomePage'
import TicketsPage from '@/features/tickets/TicketsPage'
import KnowledgePage from '@/features/knowledge/KnowledgePage'
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
              <Route element={<UserRoute />}>
                <Route path="/chat" element={<ChatPage />} />
                <Route path="/tickets" element={<TicketsPage />} />
              </Route>
              <Route element={<AdminRoute />}>
                <Route path="/users" element={<UsersPage />} />
                <Route path="/knowledge" element={<KnowledgePage />} />
              </Route>
            </Route>
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
