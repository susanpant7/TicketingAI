import { ApiError, apiStream } from '@/shared/api/apiClient'

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
}

export interface ChatReply {
  answer: string
  /** The department the reply relates to ("HR", "IT" or "Finance"). Missing when there is none. Not shown yet. */
  department?: string
}

// The server streams one JSON object per line: text as it is written, then the final reply, or an error.
type StreamEvent =
  | { type: 'token'; text: string }
  | { type: 'done'; response: ChatReply }
  | { type: 'error'; message: string }

// The server keeps no conversation state yet, so the recent messages are sent along with each question.
// The server uses the last ones, as many as the "chat.history_messages" setting says.
const HISTORY_LENGTH = 20

/** Sends a message and calls onToken with each piece of the reply as it arrives. Resolves with the final reply. */
export async function streamMessage(
  message: string,
  history: ChatMessage[],
  onToken: (text: string) => void,
  signal: AbortSignal,
): Promise<ChatReply> {
  const response = await apiStream('/chat', {
    method: 'POST',
    body: JSON.stringify({
      message,
      history: history.slice(-HISTORY_LENGTH).map(({ role, content }) => ({ role, content })),
    }),
    signal,
  })

  if (!response.body) throw new ApiError(502, 'The server did not send a reply.')

  const reader = response.body.pipeThrough(new TextDecoderStream()).getReader()
  let buffered = ''

  for (let chunk = await reader.read(); !chunk.done; chunk = await reader.read()) {
    buffered += chunk.value

    let newline = buffered.indexOf('\n')
    while (newline >= 0) {
      const line = buffered.slice(0, newline).trim()
      buffered = buffered.slice(newline + 1)
      newline = buffered.indexOf('\n')
      if (!line) continue

      const event = JSON.parse(line) as StreamEvent
      if (event.type === 'token') onToken(event.text)
      else if (event.type === 'done') return event.response
      else throw new ApiError(503, event.message)
    }
  }

  throw new ApiError(502, 'The connection was lost before the reply finished. Please try again.')
}
