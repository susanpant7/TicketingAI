import { useEffect, useRef, useState } from 'react'
import ChatInput from '@/features/chat/ChatInput'
import { sendMessage, type ChatMessage } from '@/features/chat/chatApi'
import MessageBubble, { TypingIndicator } from '@/features/chat/MessageBubble'
import Alert from '@/shared/components/Alert'
import { SparkleIcon } from '@/shared/components/icons'

const suggestions = [
  'How do I reset my password?',
  "I can't connect to the VPN",
  'How many leave days do I get?',
]

export default function ChatPage() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [sending, setSending] = useState(false)
  const [error, setError] = useState('')
  const endRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, sending])

  async function handleSend(text: string) {
    setError('')
    setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'user', content: text }])
    setSending(true)
    try {
      const reply = await sendMessage(text)
      setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'assistant', content: reply }])
    } catch {
      setError('Something went wrong. Please try again.')
    } finally {
      setSending(false)
    }
  }

  return (
    <main className="mx-auto flex h-[calc(100vh-4rem)] max-w-3xl flex-col px-4">
      <div className="flex-1 space-y-5 overflow-y-auto py-6">
        {messages.length === 0 && !sending ? (
          <div className="flex h-full flex-col items-center justify-center text-center">
            <span className="flex size-14 items-center justify-center rounded-2xl bg-indigo-600 text-white shadow-sm">
              <SparkleIcon className="size-8" />
            </span>
            <h1 className="mt-6 text-2xl font-bold tracking-tight text-slate-900">How can I help you today?</h1>
            <p className="mt-2 max-w-md text-sm text-slate-500">
              Ask an IT or HR question, or describe a problem and I can raise a ticket for you.
            </p>
            <div className="mt-8 flex flex-wrap justify-center gap-2">
              {suggestions.map((suggestion) => (
                <button
                  key={suggestion}
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

      <div className="space-y-3 pb-6 pt-2">
        {error && <Alert message={error} />}
        <ChatInput disabled={sending} onSend={handleSend} />
      </div>
    </main>
  )
}
