import { apiFetch } from '@/shared/api/apiClient'

export interface ChatSource {
  documentTitle: string
  heading: string
  category: string
  score: number
}

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  sources?: ChatSource[]
}

interface ChatReply {
  answer: string
  sources: ChatSource[]
}

// The server keeps no conversation state yet, so the recent messages are sent along with each question.
const HISTORY_LENGTH = 6

export function sendMessage(message: string, history: ChatMessage[]) {
  return apiFetch<ChatReply>('/chat', {
    method: 'POST',
    body: JSON.stringify({
      message,
      history: history.slice(-HISTORY_LENGTH).map(({ role, content }) => ({ role, content })),
    }),
  })
}
