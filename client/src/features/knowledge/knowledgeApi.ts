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
