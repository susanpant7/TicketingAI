import { useEffect, useState } from 'react'
import UserForm from '@/features/users/UserForm'
import { deleteUser, getDepartments, getUsers, type Department, type User } from '@/features/users/usersApi'

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [departments, setDepartments] = useState<Department[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<User | null>(null)

  const [version, setVersion] = useState(0)
  const reload = () => setVersion((v) => v + 1)

  useEffect(() => {
    let ignore = false
    Promise.all([getUsers(), getDepartments()])
      .then(([loadedUsers, loadedDepartments]) => {
        if (ignore) return
        setUsers(loadedUsers)
        setDepartments(loadedDepartments)
        setError('')
      })
      .catch(() => !ignore && setError('Failed to load users.'))
      .finally(() => !ignore && setLoading(false))
    return () => {
      ignore = true
    }
  }, [version])

  function openForm(user: User | null) {
    setEditing(user)
    setFormOpen(true)
  }

  async function handleDelete(user: User) {
    if (!window.confirm(`Delete ${user.fullName}?`)) return
    try {
      await deleteUser(user.id)
      reload()
    } catch {
      setError('Failed to delete user.')
    }
  }

  function handleSaved() {
    setFormOpen(false)
    reload()
  }

  return (
    <main className="mx-auto max-w-5xl px-6 py-10">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Users</h1>
        <button
          onClick={() => openForm(null)}
          disabled={departments.length === 0}
          className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
        >
          Add user
        </button>
      </div>

      {error && <p className="mt-4 text-sm text-red-600">{error}</p>}

      {loading ? (
        <p className="mt-6 text-gray-600">Loading...</p>
      ) : (
        <table className="mt-6 w-full text-left text-sm">
          <thead className="border-b border-gray-200 text-gray-600">
            <tr>
              <th className="py-2 pr-4 font-medium">Name</th>
              <th className="py-2 pr-4 font-medium">Username</th>
              <th className="py-2 pr-4 font-medium">Email</th>
              <th className="py-2 pr-4 font-medium">Department</th>
              <th className="py-2" />
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className="border-b border-gray-100">
                <td className="py-2 pr-4">{user.fullName}</td>
                <td className="py-2 pr-4">{user.username}</td>
                <td className="py-2 pr-4">{user.email}</td>
                <td className="py-2 pr-4">{user.departmentName}</td>
                <td className="space-x-3 py-2 text-right">
                  <button onClick={() => openForm(user)} className="text-blue-600 hover:underline">
                    Edit
                  </button>
                  <button onClick={() => handleDelete(user)} className="text-red-600 hover:underline">
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {formOpen && (
        <UserForm
          key={editing?.id ?? 'new'}
          user={editing}
          departments={departments}
          onSaved={handleSaved}
          onCancel={() => setFormOpen(false)}
        />
      )}
    </main>
  )
}
