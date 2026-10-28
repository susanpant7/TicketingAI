import { InboxIcon } from '@/shared/components/icons'
import PageHeader from '@/shared/components/PageHeader'

export default function TicketsPage() {
  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader title="Tickets" description="Your IT and HR support requests." />

      <div className="mt-8 flex flex-col items-center rounded-2xl border border-dashed border-slate-300 bg-white px-6 py-16 text-center">
        <span className="flex size-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
          <InboxIcon className="size-6" />
        </span>
        <h2 className="mt-4 text-base font-semibold text-slate-900">No tickets yet</h2>
        <p className="mt-1 text-sm text-slate-500">Tickets you raise will show up here.</p>
      </div>
    </main>
  )
}
