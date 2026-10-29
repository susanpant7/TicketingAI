import { apiFetch } from '@/shared/api/apiClient'

export interface KnowledgeChunk {
  id: number
  documentTitle: string
  category: string
  heading: string
  content: string
  sourceFile: string
  ingestedAt: string
}

export interface ReloadResult {
  deleted: number
  inserted: number
  documents: number
}

export const getChunks = () => apiFetch<KnowledgeChunk[]>('/knowledge/chunks')

export const reloadKnowledge = () => apiFetch<ReloadResult>('/knowledge/reload', { method: 'POST' })
