import { useState, type SubmitEvent } from 'react'
import { createDepartment, updateDepartment, type Department } from '@/features/departments/departmentsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { TextAreaField, TextField } from '@/shared/components/Field'
import Modal from '@/shared/components/Modal'

interface DepartmentFormProps {
  /** The department to rename, or null to add a new one. */
  department: Department | null
  onSaved: () => void
  onCancel: () => void
}

export default function DepartmentForm({ department, onSaved, onCancel }: DepartmentFormProps) {
  const [name, setName] = useState(department?.name ?? '')
  const [description, setDescription] = useState(department?.description ?? '')
  const [contact, setContact] = useState(department?.contact ?? '')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      const input = { name, description, contact }
      if (department) await updateDepartment(department.id, input)
      else await createDepartment(input)
      onSaved()
    } catch (err) {
      setError(errorMessage(err, 'Saving the department failed. Please try again.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal title={department ? 'Edit department' : 'Add department'} onClose={onCancel} maxWidth="max-w-md">
      <form onSubmit={handleSubmit} className="space-y-5">
        <TextField label="Name" required maxLength={100} value={name} onChange={(e) => setName(e.target.value)} />
        <TextAreaField
          label="What it handles"
          rows={3}
          maxLength={500}
          placeholder="For example: leave, benefits, policies, onboarding"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <TextField
          label="Contact"
          maxLength={300}
          placeholder="For example: hr@company.com or extension 100"
          value={contact}
          onChange={(e) => setContact(e.target.value)}
        />
        <p className="text-sm text-slate-500">
          The chat assistant uses the name, what it handles and the contact to send people to the right department.
        </p>

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
