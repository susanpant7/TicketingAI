import { API_URL, ApiError } from '@/shared/api/apiClient'
import { getToken } from '@/shared/api/token'

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
}

export interface ChatReply {
  answer: string
  /** The name of the department the reply relates to, or null. Not shown yet. */
  department: string | null
}

// The server keeps no conversation state yet, so the recent messages are sent along with each question.
// The server uses the last ones, as many as the "chat.history_messages" setting says.
const HISTORY_LENGTH = 20

/**
 * Sends a message and shows the reply while it is written: onDraft gets the reply so far, and the promise resolves with the
 * final reply (which can differ from the last draft, e.g. when a ticket was created). The server sends one JSON object per line.
 */
export async function streamMessage(
  message: string,
  history: ChatMessage[],
  signal: AbortSignal,
  onDraft: (textSoFar: string) => void,
): Promise<ChatReply> {
  const token = getToken()
  const response = await fetch(`${API_URL}/api/chat/stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: JSON.stringify({
      message,
      history: history.slice(-HISTORY_LENGTH).map(({ role, content }) => ({ role, content })),
    }),
    signal,
  })
  if (!response.ok || !response.body) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.detail)
  }

  const reader = response.body.pipeThrough(new TextDecoderStream()).getReader()
  let buffered = ''
  for (;;) {
    const { value, done } = await reader.read()
    if (done) break
    buffered += value
    const lines = buffered.split('\n')
    buffered = lines.pop() ?? ''
    for (const line of lines.filter(Boolean)) {
      const event = JSON.parse(line) as { draft?: string; reply?: ChatReply; error?: string }
      if (event.draft !== undefined) onDraft(event.draft)
      if (event.reply) return event.reply
      if (event.error) throw new ApiError(503, event.error)
    }
  }
  throw new ApiError(502, 'The reply was cut off. Please try again.')
}
