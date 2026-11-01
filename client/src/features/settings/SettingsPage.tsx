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

const groupTitles: Record<string, string> = {
  chat: 'Chat model and replies',
  embedding: 'Embeddings',
  retrieval: 'Searching the knowledge',
}

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

const groupTitle = (group: string) => groupTitles[group] ?? group.charAt(0).toUpperCase() + group.slice(1)

export default function SettingsPage() {
  const [tab, setTab] = useState('ai')
  // null until known; the knowledge is only counted to warn that there is none.
  const [knowledgeCount, setKnowledgeCount] = useState<number | null>(null)
  const [knowledgeVersion, setKnowledgeVersion] = useState(0)
  const knowledgeChanged = () => setKnowledgeVersion((v) => v + 1)

  useEffect(() => {
    let ignore = false
    getChunks()
      .then((chunks) => !ignore && setKnowledgeCount(chunks.length))
      .catch(() => !ignore && setKnowledgeCount(null))
    return () => {
      ignore = true
    }
  }, [knowledgeVersion])

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      {knowledgeCount === 0 && (
        <div role="alert" className="mb-6 rounded-xl bg-red-600 px-4 py-3 text-sm font-medium text-white shadow-sm">
          The app does not have any knowledge files. Select an embedding model to load the knowledge data.
        </div>
      )}
      <PageHeader
        title="Settings"
        description="How the assistant searches the knowledge and writes its answers, and the app's default data."
      />
      <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <Tabs tabs={tabs} active={tab} onChange={setTab} />
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

  const rowsOf = (group: string) =>
    settings.filter((s) => groupName(s.key) === group && !hiddenKeys.includes(s.key)).sort(bySettingOrder)

  const groups = [...new Set(settings.map((s) => groupName(s.key)))].sort()

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
        groups.map((group) => (
          <section key={group} className="mt-8 first:mt-6">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">{groupTitle(group)}</h2>
            {(group === 'chat' || group === 'embedding') && (
              <AiModelPicker kind={group} settings={settings} onChanged={() => setVersion((v) => v + 1)} />
            )}
            {rowsOf(group).length > 0 && (
              <div className="divide-y divide-slate-100 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                {rowsOf(group).map((setting) => (
                  <div key={setting.key} className="grid gap-3 px-6 py-5 sm:grid-cols-[1fr_18rem_auto] sm:items-start">
                    <div className="min-w-0">
                      <p className="font-medium text-slate-900">{setting.label}</p>
                      <p className="mt-1 text-sm text-slate-500">{setting.description}</p>
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
        ))
      )}

      {formOpen && (
        <SettingForm key={editing?.key ?? 'new'} setting={editing} onSaved={handleSaved} onCancel={() => setFormOpen(false)} />
      )}
    </>
  )
}
