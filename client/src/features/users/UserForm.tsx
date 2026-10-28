import { useState, type SubmitEvent } from 'react'
import { ApiError } from '@/shared/api/apiClient'
import { createUser, updateUser, type Department, type User } from '@/features/users/usersApi'

interface UserFormProps {
  user: User | null
  departments: Department[]
  onSaved: () => void
  onCancel: () => void
}

const inputClass = 'mt-1 w-full rounded border border-gray-300 px-3 py-2 font-normal disabled:bg-gray-100'

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
    <div className="fixed inset-0 z-10 flex items-center justify-center bg-black/40 px-6">
      <form onSubmit={handleSubmit} className="w-full max-w-md space-y-4 rounded-lg bg-white p-6">
        <h2 className="text-xl font-bold">{isEdit ? 'Edit user' : 'Add user'}</h2>

        <label className="block text-sm font-medium">
          Username
          <input
            required
            disabled={isEdit}
            maxLength={50}
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="block text-sm font-medium">
          Full name
          <input
            required
            maxLength={100}
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="block text-sm font-medium">
          Email
          <input
            required
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="block text-sm font-medium">
          {isEdit ? 'New password (leave blank to keep)' : 'Password'}
          <input
            required={!isEdit}
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className={inputClass}
          />
        </label>

        <label className="block text-sm font-medium">
          Department
          <select
            value={departmentId}
            onChange={(e) => setDepartmentId(Number(e.target.value))}
            className={inputClass}
          >
            {departments.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </label>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={onCancel}
            className="rounded border border-gray-300 px-4 py-2 text-sm font-medium hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {submitting ? 'Saving...' : 'Save'}
          </button>
        </div>
      </form>
    </div>
  )
}
