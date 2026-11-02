import { useEffect, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { ArrowUpIcon } from '@/shared/components/icons'

/** Goes to the top of a page when another page opens, and shows a button that scrolls back up once the page has been scrolled down. */
export default function ScrollToTop() {
  const { pathname, hash } = useLocation()
  const [visible, setVisible] = useState(false)

  // A new page starts at the top. A link to a section of a page (#section) is left to the browser.
  useEffect(() => {
    if (!hash) window.scrollTo(0, 0)
  }, [pathname, hash])

  useEffect(() => {
    const update = () => setVisible(window.scrollY > 400)
    update()
    window.addEventListener('scroll', update, { passive: true })
    return () => window.removeEventListener('scroll', update)
  }, [])

  if (!visible) return null

  return (
    <button
      type="button"
      onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
      aria-label="Scroll to the top"
      className="fixed bottom-20 right-4 z-20 flex size-11 items-center justify-center rounded-full bg-white text-indigo-600 shadow-lg ring-1 ring-slate-200 transition hover:bg-indigo-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500"
    >
      <ArrowUpIcon className="size-5" />
    </button>
  )
}
