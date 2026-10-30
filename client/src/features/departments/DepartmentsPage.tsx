import { useEffect, useState } from 'react'
import DepartmentForm from '@/features/departments/DepartmentForm'
import { deleteDepartment, getDepartments, loadDefaultDepartments, type Department } from '@/features/departments/departmentsApi'
import { errorMessage } from '@/shared/api/errorMessage'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import ConfirmDialog from '@/shared/components/ConfirmDialog'
import PageHeader from '@/shared/components/PageHeader'

export default function DepartmentsPage() {
  const [departments, setDepartments] = useState<Department[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<Department | null>(null)
  const [deleting, setDeleting] = useState<Department | null>(null)
  const [deleteBusy, setDeleteBusy] = useState(false)
  const [notice, setNotice] = useState('')
  const [loadingDefaults, setLoadingDefaults] = useState(false)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    getDepartments()
      .then((loaded) => {
        if (ignore) return
        setDepartments(loaded)
        setError('')
      })
      .catch((err) => !ignore && setError(errorMessage(err, 'Could not load the departments. Make sure the API is running, then try again.')))
      .finally(() => !ignore && setLoading(false))
    return () => {
      ignore = true
    }
  }, [version])

  const reload = () => setVersion((v) => v + 1)

  function openForm(department: Department | null) {
    setEditing(department)
    setFormOpen(true)
  }

  function handleSaved() {
    setFormOpen(false)
    reload()
  }

  async function handleLoadDefaults() {
    setError('')
    setNotice('')
    setLoadingDefaults(true)
    try {
      const { added, skipped } = await loadDefaultDepartments()
      setNotice(
        added === 0
          ? 'The default departments already exist.'
          : `Added ${added} default ${added === 1 ? 'department' : 'departments'}${skipped > 0 ? ` (${skipped} already existed)` : ''}.`,
      )
      reload()
    } catch (err) {
      setError(errorMessage(err, 'Could not load the default departments. Please try again.'))
    } finally {
      setLoadingDefaults(false)
    }
  }

  async function confirmDelete() {
    if (!deleting) return
    setDeleteBusy(true)
    try {
      await deleteDepartment(deleting.id)
      setError('')
      reload()
    } catch (err) {
      // The server refuses when users have appeared in the department since the list was loaded.
      setError(errorMessage(err, 'Failed to delete the department.'))
      reload()
    } finally {
      setDeleteBusy(false)
      setDeleting(null)
    }
  }

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader
        title="Departments"
        description="Add, rename and delete departments. A department that still has users cannot be deleted."
        action={
          <div className="flex gap-3">
            <Button variant="secondary" onClick={handleLoadDefaults} disabled={loadingDefaults}>
              {loadingDefaults ? 'Loading...' : 'Load default departments'}
            </Button>
            <Button onClick={() => openForm(null)}>Add department</Button>
          </div>
        }
      />

      {(notice || error) && (
        <div className="mt-6 space-y-3">
          {notice && <Alert variant="success" message={notice} />}
          {error && <Alert message={error} />}
        </div>
      )}

      <div className="mt-8 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        {loading ? (
          <p className="px-6 py-12 text-center text-sm text-slate-500">Loading departments...</p>
        ) : departments.length === 0 ? (
          <p className="px-6 py-12 text-center text-sm text-slate-500">No departments yet.</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-6 py-3 font-medium">Department</th>
                <th className="hidden px-6 py-3 font-medium md:table-cell">Contact</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {departments.map((department) => (
                <tr key={department.id} className="hover:bg-slate-50/60">
                  <td className="px-6 py-4">
                    <p className="font-medium text-slate-900">{department.name}</p>
                    {department.description && <p className="mt-0.5 text-slate-500">{department.description}</p>}
                  </td>
                  <td className="hidden px-6 py-4 text-slate-600 md:table-cell">{department.contact}</td>
                  <td className="px-6 py-4">
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" size="sm" onClick={() => openForm(department)}>
                        Edit
                      </Button>
                      <Button
                        variant="dangerGhost"
                        size="sm"
                        disabled={department.userCount > 0}
                        title={department.userCount > 0 ? 'Move or delete its users first' : undefined}
                        onClick={() => setDeleting(department)}
                      >
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
        <DepartmentForm
          key={editing?.id ?? 'new'}
          department={editing}
          onSaved={handleSaved}
          onCancel={() => setFormOpen(false)}
        />
      )}

      {deleting && (
        <ConfirmDialog
          title="Delete department"
          message={`Are you sure you want to delete the department "${deleting.name}"? This cannot be undone.`}
          confirmLabel="Delete"
          busy={deleteBusy}
          onConfirm={confirmDelete}
          onCancel={() => setDeleting(null)}
        />
      )}
    </main>
  )
}
