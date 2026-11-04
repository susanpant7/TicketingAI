import { useEffect, useState } from 'react'
import { getChunks } from '@/features/knowledge/knowledgeApi'
import AiModelPicker from '@/features/settings/AiModelPicker'
import ManageAppData from '@/features/settings/ManageAppData'
import SettingForm from '@/features/settings/SettingForm'
import { getSettings, type SavedSetting, type Setting } from '@/features/settings/settingsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import PageHeader from '@/shared/components/PageHeader'
import Tabs from '@/shared/components/Tabs'

const tabs = [
  { id: 'ai', label: 'AI assistant' },
  { id: 'data', label: 'Manage App Data' },
]

// Settings are grouped by the first part of their key: "chat.model" is in the "chat" group.
const groupName = (key: string) => key.split('.')[0]

// The AI tab shows one part at a time, so the page stays short. The prompt texts are long, so they have their own part.
const promptKeys = ['chat.system_prompt', 'chat.format_reminder', 'retrieval.rewrite_prompt']

const sections = [
  { id: 'chat', label: 'Chat model' },
  { id: 'embedding', label: 'Embeddings' },
  { id: 'retrieval', label: 'Searching' },
  { id: 'prompt', label: 'Prompt' },
]

const sectionOf = (key: string) =>
  promptKeys.includes(key) ? 'prompt' : ['chat', 'embedding', 'retrieval'].includes(groupName(key)) ? groupName(key) : 'other'

// The order of the settings inside a group. The chat API base URL and the chat model come first and in this order, because
// they must be changed together. A setting that is not listed (one an admin added) goes last, by key.
const settingOrder = [
  'embedding.base_url',
  'embedding.model',
  'embedding.api_key_name',
  'embedding.query_prefix',
  'embedding.document_prefix',
  'retrieval.top_k',
  'retrieval.min_score',
  'retrieval.max_score_gap',
  'retrieval.follow_up_max_words',
  'retrieval.rewrite_follow_ups',
  'retrieval.rewrite_prompt',
  'chat.base_url',
  'chat.model',
  'chat.api_key_name',
  'chat.reasoning_effort',
  'chat.temperature',
  'chat.top_p',
  'chat.max_tokens',
  'chat.num_ctx',
  'chat.keep_alive',
  'chat.history_messages',
  'chat.system_prompt',
  'chat.format_reminder',
]

const position = (key: string) => {
  const index = settingOrder.indexOf(key)
  return index === -1 ? settingOrder.length : index
}

// The chat model and the embedding model in use are edited in the list above their settings (Edit on the row at the top, or on a model),
// so the address, the model, the key name and the prefixes are not shown again as separate rows. The embedding settings are changed only
// there, because a new embedding model converts the stored knowledge.
const hiddenKeys = [
  'chat.base_url',
  'chat.model',
  'chat.api_key_name',
  'embedding.base_url',
  'embedding.model',
  'embedding.api_key_name',
  'embedding.query_prefix',
  'embedding.document_prefix',
]

const bySettingOrder = (a: Setting, b: Setting) => position(a.key) - position(b.key) || a.key.localeCompare(b.key)

export default function SettingsPage() {
  const [tab, setTab] = useState('ai')
  // null until known; the knowledge is only counted to warn that there is none.
  const [knowledgeCount, setKnowledgeCount] = useState<number | null>(null)
  const [knowledgeVersion, setKnowledgeVersion] = useState(0)
  // Whether a chat model and an embedding model are selected (null until known). A setting that is missing counts as not selected.
  const [selected, setSelected] = useState<{ chat: boolean; embedding: boolean } | null>(null)
  const knowledgeChanged = () => setKnowledgeVersion((v) => v + 1)

  useEffect(() => {
    let ignore = false
    getChunks()
      .then((chunks) => !ignore && setKnowledgeCount(chunks.length))
      .catch(() => !ignore && setKnowledgeCount(null))
    getSettings()
      .then((all) => {
        const has = (key: string) => (all.find((s) => s.key === key)?.value ?? '') !== ''
        if (!ignore) setSelected({ chat: has('chat.model'), embedding: has('embedding.model') })
      })
      .catch(() => !ignore && setSelected(null))
    return () => {
      ignore = true
    }
  }, [knowledgeVersion])

  return (
    <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-10">
      {selected && (!selected.chat || !selected.embedding) && (
        <div className="mb-6 space-y-3">
          {!selected.chat && (
            <Alert
              variant="warning"
              message="No chat model is selected, so the assistant cannot answer. Choose one on the AI assistant tab (Chat model)."
            />
          )}
          {!selected.embedding && (
            <Alert
              variant="warning"
              message="No embedding model is selected, so the assistant cannot search the knowledge. Choose one on the AI assistant tab (Embeddings)."
            />
          )}
        </div>
      )}
      {knowledgeCount === 0 && (
        <div role="alert" className="mb-6 rounded-xl bg-red-600 px-4 py-3 text-sm font-medium text-white shadow-sm">
          {selected?.embedding === false
            ? 'The app does not have any knowledge files. Select an embedding model to load the knowledge data.'
            : 'The app does not have any knowledge files. Load them with Manage App Data (Knowledge files).'}
        </div>
      )}
      <PageHeader
        title="Settings"
        description="How the assistant searches the knowledge and writes its answers, and the app's default data."
      />
      <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <Tabs fill tabs={tabs} active={tab} onChange={setTab} />
      </div>
      <div className="mt-6">
        {tab === 'ai' ? <AiSettingsList onKnowledgeChanged={knowledgeChanged} /> : <ManageAppData onChanged={knowledgeChanged} />}
      </div>
    </main>
  )
}

