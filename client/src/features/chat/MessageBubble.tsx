import type { ChatMessage } from '@/features/chat/chatApi'
import { SparkleIcon } from '@/shared/components/icons'

export default function MessageBubble({ message }: { message: ChatMessage }) {
  if (message.role === 'user') {
    return (
      <div className="flex justify-end">
        <p className="max-w-[80%] whitespace-pre-wrap rounded-2xl rounded-br-md bg-indigo-600 px-4 py-2.5 text-sm leading-6 text-white shadow-sm">
          {message.content}
        </p>
      </div>
    )
  }

  return (
    <div className="flex items-start gap-3">
      <AssistantAvatar />
      <p className="max-w-[80%] whitespace-pre-wrap rounded-2xl rounded-tl-md bg-white px-4 py-2.5 text-sm leading-6 text-slate-800 shadow-sm ring-1 ring-slate-200">
        {message.content}
      </p>
    </div>
  )
}

export function TypingIndicator() {
  return (
    <div className="flex items-start gap-3" role="status" aria-label="Assistant is typing">
      <AssistantAvatar />
      <div className="flex gap-1 rounded-2xl rounded-tl-md bg-white px-4 py-4 shadow-sm ring-1 ring-slate-200">
        {[0, 150, 300].map((delay) => (
          <span
            key={delay}
            style={{ animationDelay: `${delay}ms` }}
            className="size-2 animate-bounce rounded-full bg-slate-400"
          />
        ))}
      </div>
    </div>
  )
}

function AssistantAvatar() {
  return (
    <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-indigo-600 text-white">
      <SparkleIcon className="size-4" />
    </span>
  )
}
