import { useEffect, useState } from 'react'
import {
  deleteAllChunks,
  getChunks,
  ingestDocuments,
  type KnowledgeChunk,
} from '@/features/knowledge/knowledgeApi'
import { ApiError } from '@/shared/api/apiClient'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import CategoryBadge from '@/shared/components/CategoryBadge'
import ConfirmDialog from '@/shared/components/ConfirmDialog'
import PageHeader from '@/shared/components/PageHeader'

// The API returns UTC timestamps without a time zone suffix.
const formatDate = (iso: string) => new Date(iso.endsWith('Z') ? iso : `${iso}Z`).toLocaleString()

export default function KnowledgePage() {
  const [chunks, setChunks] = useState<KnowledgeChunk[]>([])
  const [loading, setLoading] = useState(true)
  const [ingesting, setIngesting] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    getChunks()
      .then((loaded) => !ignore && setChunks(loaded))
      .catch(() => !ignore && setError('Failed to load knowledge chunks.'))
      .finally(() => !ignore && setLoading(false))
    return () => {
      ignore = true
    }
  }, [version])

  const reload = () => setVersion((v) => v + 1)

  async function handleIngest() {
    setError('')
    setNotice('')
    setIngesting(true)
    try {
      const result = await ingestDocuments()
      setNotice(
        `Ingested ${result.inserted} sections from ${result.documents} documents (replaced ${result.deleted} existing).`,
      )
      reload()
    } catch (err) {
      setError(err instanceof ApiError && err.status !== 500 ? err.message : 'Ingestion failed. Please try again.')
    } finally {
      setIngesting(false)
    }
  }

  async function handleDeleteAll() {
    setError('')
    setNotice('')
    setDeleting(true)
    try {
      const { deleted } = await deleteAllChunks()
      setNotice(`Deleted ${deleted} sections.`)
      reload()
    } catch {
      setError('Failed to delete knowledge chunks.')
    } finally {
      setDeleting(false)
      setConfirmDelete(false)
    }
  }

  const documentCount = new Set(chunks.map((c) => c.sourceFile)).size
  const lastIngested = chunks.length > 0 ? chunks.reduce((a, b) => (a.ingestedAt > b.ingestedAt ? a : b)).ingestedAt : null

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader
        title="Knowledge"
        description="Load the HR, IT and Finance documents into the knowledge base that the assistant searches."
        action={
          <div className="flex gap-3">
            <Button variant="secondary" onClick={() => setConfirmDelete(true)} disabled={ingesting || chunks.length === 0}>
              Delete all
            </Button>
            <Button onClick={handleIngest} disabled={ingesting}>
              {ingesting ? 'Ingesting...' : 'Ingest documents'}
            </Button>
          </div>
        }
      />

      <div className="mt-6 space-y-3">
        {ingesting && (
          <Alert variant="success" message="Embedding all documents. This can take a minute, please keep this page open." />
        )}
        {notice && <Alert variant="success" message={notice} />}
        {error && <Alert message={error} />}
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-3">
        <Stat label="Sections" value={String(chunks.length)} />
        <Stat label="Documents" value={String(documentCount)} />
        <Stat label="Last ingested" value={lastIngested ? formatDate(lastIngested) : 'Never'} />
      </div>

      <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        {loading ? (
          <p className="px-6 py-12 text-center text-sm text-slate-500">Loading...</p>
        ) : chunks.length === 0 ? (
          <p className="px-6 py-12 text-center text-sm text-slate-500">
            The knowledge base is empty. Click "Ingest documents" to load the documents.
          </p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-6 py-3 font-medium">Section</th>
                <th className="hidden px-6 py-3 font-medium md:table-cell">Document</th>
                <th className="px-6 py-3 font-medium">Category</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {chunks.map((chunk) => (
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
                  <td className="px-6 py-4">
                    <CategoryBadge name={chunk.category} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {confirmDelete && (
        <ConfirmDialog
          title="Delete all knowledge"
          message={`This deletes all ${chunks.length} sections from the knowledge base. The assistant will have nothing to search until you ingest the documents again.`}
          confirmLabel="Delete all"
          busy={deleting}
          onConfirm={handleDeleteAll}
          onCancel={() => setConfirmDelete(false)}
        />
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
