import { useEffect, useRef, useState } from 'react'
import { getChunks, reloadKnowledge, type KnowledgeChunk } from '@/features/knowledge/knowledgeApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import CategoryBadge from '@/shared/components/CategoryBadge'
import PageHeader from '@/shared/components/PageHeader'
import Tabs from '@/shared/components/Tabs'

const ALL = 'All'

// The API returns UTC timestamps without a time zone suffix.
const formatDate = (iso: string) => new Date(iso.endsWith('Z') ? iso : `${iso}Z`).toLocaleString()

export default function KnowledgePage() {
  const [chunks, setChunks] = useState<KnowledgeChunk[]>([])
  const [loading, setLoading] = useState(true)
  const [reloading, setReloading] = useState(false)
  const reloadInFlight = useRef(false)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [activeTab, setActiveTab] = useState(ALL)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    getChunks()
      .then((loaded) => !ignore && setChunks(loaded))
      .catch(
        (err) =>
          !ignore &&
          setError(errorMessage(err, 'Could not load the knowledge data. Make sure the API is running, then try again.')),
      )
      .finally(() => !ignore && setLoading(false))
    return () => {
      ignore = true
    }
  }, [version])

  const reload = () => setVersion((v) => v + 1)

  async function handleReload() {
    // Checked synchronously, so a fast double click cannot start two reloads before the button disables.
    if (reloadInFlight.current) return
    reloadInFlight.current = true

    setError('')
    setNotice('')
    setReloading(true)
    try {
      const result = await reloadKnowledge()
      setNotice(
        `Reloaded ${result.inserted} sections from ${result.documents} documents (replaced ${result.deleted} existing).`,
      )
      reload()
    } catch (err) {
      setError(errorMessage(err, 'Reloading the knowledge data failed. Please try again.'))
    } finally {
      reloadInFlight.current = false
      setReloading(false)
    }
  }

  // One tab per category found in the data, so a new category in the documents gets its own tab automatically.
  const categories = [...new Set(chunks.map((c) => c.category))].sort()
  const selectedTab = categories.includes(activeTab) ? activeTab : ALL
  const visibleChunks = selectedTab === ALL ? chunks : chunks.filter((c) => c.category === selectedTab)
  const tabs = [
    { id: ALL, label: ALL, count: chunks.length },
    ...categories.map((category) => ({
      id: category,
      label: category,
      count: chunks.filter((c) => c.category === category).length,
    })),
  ]

  const documentCount = new Set(chunks.map((c) => c.sourceFile)).size
  const lastIngested = chunks.length > 0 ? chunks.reduce((a, b) => (a.ingestedAt > b.ingestedAt ? a : b)).ingestedAt : null

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader
        title="Knowledge"
        description="The assistant answers from this knowledge base, built from the HR, IT and Finance documents."
        action={
          <Button onClick={handleReload} disabled={reloading}>
            {reloading ? 'Reloading...' : 'Reload knowledge data'}
          </Button>
        }
      />

      <div className="mt-6 space-y-3">
        {reloading && (
          <Alert variant="success" message="Embedding all documents. This can take a minute, please keep this page open." />
        )}
        {notice && <Alert variant="success" message={notice} />}
        {error && <Alert message={error} />}
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-3">
        <Stat label="Sections" value={String(chunks.length)} />
        <Stat label="Documents" value={String(documentCount)} />
        <Stat label="Last reloaded" value={lastIngested ? formatDate(lastIngested) : 'Never'} />
      </div>

      {(loading || chunks.length > 0 || !error) && (
        <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          {loading ? (
            <p className="px-6 py-12 text-center text-sm text-slate-500">Loading...</p>
          ) : chunks.length === 0 ? (
            <p className="px-6 py-12 text-center text-sm text-slate-500">
              The knowledge base is empty. Click "Reload knowledge data" to load the documents.
            </p>
          ) : (
            <>
              <Tabs tabs={tabs} active={selectedTab} onChange={setActiveTab} />
              <table className="w-full text-left text-sm">
                <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
                  <tr>
                    <th className="px-6 py-3 font-medium">Section</th>
                    <th className="hidden px-6 py-3 font-medium md:table-cell">Document</th>
                    {selectedTab === ALL && <th className="px-6 py-3 font-medium">Category</th>}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {visibleChunks.map((chunk) => (
                    <tr
                      key={chunk.id}
                      onClick={() => setExpandedId(expandedId === chunk.id ? null : chunk.id)}
                      className="cursor-pointer align-top hover:bg-slate-50/60"
                    >
                      <td className="px-6 py-4">
                        <p className="font-medium text-slate-900">{chunk.heading}</p>
                        <p className={`mt-1 max-w-xl text-slate-500 ${expandedId === chunk.id ? '' : 'line-clamp-1'}`}>
                          {chunk.content}
                        </p>
                      </td>
                      <td className="hidden px-6 py-4 text-slate-600 md:table-cell">{chunk.documentTitle}</td>
                      {selectedTab === ALL && (
                        <td className="px-6 py-4">
                          <CategoryBadge name={chunk.category} />
                        </td>
                      )}
                    </tr>
                  ))}
                  </tbody>
              </table>
            </>
          )}
        </div>
      )}
    </main>
  )
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="mt-1 text-xl font-semibold text-slate-900">{value}</p>
    </div>
  )
}
