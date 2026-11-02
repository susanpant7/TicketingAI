import { useEffect, useState } from 'react'
import { loadDefaultDepartments } from '@/features/departments/departmentsApi'
import { loadDefaultKnowledge } from '@/features/knowledge/knowledgeApi'
import { cleanUpData, restoreDefaults } from '@/features/settings/settingsApi'
import { loadDefaultUsers } from '@/features/users/usersApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import ConfirmDialog from '@/shared/components/ConfirmDialog'

type Added = { added: number; skipped: number }

// A failure whose message is written for the admin (unlike other errors, which errorMessage hides).
class StepError extends Error {}

const failureText = (err: unknown, fallback: string) => (err instanceof StepError ? err.message : errorMessage(err, fallback))

/** One kind of default data. `run` loads it and returns what to tell the admin. */
interface Module {
  id: string
  title: string
  description: string
  button: string
  /** Replaces what is there (asks first), instead of only adding what is missing. */
  replaces?: boolean
  /** Asks for the password the loaded accounts sign in with, starting with this one. */
  password?: { label: string; initial: string }
  /** Left out of "Load all default data" (loaded on its own, later). */
  separate?: boolean
  run: (password: string) => Promise<string>
}

const addedText = ({ added, skipped }: Added, one: string, many: string) =>
  added === 0
    ? `The default ${many} already exist.`
    : `Added ${added} default ${added === 1 ? one : many}${skipped > 0 ? ` (${skipped} already existed)` : ''}.`

const modules: Module[] = [
  {
    id: 'settings',
    title: 'AI assistant settings',
    description:
      'The system prompt and the search settings. Replaces every setting, including ones you added. No chat or embedding model is selected: they are chosen on the Settings page. If an embedding model was selected, the stored knowledge is deleted.',
    button: 'Load default AI assistant data',
    replaces: true,
    run: async () => {
      await restoreDefaults()
      return 'The default AI assistant settings are loaded.'
    },
  },
  {
    id: 'departments',
    title: 'Departments',
    description: 'HR, IT and Finance, with what each handles and how to contact it. Ones that already exist are skipped.',
    button: 'Load default departments',
    run: async () => addedText(await loadDefaultDepartments(), 'department', 'departments'),
  },
  {
    id: 'users',
    title: 'Users',
    description: 'Demo users for every department, who sign in with the password below. Needs the departments first.',
    button: 'Load default users',
    password: { label: 'Password for the default users', initial: 'user' },
    run: async (password) => addedText(await loadDefaultUsers(password), 'user', 'users'),
  },
  {
    id: 'knowledge',
    title: 'Knowledge files',
    description:
      'The bundled HR, IT and Finance documents the assistant answers from. A document with the same name is replaced. Needs the departments first, and an embedding model selected on the Settings page.',
    button: 'Load default knowledge files',
    separate: true,
    run: async () => {
      const results = await loadDefaultKnowledge()
      // Usually every file fails for the same reason (Ollama not running), so each reason is shown once.
      const reasons = [...new Set(results.map((r) => r.error).filter(Boolean))]
      if (reasons.length > 0) {
        const failed = results.filter((r) => r.error).length
        throw new StepError(`${failed} of ${results.length} knowledge files were not loaded. ${reasons.join(' ')}`)
      }
      return `Loaded ${results.length} knowledge files.`
    },
  },
]

type StepState = 'waiting' | 'running' | 'done' | 'failed'
type Step = { id: string; title: string; state: StepState }

const loadAllModules = modules.filter((m) => !m.separate)
const allSteps = [{ id: 'cleanup', title: 'Clean up all data' }, ...loadAllModules.map((m) => ({ id: m.id, title: m.title }))]

const stepMarks: Record<StepState, { mark: string; className: string }> = {
  waiting: { mark: '○', className: 'text-slate-300' },
  running: { mark: '◌', className: 'animate-spin text-indigo-600' },
  done: { mark: '✓', className: 'text-emerald-600' },
  failed: { mark: '✕', className: 'text-red-600' },
}

