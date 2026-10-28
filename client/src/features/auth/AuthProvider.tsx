import { useState, type ReactNode } from 'react'
import { AuthContext } from '@/features/auth/AuthContext'

const STORAGE_KEY = 'isAuthenticated'

export default function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(
    () => localStorage.getItem(STORAGE_KEY) === 'true',
  )

  function login(username: string, password: string) {
    const success = username === 'admin' && password === 'admin'
    if (success) localStorage.setItem(STORAGE_KEY, 'true')
    setIsAuthenticated(success)
    return success
  }

  function logout() {
    localStorage.removeItem(STORAGE_KEY)
    setIsAuthenticated(false)
  }

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}
