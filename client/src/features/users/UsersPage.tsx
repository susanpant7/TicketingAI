import { useEffect, useState } from 'react'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import ConfirmDialog from '@/shared/components/ConfirmDialog'
import PageHeader from '@/shared/components/PageHeader'
import UserForm from '@/features/users/UserForm'
import { deleteUser, getDepartments, getUsers, type Department, type User } from '@/features/users/usersApi'

const departmentStyles: Record<string, string> = {
  IT: 'bg-indigo-50 text-indigo-700',
  HR: 'bg-emerald-50 text-emerald-700',
  Finance: 'bg-amber-50 text-amber-700',
}
const defaultDepartmentStyle = 'bg-slate-100 text-slate-700'

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [departments, setDepartments] = useState<Department[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<User | null>(null)
  const [deleting, setDeleting] = useState<User | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)

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

  async function confirmDelete() {
    if (!deleting) return
    setDeleteBusy(true)
    try {
      await deleteUser(deleting.id)
      reload()
    } catch {
      setError('Failed to delete user.')
    } finally {
      setDeleteBusy(false)
      setDeleting(null)
    }
  }

  function handleSaved() {
    setFormOpen(false)
    reload()
  }

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader
        title="Users"
        description="Add, edit and remove users, and manage their departments."
        action={
          <Button onClick={() => openForm(null)} disabled={departments.length === 0}>
            Add user
          </Button>
        }
      />

      {error && (
        <div className="mt-6">
          <Alert message={error} />
        </div>
      )}

      <div className="mt-8 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        {loading ? (
          <p className="px-6 py-12 text-center text-sm text-slate-500">Loading users...</p>
        ) : users.length === 0 ? (
          <p className="px-6 py-12 text-center text-sm text-slate-500">No users yet.</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-6 py-3 font-medium">User</th>
                <th className="hidden px-6 py-3 font-medium md:table-cell">Username</th>
                <th className="px-6 py-3 font-medium">Department</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {users.map((user) => (
                <tr key={user.id} className="hover:bg-slate-50/60">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-indigo-100 text-sm font-semibold text-indigo-700">
                        {user.fullName[0]}
                      </span>
                      <div className="min-w-0">
                        <p className="font-medium text-slate-900">{user.fullName}</p>
                        <p className="truncate text-slate-500">{user.email}</p>
                      </div>
                    </div>
                  </td>
                  <td className="hidden px-6 py-4 text-slate-600 md:table-cell">{user.username}</td>
                  <td className="px-6 py-4">
                    <span
                      className={`rounded-full px-2.5 py-1 text-xs font-medium ${departmentStyles[user.departmentName] ?? defaultDepartmentStyle}`}
                    >
                      {user.departmentName}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" size="sm" onClick={() => openForm(user)}>
                        Edit
                      </Button>
                      <Button variant="dangerGhost" size="sm" onClick={() => setDeleting(user)}>
                        Delete
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {formOpen && (
        <UserForm
          key={editing?.id ?? 'new'}
          user={editing}
          departments={departments}
          onSaved={handleSaved}
          onCancel={() => setFormOpen(false)}
        />
      )}

      {deleting && (
        <ConfirmDialog
          title="Delete user"
          message={`Are you sure you want to delete ${deleting.fullName} (${deleting.username})? This cannot be undone.`}
          confirmLabel="Delete"
          busy={deleteBusy}
          onConfirm={confirmDelete}
          onCancel={() => setDeleting(null)}
        />
      )}
    </main>
  )
}
