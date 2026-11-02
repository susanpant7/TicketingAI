import { useEffect, useState, type ReactNode } from 'react'
import AiModelForm from '@/features/settings/AiModelForm'
import ModelListDialog from '@/features/settings/ModelListDialog'
import { loadDefaultDepartments } from '@/features/departments/departmentsApi'
import { loadDefaultKnowledge } from '@/features/knowledge/knowledgeApi'
import ModelTourDialog, { type TourProvider } from '@/features/settings/ModelTourDialog'
import {
  deleteSavedModel,
  getAiProviders,
  getAvailableModels,
  getSavedModels,
  switchChatModel,
  switchEmbeddingModel,
  type AiProvider,
  type AiProvidersInfo,
  type ModelChoice,
  type ModelKind,
  type SaveAiModelRequest,
  type SavedAiModel,
  type Setting,
} from '@/features/settings/settingsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import ConfirmDialog from '@/shared/components/ConfirmDialog'

const sameUrl = (a: string, b: string) => a.replace(/\/+$/, '') === b.replace(/\/+$/, '')

/** The keys of the settings that hold the model in use, for each kind. */
const keys = {
  chat: { baseUrl: 'chat.base_url', model: 'chat.model', apiKeyName: 'chat.api_key_name' },
  embedding: {
    baseUrl: 'embedding.base_url',
    model: 'embedding.model',
    apiKeyName: 'embedding.api_key_name',
  },
}

interface AiModelPickerProps {
  kind: ModelKind
  settings: Setting[]
  /** Called after the settings were changed, so the page loads them again. */
  onChanged: () => void
}

/** One row of the lists: a saved model or a known service. */
interface Choice extends ModelChoice {
  key: string
  name: string
  notes: ReactNode
  hasApiKey: boolean
  needsApiKey: boolean
  saved?: SavedAiModel
}

/**
 * Choosing the chat model or the embedding model: the models the admin saved, and the known services. "View models" then Select sets the address, the
 * model and the key name together. For an embedding model it also converts the stored knowledge to the new model, which takes a while.
 * Only the NAME of a key's variable is shown and saved, never the key.
 */
