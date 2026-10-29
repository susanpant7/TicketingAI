import { useEffect, useState } from 'react'
import { useAuth } from '@/features/auth/useAuth'
import {
  getTickets,
  STATUS_LABELS,
  updateTicketStatus,
  type Ticket,
  type TicketStatus,
} from '@/features/tickets/ticketsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import CategoryBadge from '@/shared/components/CategoryBadge'
import { InboxIcon } from '@/shared/components/icons'
import PageHeader from '@/shared/components/PageHeader'
import Tabs from '@/shared/components/Tabs'

const REPORTED = 'reported'
const ASSIGNED = 'assigned'

// The API returns UTC timestamps without a time zone suffix.
const formatDate = (iso: string) => new Date(iso.endsWith('Z') ? iso : `${iso}Z`).toLocaleString()

const statusStyles: Record<TicketStatus, string> = {
  New: 'bg-sky-50 text-sky-700 ring-sky-200',
  InProgress: 'bg-amber-50 text-amber-800 ring-amber-200',
  Completed: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
}

function StatusBadge({ status }: { status: TicketStatus }) {
  return (
    <span className={`inline-block rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset ${statusStyles[status]}`}>
      {STATUS_LABELS[status]}
    </span>
  )
}

export default function TicketsPage() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [activeTab, setActiveTab] = useState(REPORTED)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [savingId, setSavingId] = useState<number | null>(null)

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

  async function handleStatusChange(ticket: Ticket, status: TicketStatus) {
    setError('')
    setSavingId(ticket.id)
    try {
      const updated = await updateTicketStatus(ticket.id, status)
      setTickets((current) => current.map((t) => (t.id === updated.id ? updated : t)))
    } catch (err) {
      setError(errorMessage(err, 'Could not change the status. Please try again.'))
    } finally {
      setSavingId(null)
    }
  }

  // An admin sees every ticket. An employee has two tabs: tickets they reported and tickets assigned to them.
  const reported = tickets.filter((t) => t.reportedByUsername === user?.username)
  const assigned = tickets.filter((t) => t.assignedToUsername === user?.username)
  const visible = isAdmin ? tickets : activeTab === ASSIGNED ? assigned : reported
  const showAssignedColumns = !isAdmin && activeTab === ASSIGNED
  const canChangeStatus = showAssignedColumns

  const tabs = [
    { id: REPORTED, label: 'Reported by you', count: reported.length },
    { id: ASSIGNED, label: 'Assigned to you', count: assigned.length },
  ]

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
      ) : (
        <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          {!isAdmin && <Tabs tabs={tabs} active={activeTab} onChange={setActiveTab} />}

          {visible.length === 0 ? (
            <div className="flex flex-col items-center px-6 py-16 text-center">
              <span className="flex size-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                <InboxIcon className="size-6" />
              </span>
              <h2 className="mt-4 text-base font-semibold text-slate-900">No tickets</h2>
              <p className="mt-1 text-sm text-slate-500">
                {isAdmin
                  ? 'Tickets created from the chat will show up here.'
                  : showAssignedColumns
                    ? 'Tickets assigned to you will show up here.'
                    : 'When the assistant creates a ticket for you, it will show up here.'}
              </p>
            </div>
          ) : (
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="px-6 py-3 font-medium">Ticket</th>
                  <th className="px-6 py-3 font-medium">Department</th>
                  {(isAdmin || !showAssignedColumns) && (
                    <th className="hidden px-6 py-3 font-medium md:table-cell">Assigned to</th>
                  )}
                  {(isAdmin || showAssignedColumns) && (
                    <th className="hidden px-6 py-3 font-medium md:table-cell">Reported by</th>
                  )}
                  <th className="hidden px-6 py-3 font-medium lg:table-cell">Created</th>
                  <th className="px-6 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {visible.map((ticket) => (
                  <tr
                    key={ticket.id}
                    onClick={() => setExpandedId(expandedId === ticket.id ? null : ticket.id)}
                    className="cursor-pointer align-top hover:bg-slate-50/60"
                  >
                    <td className="px-6 py-4">
                      <p className="font-medium text-slate-900">
                        #{ticket.id} {ticket.title}
                      </p>
                      <p
                        className={`mt-1 max-w-xl whitespace-pre-wrap text-slate-500 ${expandedId === ticket.id ? '' : 'line-clamp-1'}`}
                      >
                        {ticket.description}
                      </p>
                    </td>
                    <td className="px-6 py-4">
                      <CategoryBadge name={ticket.department} />
                    </td>
                    {(isAdmin || !showAssignedColumns) && (
                      <td className="hidden px-6 py-4 text-slate-600 md:table-cell">{ticket.assignedTo}</td>
                    )}
                    {(isAdmin || showAssignedColumns) && (
                      <td className="hidden px-6 py-4 text-slate-600 md:table-cell">{ticket.reportedBy}</td>
                    )}
                    <td className="hidden px-6 py-4 text-slate-600 lg:table-cell">{formatDate(ticket.createdAt)}</td>
                    <td className="px-6 py-4" onClick={(e) => e.stopPropagation()}>
                      {canChangeStatus ? (
                        <select
                          aria-label={`Status of ticket ${ticket.id}`}
                          value={ticket.status}
                          disabled={savingId === ticket.id}
                          onChange={(e) => handleStatusChange(ticket, e.target.value as TicketStatus)}
                          className="rounded-lg border-0 bg-white py-1.5 pl-3 pr-8 text-sm text-slate-900 shadow-sm ring-1 ring-inset ring-slate-300 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-indigo-600 disabled:bg-slate-100"
                        >
                          {(Object.keys(STATUS_LABELS) as TicketStatus[]).map((status) => (
                            <option key={status} value={status}>
                              {STATUS_LABELS[status]}
                            </option>
                          ))}
                        </select>
                      ) : (
                        <StatusBadge status={ticket.status} />
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </main>
  )
}
