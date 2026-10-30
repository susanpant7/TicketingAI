import type { ComponentType, SVGProps } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import { buttonClass } from '@/shared/components/buttonStyles'
import {
  BookIcon,
  BuildingIcon,
  ChatIcon,
  SlidersIcon,
  TicketIcon,
  UsersIcon,
} from '@/shared/components/icons'

type Icon = ComponentType<SVGProps<SVGSVGElement>>

const steps = [
  {
    icon: ChatIcon,
    title: 'Ask the assistant',
    text: 'Type your question in the chat. It answers from your company knowledge and points you to the right department.',
  },
  {
    icon: TicketIcon,
    title: 'Create a ticket',
    text: 'If the answer does not solve the problem, the assistant offers to create a ticket. Say yes and it is done.',
  },
  {
    icon: UsersIcon,
    title: 'Get it handled',
    text: 'The ticket goes to a member of the right department, who updates its status until it is completed.',
  },
]

const adminLinks: { to: string; icon: Icon; title: string; text: string }[] = [
  { to: '/tickets', icon: TicketIcon, title: 'Tickets', text: 'See every ticket and its status.' },
  { to: '/knowledge', icon: BookIcon, title: 'Knowledge', text: 'Upload the files the assistant answers from.' },
  { to: '/departments', icon: BuildingIcon, title: 'Departments', text: 'Set up the departments and how to contact them.' },
  { to: '/users', icon: UsersIcon, title: 'Users', text: 'Add users and choose their department.' },
  { to: '/settings', icon: SlidersIcon, title: 'Settings', text: 'Models, prompt and how the assistant searches.' },
]

function Card({ icon: Icon, title, text }: { icon: Icon; title: string; text: string }) {
  return (
    <>
      <span className="flex size-11 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
        <Icon className="size-6" />
      </span>
      <h2 className="mt-5 text-base font-semibold text-slate-900">{title}</h2>
      <p className="mt-2 text-sm leading-6 text-slate-600">{text}</p>
    </>
  )
}

export default function HomePage() {
  const { user, isAuthenticated } = useAuth()
  const isAdmin = user?.role === 'Admin'

  return (
    <main>
      <section className="bg-gradient-to-b from-indigo-50 via-white to-slate-50">
        <div className="mx-auto max-w-4xl px-6 py-20 text-center">
          <span className="rounded-full bg-indigo-100 px-3 py-1 text-xs font-semibold text-indigo-700">
            {isAdmin ? 'Administration' : 'Company support'}
          </span>

          {isAuthenticated && user ? (
            <>
              <h1 className="mt-6 text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
                Hello, <span className="text-indigo-600">{user.username}</span>
              </h1>
              <p className="mx-auto mt-5 max-w-2xl text-lg leading-8 text-slate-600">
                {isAdmin
                  ? 'Keep the assistant useful: set up the departments and users, and keep its knowledge up to date.'
                  : 'Ask the assistant anything about work. Use the chat button at the bottom right of the page.'}
              </p>
              {!isAdmin && (
                <div className="mt-8 flex justify-center">
                  <Link to="/tickets" className={buttonClass('primary', 'lg')}>
                    View my tickets
                  </Link>
                </div>
              )}
            </>
          ) : (
            <>
              <h1 className="mt-6 text-4xl font-bold tracking-tight text-slate-900 sm:text-6xl">
                Support that answers
                <span className="text-indigo-600"> before you ask twice</span>
              </h1>
              <p className="mx-auto mt-6 max-w-2xl text-lg leading-8 text-slate-600">
                Chat with an AI assistant that knows your company, and turn any unresolved issue into a ticket for the
                right department in seconds.
              </p>
              <div className="mt-10 flex justify-center">
                <Link to="/login" className={buttonClass('primary', 'lg')}>
                  Login to get started
                </Link>
              </div>
            </>
          )}
        </div>
      </section>

      {isAdmin ? (
        <section className="mx-auto max-w-6xl px-6 pb-24">
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {adminLinks.map(({ to, ...card }) => (
              <Link
                key={to}
                to={to}
                className="block rounded-2xl border border-slate-200 bg-white p-6 shadow-sm transition hover:border-indigo-300 hover:shadow-md"
              >
                <Card {...card} />
              </Link>
            ))}
          </div>
        </section>
      ) : (
        <section className="mx-auto max-w-6xl px-6 pb-24">
          <h2 className="text-center text-sm font-semibold uppercase tracking-wide text-slate-500">How it works</h2>
          <div className="mt-6 grid gap-6 sm:grid-cols-3">
            {steps.map((step, index) => (
              <div key={step.title} className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
                <p className="mb-4 text-xs font-semibold text-indigo-600">Step {index + 1}</p>
                <Card {...step} />
              </div>
            ))}
          </div>
        </section>
      )}
    </main>
  )
}
