import { apiFetch } from '@/shared/api/apiClient'

export type TicketStatus = 'New' | 'InProgress' | 'Completed'

export const STATUS_LABELS: Record<TicketStatus, string> = {
  New: 'New',
  InProgress: 'In progress',
  Completed: 'Completed',
}

export interface Ticket {
  id: number
  title: string
  description: string
  department: string
  status: TicketStatus
  reportedBy: string
  reportedByUsername: string
  assignedTo: string
  assignedToUsername: string
  createdAt: string
}

export const getTickets = () => apiFetch<Ticket[]>('/tickets')

export const updateTicketStatus = (id: number, status: TicketStatus) =>
  apiFetch<Ticket>(`/tickets/${id}/status`, { method: 'PUT', body: JSON.stringify({ status }) })
