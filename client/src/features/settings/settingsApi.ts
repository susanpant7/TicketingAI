import { apiFetch } from '@/shared/api/apiClient'

export interface Setting {
  key: string
  label: string
  description: string
  value: string
}

export interface SavedSetting {
  setting: Setting
}

export const getSettings = () => apiFetch<Setting[]>('/settings')

export const createSetting = (input: Setting) => apiFetch<Setting>('/settings', { method: 'POST', body: JSON.stringify(input) })

/** Puts back every default. When that changes the embedding model, the stored knowledge is embedded again first (`reembedded` sections). */
export const restoreDefaults = () => apiFetch<{ reembedded: number }>('/settings/restore-defaults', { method: 'POST' })

/** Deletes all data (tickets, users, knowledge, departments and settings), as on a fresh start. */
export const cleanUpData = () => apiFetch<void>('/settings/clean-up', { method: 'POST' })

export const updateSetting = (key: string, input: Omit<Setting, 'key'>) =>
  apiFetch<SavedSetting>(`/settings/${encodeURIComponent(key)}`, { method: 'PUT', body: JSON.stringify(input) })

export type ModelKind = 'chat' | 'embedding'

/** A chat or embedding model the admin saved. The key itself is never sent: `apiKeyName` is the name of its environment variable. */
export interface SavedAiModel {
  id: number
  kind: ModelKind
  name: string
  baseUrl: string
  model: string
  apiKeyName: string
  hasApiKey: boolean
  notes: string
}

export type SaveAiModelRequest = Pick<SavedAiModel, 'name' | 'baseUrl' | 'model' | 'apiKeyName' | 'notes'>

/** A known service. `hasApiKey` says whether the variable named `apiKeyName` is set on the server. */
export interface AiProvider {
  name: string
  baseUrl: string
  exampleModel: string
  needsApiKey: boolean
  freeTier: string
  signUpUrl: string
  apiKeyName: string
  hasApiKey: boolean
}

export interface AiProvidersInfo {
  providers: AiProvider[]
  embeddingProviders: AiProvider[]
  /** Whether the key the chat model is using right now is set . */
  chatKeyInUseSet: boolean
  /** The same for the embedding model. */
  embeddingKeyInUseSet: boolean
  /** The name of the variable the chat model really reads (a saved name, else its service's). */
  chatKeyNameInUse: string
  /** The same for the embedding model. */
  embeddingKeyNameInUse: string
}

export const getAiProviders = () => apiFetch<AiProvidersInfo>('/settings/ai-providers')

export const getSavedModels = (kind: ModelKind) => apiFetch<SavedAiModel[]>(`/settings/saved-models/${kind}`)

export const addSavedModel = (kind: ModelKind, input: SaveAiModelRequest) =>
  apiFetch<SavedAiModel>(`/settings/saved-models/${kind}`, { method: 'POST', body: JSON.stringify(input) })

export const updateSavedModel = (kind: ModelKind, id: number, input: SaveAiModelRequest) =>
  apiFetch<SavedAiModel>(`/settings/saved-models/${kind}/${id}`, { method: 'PUT', body: JSON.stringify(input) })

export const deleteSavedModel = (kind: ModelKind, id: number) =>
  apiFetch<void>(`/settings/saved-models/${kind}/${id}`, { method: 'DELETE' })

export interface AvailableAiModels {
  baseUrl: string
  models: string[]
}

/** Asks the service at a known address which models of this kind it offers for the key with this name. */
export const getAvailableModels = (kind: ModelKind, baseUrl: string, keyName: string) =>
  apiFetch<AvailableAiModels>(
    `/settings/available-models?kind=${kind}&baseUrl=${encodeURIComponent(baseUrl)}&keyName=${encodeURIComponent(keyName)}`,
  )

export interface ModelChoice {
  baseUrl: string
  model: string
  apiKeyName: string
}

/** Sets the Chat API base URL, the Chat model and the Chat API key name together. */
export const switchChatModel = (choice: ModelChoice) =>
  apiFetch<{ reembedded: number }>('/settings/use-chat-model', {
    method: 'POST',
    body: JSON.stringify({ baseUrl: choice.baseUrl, model: choice.model, apiKeyName: choice.apiKeyName }),
  })

/**
 * Converts the stored knowledge to the new embedding model, then saves the embedding settings. It takes a while, and when it fails
 * nothing is changed. `reembedded` is how many sections were converted.
 */
export const switchEmbeddingModel = (choice: ModelChoice) =>
  apiFetch<{ reembedded: number }>('/settings/use-embedding-model', {
    method: 'POST',
    // Only the model's own fields: the choice from the page also holds a React element (its notes), which cannot be sent as JSON.
    body: JSON.stringify({
      baseUrl: choice.baseUrl,
      model: choice.model,
      apiKeyName: choice.apiKeyName,
    }),
  })

/**
 * Tests one model of a known service with a tiny request and saves nothing. It resolves with how long the model took to answer, and
 * throws an error with the service's own message when it does not (a retired name, a wrong key).
 */
export const testModel = (kind: ModelKind, choice: ModelChoice) =>
  apiFetch<{ milliseconds: number }>(`/settings/test-model/${kind}`, { method: 'POST', body: JSON.stringify(choice) })
