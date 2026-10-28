import { getToken } from '@/shared/api/token'

export class ApiError extends Error {
  status: number

  constructor(status: number) {
    super(`Request failed with status ${status}`)
    this.status = status
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body) headers.set('Content-Type', 'application/json')

  const token = getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`/api${path}`, { ...init, headers })
  if (!response.ok) throw new ApiError(response.status)

  return response.json() as Promise<T>
}
