import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'

const publicLinks = [{ to: '/', label: 'Home' }]
const privateLinks = [{ to: '/tickets', label: 'Tickets' }]

export default function Navbar() {
  const { isAuthenticated, logout } = useAuth()
  const links = isAuthenticated ? [...publicLinks, ...privateLinks] : publicLinks

  return (
    <header className="border-b border-gray-200">
      <nav className="mx-auto flex h-14 max-w-5xl items-center justify-between px-6">
        <div className="flex items-center gap-6">
          <Link to="/" className="text-lg font-bold">
            Ticketing AI
          </Link>
          {links.map(({ to, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                isActive ? 'font-medium text-blue-600' : 'text-gray-600 hover:text-gray-900'
              }
            >
              {label}
            </NavLink>
          ))}
        </div>
        {isAuthenticated ? (
          <button
            onClick={logout}
            className="rounded border border-gray-300 px-3 py-1.5 text-sm font-medium hover:bg-gray-50"
          >
            Logout
          </button>
        ) : (
          <Link
            to="/login"
            className="rounded bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700"
          >
            Login
          </Link>
        )}
      </nav>
    </header>
  )
}
