import { useState, type SubmitEvent } from 'react'
import { loadDefaultUsers, type LoadDefaultsResult } from '@/features/users/usersApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { TextField } from '@/shared/components/Field'
import Modal from '@/shared/components/Modal'

interface LoadDefaultsFormProps {
  departmentCount: number
  onLoaded: (result: LoadDefaultsResult) => void
  onCancel: () => void
}

export default function LoadDefaultsForm({ departmentCount, onLoaded, onCancel }: LoadDefaultsFormProps) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      onLoaded(await loadDefaultUsers(password))
    } catch (err) {
      setError(errorMessage(err, 'Could not load the default users. Please try again.'))
      setSubmitting(false)
    }
  }

  return (
    <Modal title="Load default users" onClose={onCancel}>
      <form onSubmit={handleSubmit} className="space-y-5">
        <p className="text-sm leading-6 text-slate-600">
          This adds 15 default users and divides them equally over the {departmentCount}{' '}
          {departmentCount === 1 ? 'department' : 'departments'} you have. A user that already exists is skipped. Enter the password
          that all of these users will log in with.
        </p>

        <TextField
          label="Password for all users"
          type="password"
          required
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />

        {error && <Alert message={error} />}

        <div className="flex justify-end gap-3 pt-1">
          <Button variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={submitting || password.length === 0}>
            {submitting ? 'Loading...' : 'Load users'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
