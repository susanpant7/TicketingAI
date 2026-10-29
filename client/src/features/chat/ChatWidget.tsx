import { useEffect, useRef, useState } from 'react'
import ChatPanel from '@/features/chat/ChatPanel'
import { ChatIcon } from '@/shared/components/icons'

/** A floating chat button in the corner of every page. It opens the chat window, which can be minimized again. */
export default function ChatWidget() {
  const [open, setOpen] = useState(false)
  const [unread, setUnread] = useState(false)

  // A reply can arrive after the window was minimized, so the callback reads the current state from a ref.
  const openRef = useRef(open)
  useEffect(() => {
    openRef.current = open
  }, [open])

  function openChat() {
    setOpen(true)
    setUnread(false)
  }

  return (
    <>
      <ChatPanel
        open={open}
        onMinimize={() => setOpen(false)}
        onReply={() => {
          if (!openRef.current) setUnread(true)
        }}
      />

      {!open && (
        <button
          type="button"
          onClick={openChat}
          aria-label={unread ? 'Open chat (new reply)' : 'Open chat'}
          className="fixed bottom-4 right-4 z-30 flex size-14 items-center justify-center rounded-full bg-indigo-600 text-white shadow-lg transition hover:scale-105 hover:bg-indigo-500 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2"
        >
          <ChatIcon className="size-7" />
          {unread && <span className="absolute right-0.5 top-0.5 size-3.5 rounded-full bg-red-500 ring-2 ring-white" />}
        </button>
      )}
    </>
  )
}
