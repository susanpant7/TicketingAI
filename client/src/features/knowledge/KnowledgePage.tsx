import { useEffect, useState, type SubmitEvent } from 'react'
import { getDepartments, type Department } from '@/features/departments/departmentsApi'
import KnowledgeEditor from '@/features/knowledge/KnowledgeEditor'
import {
  deleteKnowledgeDocument,
  getChunks,
  saveKnowledgeDocument,
  searchKnowledge,
  sectionsOf,
  type KnowledgeChunk,
  type KnowledgeSearchResult,
  type KnowledgeSectionInput,
} from '@/features/knowledge/knowledgeApi'
import SectionForm from '@/features/knowledge/SectionForm'
import UploadForm from '@/features/knowledge/UploadForm'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import CategoryBadge from '@/shared/components/CategoryBadge'
import ConfirmDialog from '@/shared/components/ConfirmDialog'
import PageHeader from '@/shared/components/PageHeader'
import Tabs from '@/shared/components/Tabs'

const ALL = 'All'

// The API returns UTC timestamps without a time zone suffix.
const formatDate = (iso: string) => new Date(iso.endsWith('Z') ? iso : `${iso}Z`).toLocaleString()

/** One section of a document, by its place in the document. index null = a new section at the end. */
type SectionRef = { sourceFile: string; index: number | null }