/** Loading and clearing the default data for every part of the app, in one place. */
export default function ManageAppData({ onChanged }: { onChanged: () => void }) {
  const [passwords, setPasswords] = useState<Record<string, string>>(() =>
    Object.fromEntries(modules.filter((m) => m.password).map((m) => [m.id, m.password!.initial])),
  )
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [steps, setSteps] = useState<Step[]>([])
  const [confirming, setConfirming] = useState<'all' | 'cleanup' | Module | null>(null)

  // Cleaning up or loading may have changed the knowledge.
  useEffect(() => {
    if (!busy) onChanged()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [busy])

  const passwordFor = (module: Module) => passwords[module.id] ?? ''
  const missingPassword = (module: Module) => module.password !== undefined && passwordFor(module).length === 0

  function start() {
    setConfirming(null)
    setError('')
    setNotice('')
    setBusy(true)
  }

  async function runOne(module: Module) {
    start()
    setSteps([])
    try {
      setNotice(await module.run(passwordFor(module)))
    } catch (err) {
      setError(failureText(err, `Could not load the ${module.title.toLowerCase()}. Please try again.`))
    } finally {
      setBusy(false)
    }
  }

  async function cleanUp() {
    start()
    setSteps([])
    try {
      await cleanUpData()
      setNotice('All data is cleaned up.')
    } catch (err) {
      setError(errorMessage(err, 'Could not clean up the data. Please try again.'))
    } finally {
      setBusy(false)
    }
  }

  // Cleans up, then loads every part in order, stopping at the first one that fails.
  async function loadAll() {
    start()
    const progress: Step[] = allSteps.map((step) => ({ ...step, state: 'waiting' }))
    const mark = (index: number, state: StepState) => {
      progress[index] = { ...progress[index], state }
      setSteps([...progress])
    }
    setSteps([...progress])

    const actions = [() => cleanUpData(), ...loadAllModules.map((module) => () => module.run(passwordFor(module)))]
    for (const [index, action] of actions.entries()) {
      mark(index, 'running')
      try {
        await action()
        mark(index, 'done')
      } catch (err) {
        mark(index, 'failed')
        setError(failureText(err, `${progress[index].title} did not work. Please try again.`))
        setBusy(false)
        return
      }
    }
    setNotice(
      'The default data is loaded, without the knowledge files. No chat or embedding model is selected: choose them on the Settings page (AI assistant tab). After an embedding model is selected, load the knowledge files on the Knowledge page or below.',
    )
    setBusy(false)
  }

  return (
    <div className="space-y-6">
      <section className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="min-w-0 max-w-2xl">
            <h2 className="font-semibold text-slate-900">Load all default data</h2>
            <p className="mt-1 text-sm text-slate-500">
              Cleans up all data first, then loads the default AI assistant settings, departments and users. The knowledge files
              and the chat and embedding models are not loaded: they come later, after a model is selected.
            </p>
          </div>
          <Button disabled={busy || modules.some(missingPassword)} onClick={() => setConfirming('all')}>
            Load all default data
          </Button>
        </div>

        {steps.length > 0 && (
          <ol className="mt-5 space-y-2">
            {steps.map((step) => (
              <li key={step.id} className="flex items-center gap-2 text-sm text-slate-700">
                <span aria-hidden="true" className={`inline-block w-4 text-center ${stepMarks[step.state].className}`}>
                  {stepMarks[step.state].mark}
                </span>
                {step.title}
                <span className="sr-only">({step.state})</span>
              </li>
            ))}
          </ol>
        )}
      </section>

      {notice && <Alert variant="success" message={notice} />}
      {error && <Alert message={error} />}

      <section>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">Load default data for one part</h2>
        <div className="divide-y divide-slate-100 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
          {modules.map((module) => (
            <div key={module.id} className="grid gap-3 px-4 py-5 sm:px-6 sm:grid-cols-[1fr_auto] sm:items-center">
              <div className="min-w-0">
                <p className="font-medium text-slate-900">{module.title}</p>
                <p className="mt-1 text-sm text-slate-500">{module.description}</p>
                {module.password && (
                  <label className="mt-3 flex max-w-xs flex-col gap-1 text-sm font-medium text-slate-700">
                    {module.password.label}
                    <input
                      type="text"
                      autoComplete="off"
                      value={passwordFor(module)}
                      onChange={(e) => setPasswords({ ...passwords, [module.id]: e.target.value })}
                      className="rounded-lg border-0 px-3 py-2 text-sm font-normal shadow-sm ring-1 ring-inset ring-slate-300"
                    />
                  </label>
                )}
              </div>
              <Button
                variant="secondary"
                disabled={busy || missingPassword(module)}
                onClick={() => (module.replaces ? setConfirming(module) : void runOne(module))}
              >
                {module.button}
              </Button>
            </div>
          ))}
        </div>
      </section>

      <section className="rounded-2xl border border-red-200 bg-white p-4 shadow-sm sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="min-w-0 max-w-2xl">
            <h2 className="font-semibold text-red-700">Clean up data</h2>
            <p className="mt-1 text-sm text-slate-500">
              Deletes everything, as on a fresh start. You stay signed in as the admin.
            </p>
          </div>
          <Button variant="danger" disabled={busy} onClick={() => setConfirming('cleanup')}>
            Clean up data
          </Button>
        </div>
      </section>

      {confirming === 'all' && (
        <ConfirmDialog
          title="Load all default data"
          message="This first deletes everything (tickets, users, knowledge, departments and settings), then loads the defaults (without the knowledge files and without a chat or embedding model). Your own changes cannot be recovered."
          confirmLabel="Load all default data"
          busy={busy}
          onConfirm={loadAll}
          onCancel={() => setConfirming(null)}
        />
      )}

      {confirming === 'cleanup' && (
        <ConfirmDialog
          title="Clean up all data"
          message="Delete everything: tickets, users, knowledge, departments and settings? This cannot be undone."
          confirmLabel="Clean up data"
          busy={busy}
          onConfirm={cleanUp}
          onCancel={() => setConfirming(null)}
        />
      )}

      {confirming !== null && typeof confirming === 'object' && (
        <ConfirmDialog
          title={confirming.button}
          message={`Replace the current ${confirming.title.toLowerCase()} with the defaults? Your changes will be lost.`}
          confirmLabel={confirming.button}
          busy={busy}
          onConfirm={() => runOne(confirming)}
          onCancel={() => setConfirming(null)}
        />
      )}
    </div>
  )
}
