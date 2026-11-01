import { useState, type SubmitEvent } from 'react'
import {
  addSavedModel,
  updateSavedModel,
  type ModelKind,
  type SaveAiModelRequest,
  type SavedAiModel,
} from '@/features/settings/settingsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { TextAreaField, TextField } from '@/shared/components/Field'
import Modal from '@/shared/components/Modal'

interface AiModelFormProps {
  kind: ModelKind
  /** The saved model being edited, or null to add one. */
  saved: SavedAiModel | null
  /** The starting values when adding: the model in use now, or the service a copy is made of. */
  initial: SaveAiModelRequest
  /** A title for the form when it is not the plain add or edit. */
  title?: string
  /**
   * Given for the model IN USE: the form then has no name or notes, and its button hands the values over to be tested and used
   * (nothing is saved to the list).
   */
  onUse?: (values: SaveAiModelRequest) => void
  onSaved: () => void
  onCancel: () => void
}

/** Saves a model (an address, a model, the name of its key variable and, for an embedding model, its prefixes) to switch to later. */
export default function AiModelForm({ kind, saved, initial, title, onUse, onSaved, onCancel }: AiModelFormProps) {
  const [values, setValues] = useState<SaveAiModelRequest>(saved ?? initial)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const isEmbedding = kind === 'embedding'

  const change = (changes: Partial<SaveAiModelRequest>) => setValues({ ...values, ...changes })

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    if (onUse) {
      onUse(values)
      return
    }

    setError('')
    setSubmitting(true)
    try {
      if (saved) await updateSavedModel(kind, saved.id, values)
      else await addSavedModel(kind, values)
      onSaved()
    } catch (err) {
      setError(errorMessage(err, 'Could not save the model. Please try again.'))
      setSubmitting(false)
    }
  }

  return (
    <Modal
      title={title ?? (saved ? `Edit ${saved.name}` : `Add ${isEmbedding ? 'an embedding' : 'a chat'} model`)}
      onClose={onCancel}
      maxWidth="max-w-xl"
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        {!onUse && (
          <TextField
            label="Name"
            required
          maxLength={100}
            placeholder={isEmbedding ? 'Gemini embeddings (free)' : 'Groq GPT OSS (free)'}
            value={values.name}
            onChange={(e) => change({ name: e.target.value })}
          />
        )}
        <TextField
          label={isEmbedding ? 'Embedding API base URL' : 'Chat API base URL'}
          required
          maxLength={200}
          placeholder="https://api.groq.com/openai/v1"
          value={values.baseUrl}
          onChange={(e) => change({ baseUrl: e.target.value })}
        />
        <TextField
          label="Model"
          required
          maxLength={200}
          placeholder={isEmbedding ? 'gemini-embedding-001' : 'openai/gpt-oss-20b'}
          value={values.model}
          onChange={(e) => change({ model: e.target.value })}
        />
        <TextField
          label="API key name"
          maxLength={100}
          placeholder="GROQ_API_KEY"
          value={values.apiKeyName}
          onChange={(e) => change({ apiKeyName: e.target.value })}
        />
        <p className="-mt-3 text-sm text-slate-500">
          The name of the variable in the server's <code>.env</code> file that holds this service's key (it must end in{' '}
          <code>API_KEY</code>). Leave it empty for Ollama. Only the name is saved here, never the key.
        </p>
        {isEmbedding && (
          <div className="grid gap-5 sm:grid-cols-2">
            <TextField
              label="Query prefix"
              maxLength={100}
              placeholder="search_query:"
              value={values.queryPrefix}
              onChange={(e) => change({ queryPrefix: e.target.value })}
            />
            <TextField
              label="Document prefix"
              maxLength={100}
              placeholder="search_document:"
              value={values.documentPrefix}
              onChange={(e) => change({ documentPrefix: e.target.value })}
            />
          </div>
        )}
        {!onUse && (
          <TextAreaField
            label="Notes"
            rows={3}
            maxLength={500}
            value={values.notes}
            onChange={(e) => change({ notes: e.target.value })}
          />
        )}
        <p className="text-sm text-slate-500">An address with no path (http://localhost:11434) is Ollama.</p>

        {error && <Alert message={error} />}

        <div className="flex justify-end gap-3 pt-1">
          <Button variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {onUse ? (isEmbedding ? 'Convert and use' : 'Test and use') : submitting ? 'Saving...' : 'Save'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
