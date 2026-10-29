import { apiFetch } from '@/shared/api/apiClient'

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
}

export interface ChatReply {
  answer: string
  /** The department the reply relates to ("HR", "IT" or "Finance"), or null. Not shown yet. */
  department: string | null
}

// The server keeps no conversation state yet, so the recent messages are sent along with each question.
// The server uses the last ones, as many as the "chat.history_messages" setting says.
const HISTORY_LENGTH = 20

export function sendMessage(message: string, history: ChatMessage[], signal: AbortSignal) {
  return apiFetch<ChatReply>('/chat', {
    method: 'POST',
    body: JSON.stringify({
      message,
      history: history.slice(-HISTORY_LENGTH).map(({ role, content }) => ({ role, content })),
    }),
    signal,
  })
}
