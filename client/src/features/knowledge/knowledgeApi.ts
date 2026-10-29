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

export interface IngestResult {
  deleted: number
  inserted: number
  documents: number
}

export const getChunks = () => apiFetch<KnowledgeChunk[]>('/knowledge/chunks')

export const ingestDocuments = () => apiFetch<IngestResult>('/knowledge/ingest', { method: 'POST' })

export const deleteAllChunks = () => apiFetch<{ deleted: number }>('/knowledge/chunks', { method: 'DELETE' })
