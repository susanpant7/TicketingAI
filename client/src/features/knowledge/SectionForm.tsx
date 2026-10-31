import { useState, type SubmitEvent } from 'react'
import type { KnowledgeSectionInput } from '@/features/knowledge/knowledgeApi'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { TextAreaField, TextField } from '@/shared/components/Field'
import Modal from '@/shared/components/Modal'

interface SectionFormProps {
  documentTitle: string
  /** The section being edited, or null to add one. */
  section: KnowledgeSectionInput | null
  /** Saves the section into its document; rejects with a message to show. */
  onSave: (section: KnowledgeSectionInput) => Promise<void>
  onCancel: () => void
}

/** Adds one section to a knowledge document, or edits one. */
export default function SectionForm({ documentTitle, section, onSave, onCancel }: SectionFormProps) {
  const [heading, setHeading] = useState(section?.heading ?? '')
  const [content, setContent] = useState(section?.content ?? '')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      await onSave({ heading: heading.trim(), content: content.trim() })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save the section. Please try again.')
      setSaving(false)
    }
  }

  return (
    <Modal
      title={section ? `Edit section in ${documentTitle}` : `Add a section to ${documentTitle}`}
      onClose={onCancel}
      maxWidth="max-w-2xl"
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        <TextField label="Section name" required maxLength={200} value={heading} onChange={(e) => setHeading(e.target.value)} />
        <TextAreaField label="Details" required rows={8} value={content} onChange={(e) => setContent(e.target.value)} />

        {error && <Alert message={error} />}

        <div className="flex justify-end gap-3 pt-1">
          <Button variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={saving || heading.trim().length === 0 || content.trim().length === 0}>
            {saving ? 'Saving...' : 'Save section'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
