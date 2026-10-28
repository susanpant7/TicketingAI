import { useState, type SubmitEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import Alert from '@/shared/components/Alert'
import Button from '@/shared/components/Button'
import { TextField } from '@/shared/components/Field'
import { SparkleIcon } from '@/shared/components/icons'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setSubmitting(true)
    try {
      const user = await login(username, password)
      if (user) {
        navigate(user.role === 'Admin' ? '/users' : '/tickets', { replace: true })
      } else {
        setError('Invalid username or password')
      }
    } catch {
      setError('Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="flex min-h-[calc(100vh-4rem)] items-center justify-center px-6 py-12">
      <div className="w-full max-w-sm">
        <div className="text-center">
          <span className="mx-auto flex size-12 items-center justify-center rounded-xl bg-indigo-600 text-white shadow-sm">
            <SparkleIcon className="size-7" />
          </span>
          <h1 className="mt-6 text-2xl font-bold tracking-tight text-slate-900">Welcome back</h1>
          <p className="mt-2 text-sm text-slate-500">Login to your Ticketing AI account</p>
        </div>

        <form
          onSubmit={handleSubmit}
          className="mt-8 space-y-5 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
        >
          <TextField
            label="Username"
            required
            autoComplete="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
          />
          <TextField
            label="Password"
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />

          {error && <Alert message={error} />}

          <Button type="submit" disabled={submitting} className="w-full">
            {submitting ? 'Logging in...' : 'Login'}
          </Button>
        </form>
      </div>
    </main>
  )
}
