import { useState, type SubmitEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  function handleSubmit(e: SubmitEvent<HTMLFormElement>) {
    e.preventDefault()
    if (login(username, password)) {
      navigate('/tickets', { replace: true })
    } else {
      setError('Invalid username or password')
    }
  }

  return (
    <main className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-6">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm space-y-4 rounded-lg border border-gray-200 p-6"
      >
        <h1 className="text-2xl font-bold">Login</h1>

        <label className="block text-sm font-medium">
          Username
          <input
            type="text"
            required
            autoComplete="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className="mt-1 w-full rounded border border-gray-300 px-3 py-2 font-normal"
          />
        </label>

        <label className="block text-sm font-medium">
          Password
          <input
            type="password"
            required
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="mt-1 w-full rounded border border-gray-300 px-3 py-2 font-normal"
          />
        </label>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <button
          type="submit"
          className="w-full rounded bg-blue-600 px-4 py-2 font-medium text-white hover:bg-blue-700"
        >
          Login
        </button>
      </form>
    </main>
  )
}
