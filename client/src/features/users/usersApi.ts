import { apiFetch } from '@/shared/api/apiClient'

export interface User {
  id: number
  username: string
  fullName: string
  email: string
  departmentId: number
  departmentName: string
}

export interface Department {
  id: number
  name: string
}

export interface CreateUserInput {
  username: string
  fullName: string
  email: string
  password: string
  departmentId: number
}

export interface UpdateUserInput {
  fullName: string
  email: string
  departmentId: number
  password?: string
}

export interface LoadDefaultsResult {
  added: number
  skipped: number
}

export const loadDefaultUsers = (password: string) =>
  apiFetch<LoadDefaultsResult>('/users/load-defaults', { method: 'POST', body: JSON.stringify({ password }) })

export const getUsers = () => apiFetch<User[]>('/users')

export const getDepartments = () => apiFetch<Department[]>('/departments')

export const createUser = (input: CreateUserInput) =>
  apiFetch<User>('/users', { method: 'POST', body: JSON.stringify(input) })

export const updateUser = (id: number, input: UpdateUserInput) =>
  apiFetch<User>(`/users/${id}`, { method: 'PUT', body: JSON.stringify(input) })

export const deleteUser = (id: number) => apiFetch<void>(`/users/${id}`, { method: 'DELETE' })
