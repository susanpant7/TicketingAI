import { createContext } from 'react'

export interface AuthUser {
  username: string
  role: string
}

export interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<AuthUser | null>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
