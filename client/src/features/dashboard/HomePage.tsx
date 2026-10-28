import { Link } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import { buttonClass } from '@/shared/components/buttonStyles'
import { ChatIcon, TicketIcon, UsersIcon } from '@/shared/components/icons'

const features = [
  {
    icon: TicketIcon,
    title: 'Track tickets',
    text: 'Raise and follow IT and HR requests in one place, from first report to resolution.',
  },
  {
    icon: ChatIcon,
    title: 'Ask the AI assistant',
    text: 'Get instant answers from your company knowledge base, and let it open a ticket when needed.',
  },
  {
    icon: UsersIcon,
    title: 'One team, one view',
    text: 'IT, HR and Finance share the same workspace, so requests reach the right people.',
  },
]

export default function HomePage() {
  const { user, isAuthenticated } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const cta = !isAuthenticated
    ? { to: '/login', label: 'Login to get started' }
    : isAdmin
      ? { to: '/users', label: 'Manage users' }
      : { to: '/tickets', label: 'View tickets' }

  return (
    <main>
      <section className="bg-gradient-to-b from-indigo-50 via-white to-slate-50">
        <div className="mx-auto max-w-4xl px-6 py-24 text-center">
          <span className="rounded-full bg-indigo-100 px-3 py-1 text-xs font-semibold text-indigo-700">
            IT &amp; HR support
          </span>
          <h1 className="mt-6 text-4xl font-bold tracking-tight text-slate-900 sm:text-6xl">
            Support that answers
            <span className="text-indigo-600"> before you ask twice</span>
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-lg leading-8 text-slate-600">
            Chat with an AI assistant trained on your company knowledge, and turn any unresolved issue into a ticket
            in seconds.
          </p>
          <div className="mt-10 flex justify-center gap-3">
            <Link to={cta.to} className={buttonClass('primary', 'lg')}>
              {cta.label}
            </Link>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-6 pb-24">
        <div className="grid gap-6 sm:grid-cols-3">
          {features.map(({ icon: Icon, title, text }) => (
            <div key={title} className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
              <span className="flex size-11 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
                <Icon className="size-6" />
              </span>
              <h2 className="mt-5 text-base font-semibold text-slate-900">{title}</h2>
              <p className="mt-2 text-sm leading-6 text-slate-600">{text}</p>
            </div>
          ))}
        </div>
      </section>
    </main>
  )
}