export default function AiModelPicker({ kind, settings, onChanged }: AiModelPickerProps) {
  const isEmbedding = kind === 'embedding'
  const value = (key: string) => settings.find((s) => s.key === key)?.value ?? ''
  const k = keys[kind]
  const current: ModelChoice = {
    baseUrl: value(k.baseUrl),
    model: value(k.model),
    apiKeyName: value(k.apiKeyName),
  }

  const [info, setInfo] = useState<AiProvidersInfo | null>(null)
  const [saved, setSaved] = useState<SavedAiModel[]>([])
  const [savedVersion, setSavedVersion] = useState(0)
  const [working, setWorking] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [form, setForm] = useState<{
    saved: SavedAiModel | null
    initial?: SaveAiModelRequest
    title?: string
    inUse?: boolean
  } | null>(null)
  const [deleting, setDeleting] = useState<SavedAiModel | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)
  const [confirming, setConfirming] = useState<Choice | null>(null)
  const [confirmingSearch, setConfirmingSearch] = useState(false)
  const [tour, setTour] = useState<'current' | 'beginning' | null>(null)
  const [viewing, setViewing] = useState<{ choice: Choice; loading: boolean; models: string[]; error: string } | null>(null)

  useEffect(() => {
    let ignore = false
    getAiProviders()
      .then((loaded) => !ignore && setInfo(loaded))
      .catch(() => !ignore && setInfo(null))
    return () => {
      ignore = true
    }
    // Asked again when the model in use changes, so the "key is set" answer is for the new model.
  }, [current.baseUrl, current.apiKeyName])

  useEffect(() => {
    let ignore = false
    getSavedModels(kind)
      .then((loaded) => !ignore && setSaved(loaded))
      .catch((err) => !ignore && setError(errorMessage(err, 'Could not load the saved models.')))
    return () => {
      ignore = true
    }
  }, [kind, savedVersion])

  if (!info) return null

  const providers: AiProvider[] = isEmbedding ? info.embeddingProviders : info.providers
  const services: Choice[] = providers.map((p) => ({
    key: `service-${p.name}`,
    name: p.name,
    baseUrl: p.baseUrl,
    model: p.exampleModel,
    apiKeyName: p.apiKeyName,
    hasApiKey: p.hasApiKey,
    needsApiKey: p.needsApiKey,
    notes: (
      <>
        {p.freeTier}{' '}
        <a href={p.signUpUrl} target="_blank" rel="noreferrer" className="font-semibold text-indigo-600 hover:underline">
          {p.needsApiKey ? 'Get a key' : 'Download'}
        </a>
      </>
    ),
  }))
  const savedChoices: Choice[] = saved.map((m) => ({
    key: `saved-${m.id}`,
    name: m.name,
    baseUrl: m.baseUrl,
    model: m.model,
    apiKeyName: m.apiKeyName,
    hasApiKey: m.apiKeyName === '' ? !isOnline(m.baseUrl) || m.hasApiKey : m.hasApiKey,
    needsApiKey: isOnline(m.baseUrl),
    notes: m.notes,
    saved: m,
  }))

  const isCurrent = (c: ModelChoice) => sameUrl(c.baseUrl, current.baseUrl) && c.model === current.model
  const savedInUse = savedChoices.find(isCurrent)
  const serviceInUse = services.find((s) => sameUrl(s.baseUrl, current.baseUrl))
  // The variable the model in use really reads, as the server works it out: its saved key name, else its service's.
  const keyInUse =
    (isEmbedding ? info.embeddingKeyNameInUse : info.chatKeyNameInUse) || current.apiKeyName || serviceInUse?.apiKeyName || ''
  const keyInUseSet = isEmbedding ? info.embeddingKeyInUseSet : info.chatKeyInUseSet
  // The model in use is only shown at the top. The lists below hold the models that are not in use.
  const savedNotInUse = savedChoices.filter((c) => c !== savedInUse)
  const servicesNotInUse = services.filter((c) => savedInUse !== undefined || c !== serviceInUse)

  // The model in use as the first row of the box: the name of the saved model or service it matches, and the key it really reads.
  const inUseChoice: Choice = {
    ...current,
    key: 'in-use',
    name: savedInUse?.name ?? serviceInUse?.name ?? 'Current model',
    apiKeyName: isOnline(current.baseUrl) ? keyInUse : '',
    hasApiKey: keyInUseSet,
    needsApiKey: isOnline(current.baseUrl),
    notes: '',
  }
  // The services the tour goes through: the ones we have a key for (Ollama needs none), then the saved models at other addresses. The
  // service at the address in use is tested with the key the chat is really using.
  const tourProviders: TourProvider[] = [...services, ...savedChoices]
    .map((c): Choice => (sameUrl(c.baseUrl, current.baseUrl) ? { ...c, apiKeyName: keyInUse, hasApiKey: keyInUseSet } : c))
    .filter((c) => c.hasApiKey || !c.needsApiKey)
    .filter((c) => !c.baseUrl.includes('<'))
    .filter((c, i, all) => all.findIndex((other) => sameUrl(other.baseUrl, c.baseUrl)) === i)

  const currentKeyMissing = isOnline(current.baseUrl) && !(isEmbedding ? info.embeddingKeyInUseSet : info.chatKeyInUseSet)

  // Chat: sets the settings. Embedding: converts the stored knowledge to the new model first (it can take a minute), which only saves
  // the settings when it worked.
  // Loads the default knowledge files after the first embedding model is selected. The files belong to the default departments, which
  // are added first when they are missing (the ones that exist are left as they are). A failure is a message, not an error.
  async function loadKnowledge() {
    try {
      await loadDefaultDepartments()
      const results = await loadDefaultKnowledge()
      const failed = results.filter((r) => r.error)
      return failed.length > 0
        ? `${failed.length} of ${results.length} knowledge files were not loaded. ${failed[0].error}`
        : `The ${results.length} default knowledge files were loaded.`
    } catch (err) {
      return `The knowledge files were not loaded: ${errorMessage(err, 'please try again.')} Load the knowledge files on the Knowledge page.`
    }
  }

  async function apply(choice: Choice) {
    setError('')
    setNotice('')
    setConfirming(null)
    setWorking(choice.key)
    try {
      const { reembedded } = isEmbedding ? await switchEmbeddingModel(choice) : await switchChatModel(choice)
      if (isEmbedding && reembedded === 0) {
        // There was no knowledge, so the default knowledge files are loaded with the new model.
        setNotice(`Now using ${choice.model}. Loading the knowledge files...`)
        setNotice(`Now using ${choice.model}. ${await loadKnowledge()}`)
      } else {
        setNotice(
          isEmbedding
            ? `Now using ${choice.model}. The knowledge was cleaned and reinserted: ${reembedded} ${reembedded === 1 ? 'section was' : 'sections were'} embedded again.`
            : `Now using ${choice.model}. It answered a test question, so it works with this key.`,
        )
      }
      onChanged()
    } catch (err) {
      setError(errorMessage(err, 'Could not switch the model. Nothing was changed.'))
    } finally {
      setWorking('')
    }
  }

  const choose = (choice: Choice) => (isEmbedding ? setConfirming(choice) : apply(choice))

  async function viewModels(choice: Choice) {
    setViewing({ choice, loading: true, models: [], error: '' })
    try {
      const { models } = await getAvailableModels(kind, choice.baseUrl, choice.apiKeyName)
      setViewing({ choice, loading: false, models, error: '' })
    } catch (err) {
      setViewing({ choice, loading: false, models: [], error: errorMessage(err, 'Could not ask the service for its models.') })
    }
  }

  async function handleDelete() {
    if (!deleting) return
    setDeleteBusy(true)
    try {
      await deleteSavedModel(kind, deleting.id)
      setSavedVersion((v) => v + 1)
    } catch (err) {
      setError(errorMessage(err, 'Could not delete the model. Please try again.'))
    } finally {
      setDeleteBusy(false)
      setDeleting(null)
    }
  }

  const rowActions = (choice: Choice) => (
    <>
      <Button variant="ghost" size="sm" disabled={working !== ''} onClick={() => viewModels(choice)}>
        View models
      </Button>
      {choice.saved ? (
        <>
          <Button variant="ghost" size="sm" onClick={() => setForm({ saved: choice.saved! })}>
            Edit
          </Button>
          <Button variant="dangerGhost" size="sm" onClick={() => setDeleting(choice.saved!)}>
            Delete
          </Button>
        </>
      ) : (
        <Button
          variant="ghost"
          size="sm"
          onClick={() =>
            setForm({
              saved: null,
              title: `Edit a copy of ${choice.name}`,
              initial: { ...choice, name: `${choice.name} (mine)`, notes: '' },
            })
          }
        >
          Edit
        </Button>
      )}
    </>
  )

  return (
    <div className="mb-4 rounded-2xl border border-indigo-200 bg-indigo-50/60 p-6">
      <h3 className="text-base font-semibold text-slate-900">Choosing the {isEmbedding ? 'embedding' : 'chat'} model</h3>
      <p className="mt-1 text-sm text-slate-600">
        {isEmbedding ? (
          <>
            An address with no path, such as <code>http://localhost:11434</code>, is Ollama on our own machine; one with a path is
            an online service.{' '}
            <strong>Selecting a model cleans the stored knowledge and inserts it again with the new model</strong> (about a
            minute), and is only saved when that worked.
          </>
        ) : (
          <>
            An address with no path, such as <code>http://localhost:11434</code>, is Ollama on our own machine (free, private,
            slower). One with a path is an online service (fast, but the questions and the knowledge are sent to it).
          </>
        )}{' '}
        Each service's key is kept in <code>.env</code> under the <strong>API key name</strong> on its row. Only the name is saved
        here.{' '}
        {isEmbedding ? (
          ''
        ) : (
          <>
            The model list can include models this key cannot use, so <strong>Select</strong> tests the model first.
          </>
        )}
      </p>

      {currentKeyMissing && (
        <div className="mt-3">
          <Alert
            message={
              keyInUse
                ? `The ${isEmbedding ? 'embedding' : 'chat'} model uses an online service, but its key (${keyInUse}) is not set in .env, so it can't work.`
                : `The ${isEmbedding ? 'embedding' : 'chat'} model uses an online service, but no key name is saved for it. Select the model again, or Edit it and set the key name.`
            }
          />
        </div>
      )}
      {working !== '' && !isEmbedding && (
        <div className="mt-3">
          <Alert
            variant="warning"
            message="Testing the model with a small question before switching. It is only used when it answers."
          />
        </div>
      )}
      {working !== '' && isEmbedding && (
        <div className="mt-3">
          <Alert
            variant="warning"
            message="Converting the knowledge to the new model. This can take a minute, please keep this page open."
          />
        </div>
      )}
      {notice && (
        <div className="mt-3">
          <Alert variant="success" message={notice} />
        </div>
      )}
      {error && (
        <div className="mt-3">
          <Alert message={error} />
        </div>
      )}

      <div className="mt-4 flex flex-wrap gap-2">
        <Button size="sm" disabled={working !== '' || tourProviders.length === 0} onClick={() => setTour('current')}>
          Next model
        </Button>
        <Button
          variant="secondary"
          size="sm"
          disabled={working !== '' || tourProviders.length === 0}
          onClick={() => setConfirmingSearch(true)}
        >
          Search model from the beginning
        </Button>
        <span className="self-center text-sm text-slate-500">
          Tries the {isEmbedding ? 'embedding' : 'chat'} models of the services we have a key for, one after another, with the
          response time of each.
        </span>
      </div>

      {current.model === '' ? (
        <div className="mt-4 rounded-xl bg-amber-50 px-4 py-3 text-sm text-amber-900 ring-1 ring-amber-200">
          No {isEmbedding ? 'embedding' : 'chat'} model is selected yet. Choose one below.
          {isEmbedding &&
            ' The knowledge files are not loaded yet: after a model is selected, they are loaded from the Knowledge page.'}
        </div>
      ) : (
        <ul className="mt-4 rounded-xl bg-white px-4 ring-1 ring-indigo-200">
          <Row
            choice={inUseChoice}
            inUse
            actions={
              <>
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={working !== ''}
                  onClick={() =>
                    setForm({
                      saved: null,
                      inUse: true,
                      title: `Edit the ${isEmbedding ? 'embedding' : 'chat'} model in use`,
                      initial: {
                        name: '',
                        ...current,
                        apiKeyName: current.apiKeyName || (isOnline(current.baseUrl) ? (serviceInUse?.apiKeyName ?? '') : ''),
                        notes: '',
                      },
                    })
                  }
                >
                  Edit
                </Button>
                <Button variant="ghost" size="sm" disabled={working !== ''} onClick={() => viewModels(inUseChoice)}>
                  View models
                </Button>
              </>
            }
          />
        </ul>
      )}

      <div className="mt-6 flex items-center justify-between gap-3">
        <p className="text-sm font-semibold text-slate-700">Saved {isEmbedding ? 'embedding' : 'chat'} models</p>
        <Button variant="secondary" size="sm" onClick={() => setForm({ saved: null })}>
          Add {isEmbedding ? 'embedding' : 'chat'} model
        </Button>
      </div>
      {savedNotInUse.length === 0 ? (
        <p className="mt-2 text-sm text-slate-500">
          None yet. Add a model (its address, model and key name) to switch to it later with one click.
        </p>
      ) : (
        <ul className="mt-2 divide-y divide-slate-200">
          {savedNotInUse.map((c) => (
            <Row key={c.key} choice={c} inUse={false} actions={rowActions(c)} />
          ))}
        </ul>
      )}

      <p className="mt-6 text-sm font-semibold text-slate-700">Services we can use</p>
      <ul className="mt-2 divide-y divide-slate-200">
        {servicesNotInUse.map((c) => (
          <Row key={c.key} choice={c} inUse={false} actions={rowActions(c)} />
        ))}
      </ul>

      {tour && (
        <ModelTourDialog
          kind={kind}
          providers={tourProviders}
          current={current}
          start={tour}
          onSelect={(provider, model) => {
            setTour(null)
            choose({ ...provider, model, notes: '', hasApiKey: true, needsApiKey: isOnline(provider.baseUrl) })
          }}
          onClose={() => setTour(null)}
        />
      )}

      {viewing && (
        <ModelListDialog
          kind={kind}
          choice={viewing.choice}
          loading={viewing.loading}
          error={viewing.error}
          models={viewing.models}
          current={sameUrl(viewing.choice.baseUrl, current.baseUrl) ? current.model : ''}
          busy={working !== ''}
          onSelect={(model) => {
            const chosen = viewing.choice
            setViewing(null)
            choose({ ...chosen, model })
          }}
          onClose={() => setViewing(null)}
        />
      )}

      {form && (
        <AiModelForm
          kind={kind}
          saved={form.saved}
          initial={form.initial ?? { name: '', ...current, notes: '' }}
          title={form.title}
          onUse={
            form.inUse
              ? (values) => {
                  setForm(null)
                  choose({
                    ...values,
                    key: 'in-use',
                    name: 'in use',
                    notes: '',
                    hasApiKey: true,
                    needsApiKey: isOnline(values.baseUrl),
                  })
                }
              : undefined
          }
          onSaved={() => {
            setForm(null)
            setSavedVersion((v) => v + 1)
          }}
          onCancel={() => setForm(null)}
        />
      )}

      {deleting && (
        <ConfirmDialog
          title="Delete model"
          message={`Delete "${deleting.name}" from the saved models? The settings are not changed.`}
          confirmLabel="Delete"
          busy={deleteBusy}
          onConfirm={handleDelete}
          onCancel={() => setDeleting(null)}
        />
      )}

      {confirmingSearch && (
        <ConfirmDialog
          title="Search model from the beginning?"
          message={`Every ${isEmbedding ? 'embedding' : 'chat'} model of the services we have a key for is tested one after another, from the first one. It stops at the first model that answers, and nothing is changed until we select it.`}
          confirmLabel="Start searching"
          onConfirm={() => {
            setConfirmingSearch(false)
            setTour('beginning')
          }}
          onCancel={() => setConfirmingSearch(false)}
        />
      )}

      {confirming && (
        <ConfirmDialog
          title="Clean and reinsert the knowledge?"
          message={`The stored knowledge will be cleaned and reinserted with ${confirming.model}: every section is embedded again, because vectors from different models cannot be compared. This can take a minute. If it fails (for example a wrong key), nothing is changed.`}
          confirmLabel="Clean, reinsert and use"
          onConfirm={() => apply(confirming)}
          onCancel={() => setConfirming(null)}
        />
      )}
    </div>
  )
}

