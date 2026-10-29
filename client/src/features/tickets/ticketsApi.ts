import { apiFetch } from '@/shared/api/apiClient'

export interface Ticket {
  id: number
  title: string
  description: string
  department: string
  reportedBy: string
  reportedByUsername: string
  assignedTo: string
  assignedToUsername: string
  createdAt: string
}

export const getTickets = () => apiFetch<Ticket[]>('/tickets')
