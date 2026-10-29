import { apiFetch } from '@/shared/api/apiClient'

export interface Department {
  id: number
  name: string
  /** What the department handles. It is given to the chat assistant. */
  description: string
  /** How to reach the department. The chat assistant gives it to the user. */
  contact: string
  userCount: number
}

export type DepartmentInput = Pick<Department, 'name' | 'description' | 'contact'>

export const getDepartments = () => apiFetch<Department[]>('/departments')

export const createDepartment = (input: DepartmentInput) =>
  apiFetch<Department>('/departments', { method: 'POST', body: JSON.stringify(input) })

export const updateDepartment = (id: number, input: DepartmentInput) =>
  apiFetch<Department>(`/departments/${id}`, { method: 'PUT', body: JSON.stringify(input) })

export const deleteDepartment = (id: number) => apiFetch<void>(`/departments/${id}`, { method: 'DELETE' })
