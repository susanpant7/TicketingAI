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
      <div className="max-w-[80%]">
        <p className="whitespace-pre-wrap rounded-2xl rounded-tl-md bg-white px-4 py-2.5 text-sm leading-6 text-slate-800 shadow-sm ring-1 ring-slate-200">
          {message.content}
        </p>
        {message.sources && message.sources.length > 0 && (
          <div className="mt-2 flex flex-wrap items-center gap-1.5">
            <span className="text-xs text-slate-400">Related:</span>
            {message.sources.map((source) => (
              <span
                key={`${source.documentTitle}-${source.heading}`}
                title={`${source.category} · match ${Math.round(source.score * 100)}%`}
                className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-600"
              >
                {source.documentTitle} › {source.heading}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export function TypingIndicator() {
  return (
    <div className="flex items-start gap-3" role="status" aria-label="Assistant is typing">
      <AssistantAvatar />
      <div>
        <div className="flex w-fit gap-1 rounded-2xl rounded-tl-md bg-white px-4 py-4 shadow-sm ring-1 ring-slate-200">
          {[0, 150, 300].map((delay) => (
            <span
              key={delay}
              style={{ animationDelay: `${delay}ms` }}
              className="size-2 animate-bounce rounded-full bg-slate-400"
            />
          ))}
        </div>
        <p className="mt-2 text-xs text-slate-400">Searching the knowledge base and writing an answer. This can take up to a minute.</p>
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
