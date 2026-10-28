const highlights = [
  { title: 'Tickets', text: 'Track and manage your IT and HR support requests.' },
  { title: 'AI Chat', text: 'Ask questions and let the assistant raise a ticket for you.' },
]

export default function HomePage() {
  return (
    <main className="mx-auto max-w-3xl px-6 py-16 text-center">
      <h1 className="text-4xl font-bold">Ticketing AI</h1>
      <p className="mt-2 text-gray-600">Your IT and HR support assistant.</p>
      <div className="mt-8 grid grid-cols-1 gap-4 text-left sm:grid-cols-2">
        {highlights.map(({ title, text }) => (
          <section key={title} className="rounded-lg border border-gray-200 p-5">
            <h2 className="text-lg font-semibold">{title}</h2>
            <p className="mt-2 text-gray-600">{text}</p>
          </section>
        ))}
      </div>
    </main>
  )
}
