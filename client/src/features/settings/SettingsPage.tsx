import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import ManageAppData from '@/features/settings/ManageAppData'
import SettingForm from '@/features/settings/SettingForm'
import { EMBEDDING_MODEL_KEY, getSettings, type SavedSetting, type Setting } from '@/features/settings/settingsApi'
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

const groupTitle = (group: string) => groupTitles[group] ?? group.charAt(0).toUpperCase() + group.slice(1)

export default function SettingsPage() {
  const [tab, setTab] = useState('ai')

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader
        title="Settings"
        description="How the assistant searches the knowledge and writes its answers, and the app's default data."
      />
      <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <Tabs tabs={tabs} active={tab} onChange={setTab} />
      </div>
      <div className="mt-6">{tab === 'ai' ? <AiSettingsList /> : <ManageAppData />}</div>
    </main>
  )
}

function AiSettingsList() {
  const [settings, setSettings] = useState<Setting[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState<{ variant: 'success' | 'warning'; message: React.ReactNode } | null>(null)
  const [editing, setEditing] = useState<Setting | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    getSettings()
      .then((loaded) => !ignore && setSettings(loaded))
      .catch((err) => !ignore && setError(errorMessage(err, 'Could not load the settings. Make sure the API is running, then try again.')))
      .finally(() => !ignore && setLoading(false))
    return () => {
      ignore = true
    }
  }, [version])

  function openForm(setting: Setting | null) {
    setEditing(setting)
    setFormOpen(true)
  }

  function handleSaved({ setting, knowledgeDeleted }: SavedSetting) {
    setFormOpen(false)
    setError('')
    setVersion((v) => v + 1)

    if (setting.key === EMBEDDING_MODEL_KEY && knowledgeDeleted > 0) {
      setNotice({
        variant: 'warning',
        message: (
          <>
            The embedding model was changed and {knowledgeDeleted} knowledge sections were deleted. Load them again on the
            Manage App Data tab, or upload them on the{' '}
            <Link to="/knowledge" className="font-semibold underline">
              Knowledge page
            </Link>
            .
          </>
        ),
      })
    } else {
      setNotice({ variant: 'success', message: `Saved "${setting.label}". It applies from the next message.` })
    }
  }

  const groups = [...new Set(settings.map((s) => groupName(s.key)))].sort()

  return (
    <>
      <div className="mb-6 flex justify-end">
        <Button variant="secondary" onClick={() => openForm(null)}>
          Add setting
        </Button>
      </div>

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
            <div className="divide-y divide-slate-100 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
              {settings
                .filter((s) => groupName(s.key) === group)
                .map((setting) => (
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
          </section>
        ))
      )}

      {formOpen && (
        <SettingForm
          key={editing?.key ?? 'new'}
          setting={editing}
          onSaved={handleSaved}
          onCancel={() => setFormOpen(false)}
        />
      )}
    </>
  )
}
