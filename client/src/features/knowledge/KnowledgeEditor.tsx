import { useState, type SubmitEvent } from 'react'
import type { Department } from '@/features/departments/departmentsApi'
import { saveKnowledgeDocument, type KnowledgeSectionInput, type UploadFileResult } from '@/features/knowledge/knowledgeApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { SelectField, TextAreaField, TextField } from '@/shared/components/Field'
import Modal from '@/shared/components/Modal'

interface KnowledgeEditorProps {
  /** The document being edited, or null for a new one. */
  sourceFile: string | null
  initialTitle: string
  initialDepartmentId: number | null
  initialSections: KnowledgeSectionInput[]
  departments: Department[]
  onSaved: (result: UploadFileResult) => void
  /** Shown for an existing document. */
  onDelete?: () => void
  onCancel: () => void
}

const empty: KnowledgeSectionInput = { heading: '', content: '' }

/** Writes a knowledge document as a title, a department and any number of sections. Each section is searched on its own. */
export default function KnowledgeEditor({
  sourceFile,
  initialTitle,
  initialDepartmentId,
  initialSections,
  departments,
  onSaved,
  onDelete,
  onCancel,
}: KnowledgeEditorProps) {
  const [title, setTitle] = useState(initialTitle)
  const [departmentId, setDepartmentId] = useState(initialDepartmentId ? String(initialDepartmentId) : '')
  const [sections, setSections] = useState<KnowledgeSectionInput[]>(initialSections.length > 0 ? initialSections : [empty])
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const change = (index: number, changes: Partial<KnowledgeSectionInput>) =>
    setSections(sections.map((section, i) => (i === index ? { ...section, ...changes } : section)))

  const complete =
    title.trim().length > 0 &&
    departmentId !== '' &&
    sections.every((s) => s.heading.trim().length > 0 && s.content.trim().length > 0)

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      const result = await saveKnowledgeDocument({
        sourceFile,
        title: title.trim(),
        departmentId: Number(departmentId),
        sections,
      })
      if (result.error) {
        setError(result.error)
        setSaving(false)
        return
      }
      onSaved(result)
    } catch (err) {
      setError(errorMessage(err, 'Could not save the knowledge. Please try again.'))
      setSaving(false)
    }
  }

  return (
    <Modal title={sourceFile ? `Edit ${initialTitle}` : 'Write knowledge'} onClose={onCancel} maxWidth="max-w-3xl">
      <form onSubmit={handleSubmit} className="space-y-5">
        <div className="grid gap-5 sm:grid-cols-[1fr_14rem]">
          <TextField
            label="Title"
            required
            maxLength={200}
            placeholder="Laptop and hardware"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
          <SelectField label="Department" required value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}>
            <option value="">Select a department</option>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </SelectField>
        </div>

        <div className="space-y-4">
          {sections.map((section, index) => (
            <fieldset key={index} className="space-y-3 rounded-xl bg-slate-50 p-4 ring-1 ring-inset ring-slate-200">
              <div className="flex items-end gap-3">
                <div className="min-w-0 flex-1">
                  <TextField
                    label={`Section ${index + 1} name`}
                    required
                    maxLength={200}
                    placeholder="Requesting a new laptop"
                    value={section.heading}
                    onChange={(e) => change(index, { heading: e.target.value })}
                  />
                </div>
                {sections.length > 1 && (
                  <Button
                    variant="dangerGhost"
                    size="sm"
                    aria-label={`Remove section ${index + 1}`}
                    onClick={() => setSections(sections.filter((_, i) => i !== index))}
                  >
                    Remove
                  </Button>
                )}
              </div>
              <TextAreaField
                label="Details"
                required
                rows={5}
                placeholder="Employees request a new laptop from IT at it-help@ticket.com. New laptops are delivered within 5 working days."
                value={section.content}
                onChange={(e) => change(index, { content: e.target.value })}
              />
            </fieldset>
          ))}
          <Button variant="secondary" size="sm" onClick={() => setSections([...sections, empty])}>
            Add section
          </Button>
        </div>

        <p className="text-xs text-slate-500">
          The assistant finds each section on its own, so keep one topic per section and name what it is about in the details.
        </p>

        {error && <Alert message={error} />}

        <div className="flex flex-wrap justify-between gap-3 pt-1">
          <div>
            {onDelete && (
              <Button variant="dangerGhost" onClick={onDelete}>
                Delete document
              </Button>
            )}
          </div>
          <div className="flex gap-3">
            <Button variant="secondary" onClick={onCancel}>
              Cancel
            </Button>
            <Button type="submit" disabled={saving || !complete}>
              {saving ? 'Saving...' : 'Save'}
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  )
}