export default function KnowledgePage() {
  const [chunks, setChunks] = useState<KnowledgeChunk[]>([])
  const [departments, setDepartments] = useState<Department[]>([])
  const [loading, setLoading] = useState(true)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [activeTab, setActiveTab] = useState(ALL)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  // The document being written: null = closed, '' = a new document, otherwise its source file.
  const [editing, setEditing] = useState<string | null>(null)
  const [editingSection, setEditingSection] = useState<SectionRef | null>(null)
  const [deletingSection, setDeletingSection] = useState<SectionRef | null>(null)
  const [deletingDocument, setDeletingDocument] = useState<string | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    Promise.all([getChunks(), getDepartments()])
      .then(([loadedChunks, loadedDepartments]) => {
        if (ignore) return
        setChunks(loadedChunks)
        setDepartments(loadedDepartments)
      })
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

  const refresh = () => setVersion((v) => v + 1)

  const documentOf = (sourceFile: string) => chunks.find((c) => c.sourceFile === sourceFile)
  const titleOf = (sourceFile: string) => documentOf(sourceFile)?.documentTitle ?? sourceFile
  const departmentIdOf = (sourceFile: string) =>
    departments.find((d) => d.name.toLowerCase() === documentOf(sourceFile)?.category.toLowerCase())?.id ?? null

  // A section is changed by saving its whole document again with the new list of sections.
  async function saveSections(sourceFile: string, sections: KnowledgeSectionInput[]) {
    const departmentId = departmentIdOf(sourceFile)
    if (departmentId === null) throw new Error("This document's department no longer exists. Edit the document and choose one.")
    const result = await saveKnowledgeDocument({ sourceFile, title: titleOf(sourceFile), departmentId, sections })
    if (result.error) throw new Error(result.error)
    setError('')
    refresh()
  }

  async function saveSection(section: KnowledgeSectionInput) {
    if (!editingSection) return
    const { sourceFile, index } = editingSection
    const sections = sectionsOf(chunks, sourceFile)
    await saveSections(sourceFile, index === null ? [...sections, section] : sections.map((s, i) => (i === index ? section : s)))
    setNotice(index === null ? `Added "${section.heading}" to ${titleOf(sourceFile)}.` : `Saved "${section.heading}".`)
    setEditingSection(null)
  }

  async function confirmDeleteSection() {
    if (!deletingSection || deletingSection.index === null) return
    const { sourceFile, index } = deletingSection
    const sections = sectionsOf(chunks, sourceFile)
    setBusy(true)
    setNotice('')
    try {
      // The last section goes with its document, since a document needs at least one section.
      if (sections.length === 1) await deleteKnowledgeDocument(sourceFile)
      else await saveSections(sourceFile, sections.filter((_, i) => i !== index))
      setNotice(`Deleted "${sections[index].heading}" from ${titleOf(sourceFile)}.`)
      refresh()
    } catch (err) {
      setError(err instanceof Error && !(err instanceof TypeError) ? err.message : errorMessage(err, 'Could not delete the section.'))
    } finally {
      setBusy(false)
      setDeletingSection(null)
    }
  }

  async function confirmDeleteDocument() {
    if (!deletingDocument) return
    setBusy(true)
    setNotice('')
    try {
      const { deleted } = await deleteKnowledgeDocument(deletingDocument)
      setNotice(`Deleted ${titleOf(deletingDocument)} (${deleted} ${deleted === 1 ? 'section' : 'sections'}).`)
      setEditing(null)
      refresh()
    } catch (err) {
      setError(errorMessage(err, 'Could not delete the document.'))
    } finally {
      setBusy(false)
      setDeletingDocument(null)
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
        description="The assistant answers from this knowledge base: documents uploaded below or written here, one tab per department."
        action={
          <Button disabled={busy || departments.length === 0} onClick={() => setEditing('')}>
            Write knowledge
          </Button>
        }
      />

      <UploadForm onUploaded={refresh} />

      <div className="mt-6 space-y-3">
        {notice && <Alert variant="success" message={notice} />}
        {error && <Alert message={error} />}
      </div>

      <SearchTester />

      <div className="mt-6 grid gap-4 sm:grid-cols-3">
        <Stat label="Sections" value={String(chunks.length)} />
        <Stat label="Documents" value={String(documentCount)} />
        <Stat label="Last uploaded" value={lastIngested ? formatDate(lastIngested) : 'Never'} />
      </div>

      {(loading || chunks.length > 0 || !error) && (
        <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          {loading ? (
            <p className="px-6 py-12 text-center text-sm text-slate-500">Loading...</p>
          ) : chunks.length === 0 ? (
            <p className="px-6 py-12 text-center text-sm text-slate-500">
              The knowledge base is empty. Upload or write documents above, or load the default files in Settings → Manage App Data.
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
                    <th className="px-6 py-3" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {visibleChunks.map((chunk) => {
                    const index = chunks.filter((c) => c.sourceFile === chunk.sourceFile).indexOf(chunk)
                    return (
                      <tr
                        key={chunk.id}
                        onClick={() => setExpandedId(expandedId === chunk.id ? null : chunk.id)}
                        className="cursor-pointer align-top hover:bg-slate-50/60"
                      >
                        <td className="px-6 py-4">
                          <p className="font-medium text-slate-900">{chunk.heading}</p>
                          <p
                            className={`mt-1 max-w-xl whitespace-pre-wrap text-slate-500 ${expandedId === chunk.id ? '' : 'line-clamp-1'}`}
                          >
                            {chunk.content}
                          </p>
                        </td>
                        <td className="hidden px-6 py-4 md:table-cell">
                          <p className="text-slate-600">{chunk.documentTitle}</p>
                          <div className="mt-1 flex gap-3 text-xs" onClick={(e) => e.stopPropagation()}>
                            <button
                              type="button"
                              disabled={busy}
                              className="font-medium text-indigo-600 hover:text-indigo-500"
                              onClick={() => setEditing(chunk.sourceFile)}
                            >
                              Edit document
                            </button>
                            <button
                              type="button"
                              disabled={busy}
                              className="font-medium text-indigo-600 hover:text-indigo-500"
                              onClick={() => setEditingSection({ sourceFile: chunk.sourceFile, index: null })}
                            >
                              Add section
                            </button>
                          </div>
                        </td>
                        {selectedTab === ALL && (
                          <td className="px-6 py-4">
                            <CategoryBadge name={chunk.category} />
                          </td>
                        )}
                        <td className="px-6 py-3">
                          <div className="flex justify-end gap-1" onClick={(e) => e.stopPropagation()}>
                            <Button
                              variant="ghost"
                              size="sm"
                              disabled={busy}
                              onClick={() => setEditingSection({ sourceFile: chunk.sourceFile, index })}
                            >
                              Edit
                            </Button>
                            <Button
                              variant="dangerGhost"
                              size="sm"
                              disabled={busy}
                              onClick={() => setDeletingSection({ sourceFile: chunk.sourceFile, index })}
                            >
                              Delete
                            </Button>
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </>
          )}
        </div>
      )}

      {editing !== null && (
        <KnowledgeEditor
          key={editing}
          sourceFile={editing || null}
          initialTitle={editing ? titleOf(editing) : ''}
          initialDepartmentId={editing ? departmentIdOf(editing) : null}
          initialSections={editing ? sectionsOf(chunks, editing) : []}
          departments={departments}
          onSaved={(result) => {
            setEditing(null)
            setError('')
            setNotice(`${editing ? titleOf(editing) : 'The document'} is saved: ${result.sections} sections stored.`)
            refresh()
          }}
          onDelete={editing ? () => setDeletingDocument(editing) : undefined}
          onCancel={() => setEditing(null)}
        />
      )}

      {editingSection && (
        <SectionForm
          documentTitle={titleOf(editingSection.sourceFile)}
          section={editingSection.index === null ? null : sectionsOf(chunks, editingSection.sourceFile)[editingSection.index]}
          onSave={saveSection}
          onCancel={() => setEditingSection(null)}
        />
      )}

      {deletingSection && deletingSection.index !== null && (
        <ConfirmDialog
          title="Delete section"
          message={
            sectionsOf(chunks, deletingSection.sourceFile).length === 1
              ? `"${sectionsOf(chunks, deletingSection.sourceFile)[0].heading}" is the only section of ${titleOf(deletingSection.sourceFile)}, so the whole document is deleted. This cannot be undone.`
              : `Delete "${sectionsOf(chunks, deletingSection.sourceFile)[deletingSection.index].heading}" from ${titleOf(deletingSection.sourceFile)}? The assistant will no longer answer from it. This cannot be undone.`
          }
          confirmLabel="Delete"
          busy={busy}
          onConfirm={confirmDeleteSection}
          onCancel={() => setDeletingSection(null)}
        />
      )}

      {deletingDocument && (
        <ConfirmDialog
          title="Delete document"
          message={`Delete every section of ${titleOf(deletingDocument)}? The assistant will no longer answer from it. This cannot be undone.`}
          confirmLabel="Delete"
          busy={busy}
          onConfirm={confirmDeleteDocument}
          onCancel={() => setDeletingDocument(null)}
        />
      )}
    </main>
  )
}

/** Shows which sections a question would find, and how well each matches (0 to 1). */
function SearchTester() {
  const [query, setQuery] = useState('')
  const [found, setFound] = useState<KnowledgeSearchResult[] | null>(null)
  const [error, setError] = useState('')
  const [searching, setSearching] = useState(false)

  async function handleSearch(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setSearching(true)
    try {
      setFound(await searchKnowledge(query.trim()))
    } catch (err) {
      setFound(null)
      setError(errorMessage(err, 'The search failed. Please try again.'))
    } finally {
      setSearching(false)
    }
  }

  return (
    <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <h2 className="text-base font-semibold text-slate-900">Test a question</h2>
      <p className="mt-1 text-sm text-slate-500">
        See which sections the assistant would find for a question. Use the scores to tune the minimum similarity on the Settings page.
      </p>
      <form onSubmit={handleSearch} className="mt-4 flex gap-3">
        <input
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="How many leave days do I get?"
          maxLength={500}
          aria-label="Question"
          className="block w-full rounded-lg border-0 bg-white px-3 py-2.5 text-sm text-slate-900 shadow-sm ring-1 ring-inset ring-slate-300 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-indigo-600"
        />
        <Button type="submit" disabled={searching || query.trim().length === 0}>
          {searching ? 'Searching...' : 'Search'}
        </Button>
      </form>
      {error && (
        <div className="mt-4">
          <Alert message={error} />
        </div>
      )}
      {found && (
        <ul className="mt-4 divide-y divide-slate-100 rounded-lg ring-1 ring-inset ring-slate-200">
          {found.length === 0 && <li className="px-4 py-3 text-sm text-slate-500">Nothing found.</li>}
          {found.map((result) => (
            <li key={result.id} className="flex items-start justify-between gap-4 px-4 py-3 text-sm">
              <div className="min-w-0">
                <p className="font-medium text-slate-900">{result.heading}</p>
                <p className="text-slate-500">
                  {result.documentTitle} · {result.category}
                </p>
              </div>
              <span className="shrink-0 rounded-full bg-slate-100 px-2.5 py-1 font-mono text-xs text-slate-700">
                {result.score.toFixed(3)}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
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
