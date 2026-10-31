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

export const getChunks = () => apiFetch<KnowledgeChunk[]>('/knowledge/chunks')


export interface UploadFileResult {
  fileName: string
  sections: number
  error: string | null
}

export const deleteDepartmentKnowledge = (departmentId: number) =>
  apiFetch<{ deleted: number }>(`/knowledge/department/${departmentId}`, { method: 'DELETE' })

export function uploadKnowledge(departmentId: number, flush: boolean, files: File[]) {
  const form = new FormData()
  form.append('departmentId', String(departmentId))
  form.append('flush', String(flush))
  files.forEach((file) => form.append('files', file))
  return apiFetch<UploadFileResult[]>('/knowledge/upload', { method: 'POST', body: form })
}

/** Uploads the default knowledge files into their departments, which must exist. */
export const loadDefaultKnowledge = () => apiFetch<UploadFileResult[]>('/knowledge/load-defaults', { method: 'POST' })

/** A section as written on the page. */
export interface KnowledgeSectionInput {
  heading: string
  content: string
}

/** Saves a document written as a title and sections. sourceFile is null for a new document (the server makes it from the title). */
export const saveKnowledgeDocument = (document: {
  sourceFile: string | null
  title: string
  departmentId: number
  sections: KnowledgeSectionInput[]
}) => apiFetch<UploadFileResult>('/knowledge/documents', { method: 'PUT', body: JSON.stringify(document) })

export const deleteKnowledgeDocument = (sourceFile: string) =>
  apiFetch<{ deleted: number }>(`/knowledge/documents/${encodeURIComponent(sourceFile)}`, { method: 'DELETE' })

export interface KnowledgeSearchResult {
  id: number
  documentTitle: string
  category: string
  heading: string
  content: string
  sourceFile: string
  score: number
}

export const searchKnowledge = (query: string) =>
  apiFetch<KnowledgeSearchResult[]>(`/knowledge/search?q=${encodeURIComponent(query)}&k=5`)

/** The sections of one document, in order. */
export const sectionsOf = (chunks: KnowledgeChunk[], sourceFile: string): KnowledgeSectionInput[] =>
  chunks.filter((c) => c.sourceFile === sourceFile).map((c) => ({ heading: c.heading, content: c.content }))
