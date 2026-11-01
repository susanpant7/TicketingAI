import { useState, type SubmitEvent } from 'react'
import { createSetting, updateSetting, type SavedSetting, type Setting } from '@/features/settings/settingsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { TextAreaField, TextField } from '@/shared/components/Field'
import Modal from '@/shared/components/Modal'

interface SettingFormProps {
  /** The setting to edit, or null to add a new one. */
  setting: Setting | null
  onSaved: (saved: SavedSetting) => void
  onCancel: () => void
}

export default function SettingForm({ setting, onSaved, onCancel }: SettingFormProps) {
  const isEdit = setting !== null
  const [key, setKey] = useState(setting?.key ?? '')
  const [label, setLabel] = useState(setting?.label ?? '')
  const [description, setDescription] = useState(setting?.description ?? '')
  const [value, setValue] = useState(setting?.value ?? '')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const multiline = isEdit && (setting.value.length > 80 || setting.value.includes('\n'))

  async function save() {
    setError('')
    setSubmitting(true)
    try {
      const saved = setting
        ? await updateSetting(setting.key, { label, description, value })
        : { setting: await createSetting({ key, label, description, value }) }
      onSaved(saved)
    } catch (err) {
      setError(errorMessage(err, 'Saving the setting failed. Please try again.'))
    } finally {
      setSubmitting(false)
    }
  }

  function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()

    void save()
  }

  return (
    <Modal title={isEdit ? 'Edit setting' : 'Add setting'} onClose={onCancel} maxWidth="max-w-xl">
      <form onSubmit={handleSubmit} className="space-y-5">
        {!isEdit && (
          <TextField
            label="Key"
            required
            maxLength={100}
            placeholder="group.name, for example chat.new_option"
            value={key}
            onChange={(e) => setKey(e.target.value)}
          />
        )}
        <TextField label="Label" required maxLength={100} value={label} onChange={(e) => setLabel(e.target.value)} />
        <TextAreaField
          label="Description"
          required
          rows={3}
          maxLength={1000}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        {multiline ? (
          <TextAreaField label="Value" rows={10} value={value} onChange={(e) => setValue(e.target.value)} />
        ) : (
          <TextField label="Value" value={value} onChange={(e) => setValue(e.target.value)} />
        )}

        {!isEdit && (
          <p className="text-sm text-slate-500">
            The app only uses the settings it already knows. A new setting has no effect until code reads it.
          </p>
        )}

        {error && <Alert message={error} />}

        <div className="flex justify-end gap-3 pt-1">
          <Button variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Saving...' : 'Save'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
