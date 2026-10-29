import { useEffect, useState } from 'react'
import { useAuth } from '@/features/auth/useAuth'
import { getTickets, type Ticket } from '@/features/tickets/ticketsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import CategoryBadge from '@/shared/components/CategoryBadge'
import { InboxIcon } from '@/shared/components/icons'
import PageHeader from '@/shared/components/PageHeader'

// The API returns UTC timestamps without a time zone suffix.
const formatDate = (iso: string) => new Date(iso.endsWith('Z') ? iso : `${iso}Z`).toLocaleString()

export default function TicketsPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [expandedId, setExpandedId] = useState<number | null>(null)

  useEffect(() => {
    let ignore = false
    getTickets()
      .then((loaded) => !ignore && setTickets(loaded))
      .catch((err) => !ignore && setError(errorMessage(err, 'Could not load the tickets. Make sure the API is running, then try again.')))
      .finally(() => !ignore && setLoading(false))
    return () => {
      ignore = true
    }
  }, [])

  // The people column shows "(you)" next to the logged-in user.
  const person = (name: string, username: string) => (username === user?.username ? `${name} (you)` : name)

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader
        title="Tickets"
        description={isAdmin ? 'All the tickets.' : 'Tickets you reported and tickets assigned to you.'}
      />

      {error && (
        <div className="mt-6">
          <Alert message={error} />
        </div>
      )}

      {loading ? (
        <p className="mt-8 text-center text-sm text-slate-500">Loading...</p>
      ) : tickets.length === 0 ? (
        !error && (
          <div className="mt-8 flex flex-col items-center rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center">
            <span className="flex size-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
              <InboxIcon className="size-6" />
            </span>
            <h2 className="mt-4 text-base font-semibold text-slate-900">No tickets yet</h2>
            <p className="mt-1 text-sm text-slate-500">
              {isAdmin ? 'Tickets created from the chat will show up here.' : 'When the assistant creates a ticket for you, it will show up here.'}
            </p>
          </div>
        )
      ) : (
        <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-6 py-3 font-medium">Ticket</th>
                <th className="px-6 py-3 font-medium">Department</th>
                <th className="hidden px-6 py-3 font-medium md:table-cell">Reported by</th>
                <th className="hidden px-6 py-3 font-medium md:table-cell">Assigned to</th>
                <th className="hidden px-6 py-3 font-medium lg:table-cell">Created</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {tickets.map((ticket) => (
                <tr
                  key={ticket.id}
                  onClick={() => setExpandedId(expandedId === ticket.id ? null : ticket.id)}
                  className="cursor-pointer align-top hover:bg-slate-50/60"
                >
                  <td className="px-6 py-4">
                    <p className="font-medium text-slate-900">
                      #{ticket.id} {ticket.title}
                    </p>
                    <p className={`mt-1 max-w-xl whitespace-pre-wrap text-slate-500 ${expandedId === ticket.id ? '' : 'line-clamp-1'}`}>
                      {ticket.description}
                    </p>
                  </td>
                  <td className="px-6 py-4">
                    <CategoryBadge name={ticket.department} />
                  </td>
                  <td className="hidden px-6 py-4 text-slate-600 md:table-cell">
                    {person(ticket.reportedBy, ticket.reportedByUsername)}
                  </td>
                  <td className="hidden px-6 py-4 text-slate-600 md:table-cell">
                    {person(ticket.assignedTo, ticket.assignedToUsername)}
                  </td>
                  <td className="hidden px-6 py-4 text-slate-600 lg:table-cell">{formatDate(ticket.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  )
}