function AiSettingsList({ onKnowledgeChanged }: { onKnowledgeChanged: () => void }) {
  const [settings, setSettings] = useState<Setting[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState<{
    variant: 'success' | 'warning'
    message: React.ReactNode
  } | null>(null)
  const [editing, setEditing] = useState<Setting | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [version, setVersion] = useState(0)
  const [section, setSection] = useState('chat')
  useEffect(() => {
    let ignore = false
    getSettings()
      .then((loaded) => !ignore && setSettings(loaded))
      .catch(
        (err) =>
          !ignore && setError(errorMessage(err, 'Could not load the settings. Make sure the API is running, then try again.')),
      )
      .finally(() => !ignore && setLoading(false))
    return () => {
      ignore = true
    }
  }, [version])

  // The model may have changed the knowledge.
  useEffect(() => {
    onKnowledgeChanged()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [version])

  function openForm(setting: Setting) {
    setEditing(setting)
    setFormOpen(true)
  }

  function handleSaved({ setting }: SavedSetting) {
    setFormOpen(false)
    setError('')
    setVersion((v) => v + 1)
    setNotice({
      variant: 'success',
      message: `Saved "${setting.label}". It applies from the next message.`,
    })
  }

  const hasValue = (key: string) => (settings.find((s) => s.key === key)?.value ?? '') !== ''

  const rowsOf = (id: string) =>
    settings.filter((s) => sectionOf(s.key) === id && !hiddenKeys.includes(s.key)).sort(bySettingOrder)

  // Settings that belong to no part (an old, added one) are shown in a part of their own.
  const parts = rowsOf('other').length > 0 ? [...sections, { id: 'other', label: 'Other' }] : sections
  const current = parts.some((s) => s.id === section) ? section : 'chat'

  return (
    <>
      <div className="space-y-3">
        {notice && <Alert variant={notice.variant} message={notice.message} />}
        {error && <Alert message={error} />}
      </div>

      {loading ? (
        <p className="mt-8 text-center text-sm text-slate-500">Loading settings...</p>
      ) : settings.length === 0 && !error ? (
        <p className="mt-8 text-center text-sm text-slate-500">
          No AI settings are saved yet. Load them on the Manage App Data tab; until then the assistant uses the built-in defaults.
        </p>
      ) : (
        <>
          <nav aria-label="Parts of the AI assistant settings" className="flex flex-wrap gap-2">
            {parts.map((part) => (
              <button
                key={part.id}
                type="button"
                aria-current={part.id === current ? 'page' : undefined}
                onClick={() => setSection(part.id)}
                className={`rounded-full px-4 py-1.5 text-sm font-medium transition ${
                  part.id === current
                    ? 'bg-indigo-600 text-white shadow-sm'
                    : 'bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50 hover:text-slate-900'
                }`}
              >
                {part.label}
                {((part.id === 'chat' && !hasValue('chat.model')) ||
                  (part.id === 'embedding' && !hasValue('embedding.model'))) && (
                  <span className="ml-2 inline-block size-2 rounded-full bg-amber-400" title="No model is selected" />
                )}
              </button>
            ))}
          </nav>

          <section className="mt-4 space-y-4">
            {(current === 'chat' || current === 'embedding') && (
              <AiModelPicker kind={current} settings={settings} onChanged={() => setVersion((v) => v + 1)} />
            )}
            {rowsOf(current).length > 0 && (
              <div className="divide-y divide-slate-100 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                {rowsOf(current).map((setting) => (
                  <div key={setting.key} className="grid gap-2 px-4 py-3 sm:px-5 lg:grid-cols-[1fr_16rem_auto] lg:items-center">
                    <div className="min-w-0">
                      <p className="font-medium text-slate-900">{setting.label}</p>
                      <p className="mt-0.5 line-clamp-2 text-sm text-slate-500" title={setting.description}>
                        {setting.description}
                      </p>
                    </div>
                    <p
                      className={`break-words rounded-lg bg-slate-50 px-3 py-2 font-mono text-sm text-slate-700 ${
                        setting.value.length > 80 || setting.value.includes('\n') ? 'line-clamp-4 whitespace-pre-line' : ''
                      }`}
                    >
                      {setting.value === '' ? '(empty)' : setting.value}
                    </p>
                    <Button variant="ghost" size="sm" onClick={() => openForm(setting)}>
                      Edit
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </section>
        </>
      )}

      {formOpen && (
        <SettingForm key={editing?.key ?? 'new'} setting={editing} onSaved={handleSaved} onCancel={() => setFormOpen(false)} />
      )}
    </>
  )
}
