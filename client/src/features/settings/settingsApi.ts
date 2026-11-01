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

/** Deletes all data (tickets, users, knowledge, departments and settings), as on a fresh start. */
export const cleanUpData = () => apiFetch<void>('/settings/clean-up', { method: 'POST' })

export const updateSetting = (key: string, input: Omit<Setting, 'key'>) =>
  apiFetch<SavedSetting>(`/settings/${encodeURIComponent(key)}`, { method: 'PUT', body: JSON.stringify(input) })

export interface SavedChatModel {
  id: number
  name: string
  baseUrl: string
  model: string
  notes: string
}

export type SaveChatModelRequest = Omit<SavedChatModel, 'id'>

export interface AiProvider {
  name: string
  baseUrl: string
  exampleModel: string
  needsApiKey: boolean
  freeTier: string
  signUpUrl: string
}

export interface AiProvidersInfo {
  /** The name of the environment variable that holds the key of an online service. */
  apiKeyName: string
  /** Whether that key is set on the server. The key itself is never sent. */
  hasApiKey: boolean
  providers: AiProvider[]
}

export const getAiProviders = () => apiFetch<AiProvidersInfo>('/settings/ai-providers')

export const getChatModels = () => apiFetch<SavedChatModel[]>('/settings/chat-models')

export const addChatModel = (input: SaveChatModelRequest) =>
  apiFetch<SavedChatModel>('/settings/chat-models', { method: 'POST', body: JSON.stringify(input) })

export const updateChatModel = (id: number, input: SaveChatModelRequest) =>
  apiFetch<SavedChatModel>(`/settings/chat-models/${id}`, { method: 'PUT', body: JSON.stringify(input) })

export const deleteChatModel = (id: number) => apiFetch<void>(`/settings/chat-models/${id}`, { method: 'DELETE' })
