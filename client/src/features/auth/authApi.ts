import { apiFetch } from '@/shared/api/apiClient'

export interface LoginResponse {
  username: string
  role: string
  token: string
}

export const loginRequest = (username: string, password: string) =>
  apiFetch<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