// An address with no path (http://localhost:11434) is Ollama's own API: no key is needed.
function isOnline(baseUrl: string) {
  try {
    return new URL(baseUrl).pathname.replace(/\//g, '') !== ''
  } catch {
    return false
  }
}

interface RowProps {
  choice: Choice
  inUse: boolean
  /** The model in use with this address, when it is not the one shown. */
  usedModel?: string
  actions: ReactNode
}

function Row({ choice, inUse, usedModel, actions }: RowProps) {
  return (
    <li className="grid gap-2 py-4 sm:grid-cols-[1fr_auto] sm:items-start sm:gap-4">
      <div className="min-w-0 space-y-1">
        <p className="font-medium text-slate-800">
          {choice.name}
          {inUse && (
            <span className="ml-2 rounded-full bg-indigo-100 px-2 py-0.5 text-xs font-semibold text-indigo-700">In use</span>
          )}
        </p>
        <p className="break-all text-sm text-slate-600">
          Address: <code>{choice.baseUrl}</code>
        </p>
        <p className="break-all text-sm text-slate-600">
          Model: <code>{choice.model}</code>
          {usedModel && <span className="text-slate-400"> (in use: {usedModel})</span>}
        </p>
        <p className="text-sm text-slate-600">
          API key name:{' '}
          {choice.apiKeyName ? (
            <>
              <code>{choice.apiKeyName}</code>{' '}
              {choice.hasApiKey ? (
                <span className="text-emerald-700">✓ set in .env</span>
              ) : (
                <span className="text-amber-700">not set in .env</span>
              )}
            </>
          ) : choice.needsApiKey ? (
            <span className="text-slate-500">none saved (the service's own key is used)</span>
          ) : (
            <span className="text-slate-400">none needed</span>
          )}
        </p>
        {choice.notes && <p className="text-sm text-slate-500">{choice.notes}</p>}
      </div>
      <div className="flex flex-wrap gap-2">{actions}</div>
    </li>
  )
}
