import { BrowserRouter, Route, Routes } from 'react-router-dom'
import LoginPage from '@/features/auth/LoginPage'
import HomePage from '@/features/dashboard/HomePage'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
      </Routes>
    </BrowserRouter>
  )
}
