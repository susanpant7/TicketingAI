import { apiFetch } from '@/shared/api/apiClient'

export interface Setting {
  key: string
  label: string
  description: string
  value: string
}

export interface SavedSetting {
  setting: Setting
  /** How many knowledge sections the change deleted (only changing the embedding model deletes any). */
  knowledgeDeleted: number
}

export const EMBEDDING_MODEL_KEY = 'embedding.model'

export const getSettings = () => apiFetch<Setting[]>('/settings')

export const createSetting = (input: Setting) =>
  apiFetch<Setting>('/settings', { method: 'POST', body: JSON.stringify(input) })

export const restoreDefaults = () =>
  apiFetch<{ knowledgeDeleted: number }>('/settings/restore-defaults', { method: 'POST' })

export const updateSetting = (key: string, input: Omit<Setting, 'key'>) =>
  apiFetch<SavedSetting>(`/settings/${encodeURIComponent(key)}`, { method: 'PUT', body: JSON.stringify(input) })
