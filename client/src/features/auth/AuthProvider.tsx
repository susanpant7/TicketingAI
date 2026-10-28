import { useState, type ReactNode } from 'react'
import { AuthContext, type AuthUser } from '@/features/auth/AuthContext'
import { loginRequest } from '@/features/auth/authApi'
import { ApiError } from '@/shared/api/apiClient'
import { clearToken, getToken, setToken } from '@/shared/api/token'

const USER_KEY = 'user'

function loadUser(): AuthUser | null {
  const stored = localStorage.getItem(USER_KEY)
  return stored && getToken() ? (JSON.parse(stored) as AuthUser) : null
}

export default function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadUser)

  async function login(username: string, password: string) {
    try {
      const { token, ...authUser } = await loginRequest(username, password)
      setToken(token)
      localStorage.setItem(USER_KEY, JSON.stringify(authUser))
      setUser(authUser)
      return true
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) return false
      throw error
    }
  }

  function logout() {
    clearToken()
    localStorage.removeItem(USER_KEY)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: user !== null, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}
