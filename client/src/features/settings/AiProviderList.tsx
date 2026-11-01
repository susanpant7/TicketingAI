import { useEffect, useState, type ReactNode } from 'react'
import ChatModelForm from '@/features/settings/ChatModelForm'
import {
  deleteChatModel,
  getAiProviders,
  getChatModels,
  updateSetting,
  type AiProvidersInfo,
  type SavedChatModel,
  type Setting,
} from '@/features/settings/settingsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import ConfirmDialog from '@/shared/components/ConfirmDialog'

const BASE_URL_KEY = 'chat.base_url'
const MODEL_KEY = 'chat.model'

const sameUrl = (a: string, b: string) => a.replace(/\/+$/, '') === b.replace(/\/+$/, '')

// An address with no path (http://localhost:11434) is Ollama's own API: no key is needed.
const isOllama = (baseUrl: string) => {
  try {
    return new URL(baseUrl).pathname.replace(/\//g, '') === ''
  } catch {
    return false
  }
}

interface AiProviderListProps {
  settings: Setting[]
  /** Called after the chat settings were changed, so the page loads them again. */
  onChanged: () => void
}

/**
 * Choosing the chat model: the models the admin saved, and the known services. "Use" sets the Chat API base URL and the Chat
 * model settings to that row's values. The Chat API base URL alone decides how the model is called, so there is nothing else
 * to switch.
 */
export default function AiProviderList({ settings, onChanged }: AiProviderListProps) {
  const baseUrlSetting = settings.find((s) => s.key === BASE_URL_KEY)
  const modelSetting = settings.find((s) => s.key === MODEL_KEY)
  const baseUrl = baseUrlSetting?.value ?? ''
  const model = modelSetting?.value ?? ''

  const [info, setInfo] = useState<AiProvidersInfo | null>(null)
  const [saved, setSaved] = useState<SavedChatModel[]>([])
  const [savedVersion, setSavedVersion] = useState(0)
  const [switching, setSwitching] = useState('')
  const [error, setError] = useState('')
  const [form, setForm] = useState<{ chatModel: SavedChatModel | null } | null>(null)
  const [deleting, setDeleting] = useState<SavedChatModel | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)

  useEffect(() => {
    let ignore = false
    getAiProviders()
      .then((loaded) => !ignore && setInfo(loaded))
      .catch(() => !ignore && setInfo(null))
    return () => {
      ignore = true
    }
  }, [])

  useEffect(() => {
    let ignore = false
    getChatModels()
      .then((loaded) => !ignore && setSaved(loaded))
      .catch((err) => !ignore && setError(errorMessage(err, 'Could not load the saved chat models.')))
    return () => {
      ignore = true
    }
  }, [savedVersion])

  if (!info || !baseUrlSetting || !modelSetting) return null

  const savedInUse = saved.find((m) => sameUrl(m.baseUrl, baseUrl) && m.model === model)
  const serviceInUse = info.providers.find((p) => sameUrl(p.baseUrl, baseUrl))
  const usesKey = !isOllama(baseUrl)

  async function use(key: string, useBaseUrl: string, useModel: string) {
    if (!baseUrlSetting || !modelSetting) return
    setError('')
    setSwitching(key)
    try {
      const { key: urlKey, ...url } = baseUrlSetting
      const { key: modelKey, ...chosen } = modelSetting
      await updateSetting(urlKey, { ...url, value: useBaseUrl })
      await updateSetting(modelKey, { ...chosen, value: useModel })
      onChanged()
    } catch (err) {
      setError(errorMessage(err, 'Could not switch the chat model. Please try again.'))
    } finally {
      setSwitching('')
    }
  }

  async function handleDelete() {
    if (!deleting) return
    setDeleteBusy(true)
    try {
      await deleteChatModel(deleting.id)
      setSavedVersion((v) => v + 1)
    } catch (err) {
      setError(errorMessage(err, 'Could not delete the chat model. Please try again.'))
    } finally {
      setDeleteBusy(false)
      setDeleting(null)
    }
  }

  const renderUseButton = (key: string, useBaseUrl: string, useModel: string) => (
    <Button
      variant="secondary"
      size="sm"
      disabled={switching !== '' || (sameUrl(useBaseUrl, baseUrl) && useModel === model)}
      onClick={() => use(key, useBaseUrl, useModel)}
    >
      {switching === key ? 'Switching...' : 'Use'}
    </Button>
  )

  return (
    <div className="mb-4 rounded-2xl border border-indigo-200 bg-indigo-50/60 p-6">
      <h3 className="text-base font-semibold text-slate-900">Choosing the chat model</h3>
      <div className="mt-1 space-y-1 text-sm text-slate-600">
        <p>
          The chat model is set by two settings below: the <strong>Chat API base URL</strong> and the <strong>Chat model</strong>. An
          address with no path, such as <code>http://localhost:11434</code>, is a model on our own machine through Ollama (free,
          private, slower: 15 to 40 seconds on a CPU). An address with a path, such as Gemini's, is an online service (fast, but the
          questions and the knowledge are sent to it, and a free plan has limits).
        </p>
        <p>
          To switch, click <strong>Use</strong> on a saved model or a service below, or type the address and the model in the
          settings. An online service also needs its key in <code>{info.apiKeyName}</code> in the server's <code>.env</code> file;
          restart the server after adding it. Models are renamed from time to time, so check the service's list if replies fail.
        </p>
      </div>

      <p className="mt-3 text-sm">
        {info.hasApiKey ? (
          <span className="text-emerald-700">✓ {info.apiKeyName} is set</span>
        ) : (
          <span className="text-slate-500">{info.apiKeyName} is not set</span>
        )}
      </p>
      {usesKey && !info.hasApiKey && (
        <div className="mt-3">
          <Alert
            message={`The chat uses an online service (${serviceInUse?.name ?? baseUrl}), but ${info.apiKeyName} is not set in .env, so the assistant can't reply.`}
          />
        </div>
      )}
      {error && (
        <div className="mt-3">
          <Alert message={error} />
        </div>
      )}

      <div className="mt-6 flex items-center justify-between gap-3">
        <p className="text-sm font-semibold text-slate-700">Saved chat models</p>
        <Button variant="secondary" size="sm" onClick={() => setForm({ chatModel: null })}>
          Add chat model
        </Button>
      </div>
      {saved.length === 0 ? (
        <p className="mt-2 text-sm text-slate-500">
          None yet. Add a chat model (its address and model) to switch to it later with one click.
        </p>
      ) : (
        <ul className="mt-2 divide-y divide-slate-200">
          {saved.map((m) => (
            <Row
              key={m.id}
              name={m.name}
              inUse={savedInUse === m}
              baseUrl={m.baseUrl}
              model={m.model}
              notes={m.notes}
              actions={
                <>
                  {renderUseButton(`saved-${m.id}`, m.baseUrl, m.model)}
                  <Button variant="ghost" size="sm" onClick={() => setForm({ chatModel: m })}>
                    Edit
                  </Button>
                  <Button variant="dangerGhost" size="sm" onClick={() => setDeleting(m)}>
                    Delete
                  </Button>
                </>
              }
            />
          ))}
        </ul>
      )}

      <p className="mt-6 text-sm font-semibold text-slate-700">Services we can use</p>
      <ul className="mt-2 divide-y divide-slate-200">
        {info.providers.map((provider) => (
          <Row
            key={provider.name}
            name={provider.name}
            inUse={!savedInUse && serviceInUse === provider}
            baseUrl={provider.baseUrl}
            model={provider.exampleModel}
            usedModel={serviceInUse === provider && model !== provider.exampleModel ? model : undefined}
            notes={
              <>
                {provider.freeTier}{' '}
                <a
                  href={provider.signUpUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="font-semibold text-indigo-600 hover:underline"
                >
                  {provider.needsApiKey ? 'Get a key' : 'Download'}
                </a>
              </>
            }
            actions={renderUseButton(provider.name, provider.baseUrl, provider.exampleModel)}
          />
        ))}
      </ul>

      {form && (
        <ChatModelForm
          chatModel={form.chatModel}
          initial={{ name: '', baseUrl, model, notes: '' }}
          onSaved={() => {
            setForm(null)
            setSavedVersion((v) => v + 1)
          }}
          onCancel={() => setForm(null)}
        />
      )}

      {deleting && (
        <ConfirmDialog
          title="Delete chat model"
          message={`Delete "${deleting.name}" from the saved chat models? The chat settings are not changed.`}
          confirmLabel="Delete"
          busy={deleteBusy}
          onConfirm={handleDelete}
          onCancel={() => setDeleting(null)}
        />
      )}
    </div>
  )
}

interface RowProps {
  name: string
  inUse: boolean
  baseUrl: string
  model: string
  /** The model in use with this address, when it is not the one shown. */
  usedModel?: string
  notes: ReactNode
  actions: ReactNode
}

function Row({ name, inUse, baseUrl, model, usedModel, notes, actions }: RowProps) {
  return (
    <li className="grid gap-2 py-4 sm:grid-cols-[1fr_auto] sm:items-start sm:gap-4">
      <div className="min-w-0 space-y-1">
        <p className="font-medium text-slate-800">
          {name}
          {inUse && (
            <span className="ml-2 rounded-full bg-indigo-100 px-2 py-0.5 text-xs font-semibold text-indigo-700">In use</span>
          )}
        </p>
        <p className="break-all text-sm text-slate-600">
          Address: <code>{baseUrl}</code>
        </p>
        <p className="break-all text-sm text-slate-600">
          Model: <code>{model}</code>
          {usedModel && <span className="text-slate-400"> (in use: {usedModel})</span>}
        </p>
        {notes && <p className="text-sm text-slate-500">{notes}</p>}
      </div>
      <div className="flex flex-wrap gap-2">{actions}</div>
    </li>
  )
}
