import { useEffect, useRef, useState } from 'react'
import ChatInput from '@/features/chat/ChatInput'
import { sendMessage, type ChatMessage } from '@/features/chat/chatApi'
import MessageBubble, { TypingIndicator } from '@/features/chat/MessageBubble'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import { ChevronDownIcon, SparkleIcon } from '@/shared/components/icons'

const suggestions = [
  'How do I reset my password?',
  "I can't connect to the VPN",
  'How many leave days do I get?',
]

interface ChatPanelProps {
  open: boolean
  onMinimize: () => void
  /** Called when a reply arrives, so the launcher can show that there is something new. */
  onReply: () => void
}

/**
 * The chat window. It stays mounted while it is minimized, so the conversation (and a reply that is still
 * being written) is not lost when the window is hidden.
 */
export default function ChatPanel({ open, onMinimize, onReply }: ChatPanelProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [sending, setSending] = useState(false)
  const [error, setError] = useState('')
  const endRef = useRef<HTMLDivElement>(null)
  const abortRef = useRef<AbortController | null>(null)

  useEffect(() => {
    if (open) endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, sending, open])

  // Logging out (or leaving the app) stops a request that is still running, which also stops the model.
  useEffect(() => () => abortRef.current?.abort(), [])

  async function handleSend(text: string) {
    setError('')
    const history = messages
    const controller = new AbortController()
    abortRef.current = controller

    setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'user', content: text }])
    setSending(true)
    try {
      const reply = await sendMessage(text, history, controller.signal)
      setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'assistant', content: reply.answer }])
      onReply()
    } catch (err) {
      if (!controller.signal.aborted) setError(errorMessage(err, 'Something went wrong. Please try again.'))
    } finally {
      setSending(false)
    }
  }

  return (
    <section
      role="dialog"
      aria-label="Chat assistant"
      onKeyDown={(e) => e.key === 'Escape' && onMinimize()}
      className={`fixed bottom-4 right-4 z-30 h-[min(60rem,calc(100vh-2rem))] w-[calc(100vw-2rem)] max-w-5xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl ring-1 ring-slate-200 ${open ? 'flex' : 'hidden'}`}
    >
      <header className="flex items-center gap-3 bg-indigo-600 px-4 py-3 text-white">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-white/20">
          <SparkleIcon className="size-5" />
        </span>
        <div className="min-w-0 flex-1 leading-tight">
          <p className="text-sm font-semibold">Ticketing AI assistant</p>
          <p className="text-xs text-indigo-100">Ask a question about work</p>
        </div>
        <button
          type="button"
          onClick={onMinimize}
          aria-label="Minimize chat"
          className="rounded-lg p-2 text-indigo-100 hover:bg-white/15 hover:text-white"
        >
          <ChevronDownIcon className="size-5" />
        </button>
      </header>

      <div className="flex-1 space-y-4 overflow-y-auto bg-slate-50 px-4 py-4">
        {messages.length === 0 && !sending ? (
          <div className="flex h-full flex-col items-center justify-center text-center">
            <h2 className="text-base font-semibold text-slate-900">How can I help you today?</h2>
            <p className="mt-1 text-sm text-slate-500">Ask a question, or try one of these.</p>
            <div className="mt-5 flex flex-col gap-2">
              {suggestions.map((suggestion) => (
                <button
                  key={suggestion}
                  type="button"
                  onClick={() => handleSend(suggestion)}
                  className="rounded-full bg-white px-4 py-2 text-sm text-slate-700 shadow-sm ring-1 ring-slate-200 transition hover:bg-indigo-50 hover:text-indigo-700 hover:ring-indigo-200"
                >
                  {suggestion}
                </button>
              ))}
            </div>
          </div>
        ) : (
          <>
            {messages.map((message) => (
              <MessageBubble key={message.id} message={message} />
            ))}
            {sending && <TypingIndicator />}
          </>
        )}
        <div ref={endRef} />
      </div>

      <div className="space-y-2 border-t border-slate-200 bg-white p-3">
        {error && <Alert message={error} />}
        <ChatInput disabled={sending} onSend={handleSend} />
      </div>
    </section>
  )
}
