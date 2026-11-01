import { useState } from 'react'
import { testModel, type ModelChoice, type ModelKind } from '@/features/settings/settingsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import Modal from '@/shared/components/Modal'

interface ModelListDialogProps {
  kind: ModelKind
  /** The service or saved model whose models are listed: its address, key name and prefixes are used for every test. */
  choice: ModelChoice & { name: string }
  loading: boolean
  /** Why the list could not be loaded. */
  error: string
  models: string[]
  /** The model in use, when this service is the one in use. */
  current: string
  /** True while something else (a switch) is running. */
  busy: boolean
  onSelect: (model: string) => void
  onClose: () => void
}

/**
 * The models a service lists for our key. Test sends one tiny request to a model (nothing is saved), and a failure is shown in a
 * dialog above this one. Select uses the model.
 */
export default function ModelListDialog({
  kind,
  choice,
  loading,
  error,
  models,
  current,
  busy,
  onSelect,
  onClose,
}: ModelListDialogProps) {
  // 'testing' while the request runs, 'ok' once the model answered. A failed test is removed again and shown in the dialog above.
  const [results, setResults] = useState<Record<string, 'testing' | 'ok'>>({})
  const [failure, setFailure] = useState<{ model: string; message: string } | null>(null)

  async function test(model: string) {
    setResults((r) => ({ ...r, [model]: 'testing' }))
    try {
      await testModel(kind, { ...choice, model })
      setResults((r) => ({ ...r, [model]: 'ok' }))
    } catch (err) {
      setResults(({ [model]: _removed, ...rest }) => rest)
      setFailure({ model, message: errorMessage(err, 'The test failed.') })
    }
  }

  const testing = Object.values(results).includes('testing')

  // The model in use comes first, even when the service no longer lists it.
  const ordered = current ? [current, ...models.filter((m) => m !== current)] : models

  return (
    <>
      <Modal title={`${choice.name}: ${kind === 'embedding' ? 'embedding' : 'chat'} models`} onClose={onClose} maxWidth="max-w-2xl">
        {loading ? (
          <p className="text-sm text-slate-500">Asking the service which models this key can use...</p>
        ) : error ? (
          <Alert message={error} />
        ) : ordered.length === 0 ? (
          <p className="text-sm text-slate-500">The service did not list any models for this key.</p>
        ) : (
          <>
            <p className="text-sm text-slate-600">
              {models.length} {models.length === 1 ? 'model' : 'models'} listed, the one in use first. A service can list models this key cannot use, so
              <strong> Test</strong> a model before you <strong>Select</strong> it.
            </p>
            <ul className="mt-3 max-h-96 divide-y divide-slate-100 overflow-y-auto rounded-lg ring-1 ring-slate-200">
              {ordered.map((name) => (
                <li key={name} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
                  <div className="min-w-0">
                    <code className="break-all text-sm">{name}</code>
                    {name === current && (
                      <span className="ml-2 rounded-full bg-indigo-100 px-2 py-0.5 text-xs font-semibold text-indigo-700">In use</span>
                    )}
                    {name === current && !models.includes(name) && (
                      <span className="ml-2 text-xs text-amber-700">not in the service's list</span>
                    )}
                    {results[name] === 'ok' && <span className="ml-2 text-xs text-emerald-700">✓ works with this key</span>}
                    {results[name] === 'testing' && <span className="ml-2 text-xs text-slate-500">Testing...</span>}
                  </div>
                  <div className="flex gap-2">
                    <Button variant="secondary" size="sm" disabled={testing || busy} onClick={() => test(name)}>
                      Test
                    </Button>
                    <Button size="sm" disabled={busy || testing || name === current} onClick={() => onSelect(name)}>
                      Select
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          </>
        )}
        <div className="mt-5 flex justify-end">
          <Button variant="secondary" onClick={onClose}>
            Close
          </Button>
        </div>
      </Modal>

      {failure && (
        <Modal title={`Test failed: ${failure.model}`} onClose={() => setFailure(null)} maxWidth="max-w-lg">
          <Alert message={failure.message} />
          <p className="mt-3 text-sm text-slate-500">The model was not changed. Pick another model, or check the key and the name.</p>
          <div className="mt-5 flex justify-end">
            <Button onClick={() => setFailure(null)}>Close</Button>
          </div>
        </Modal>
      )}
    </>
  )
}
