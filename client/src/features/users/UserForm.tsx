import { useState, type SubmitEvent } from 'react'
import { createUser, updateUser, type Department, type User } from '@/features/users/usersApi'
import { ApiError } from '@/shared/api/apiClient'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { SelectField, TextField } from '@/shared/components/Field'
import Modal from '@/shared/components/Modal'

interface UserFormProps {
  user: User | null
  departments: Department[]
  onSaved: () => void
  onCancel: () => void
}

export default function UserForm({ user, departments, onSaved, onCancel }: UserFormProps) {
  const isEdit = user !== null
  const [username, setUsername] = useState(user?.username ?? '')
  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [email, setEmail] = useState(user?.email ?? '')
  const [password, setPassword] = useState('')
  const [departmentId, setDepartmentId] = useState(user?.departmentId ?? departments[0]?.id ?? 0)
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      if (user) {
        await updateUser(user.id, { fullName, email, departmentId, password: password || undefined })
      } else {
        await createUser({ username, fullName, email, password, departmentId })
      }
      onSaved()
    } catch (err) {
      setError(err instanceof ApiError && err.status !== 500 ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal title={isEdit ? 'Edit user' : 'Add user'} onClose={onCancel}>
      <form onSubmit={handleSubmit} className="space-y-5">
        <div className="grid gap-5 sm:grid-cols-2">
          <TextField
            label="Username"
            required
            disabled={isEdit}
            maxLength={50}
            value={username}
            onChange={(e) => setUsername(e.target.value)}
          />
          <TextField
            label="Full name"
            required
            maxLength={100}
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
          />
        </div>

        <TextField label="Email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />

        <div className="grid gap-5 sm:grid-cols-2">
          <TextField
            label={isEdit ? 'New password' : 'Password'}
            type="password"
            required={!isEdit}
            autoComplete="new-password"
            placeholder={isEdit ? 'Leave blank to keep' : undefined}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <SelectField label="Department" value={departmentId} onChange={(e) => setDepartmentId(Number(e.target.value))}>
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </SelectField>
        </div>

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
