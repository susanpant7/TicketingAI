import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import Button from '@/shared/components/Button'
import { buttonClass } from '@/shared/components/buttonStyles'
import { SparkleIcon } from '@/shared/components/icons'

const publicLinks = [{ to: '/', label: 'Home' }]
const privateLinks = [{ to: '/tickets', label: 'Tickets' }]
const adminLinks = [{ to: '/users', label: 'Users' }]

export default function Navbar() {
  const { user, isAuthenticated, logout } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const links = [
    ...publicLinks,
    ...(isAuthenticated && !isAdmin ? privateLinks : []),
    ...(isAdmin ? adminLinks : []),
  ]

  return (
    <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/80 backdrop-blur">
      <nav className="mx-auto flex h-16 max-w-6xl items-center justify-between px-6">
        <div className="flex items-center gap-8">
          <Link to="/" className="flex items-center gap-2.5">
            <span className="flex size-8 items-center justify-center rounded-lg bg-indigo-600 text-white">
              <SparkleIcon className="size-5" />
            </span>
            <span className="text-lg font-semibold tracking-tight text-slate-900">Ticketing AI</span>
          </Link>
          <div className="flex items-center gap-1">
            {links.map(({ to, label }) => (
              <NavLink
                key={to}
                to={to}
                className={({ isActive }) =>
                  `rounded-lg px-3 py-2 text-sm font-medium transition ${
                    isActive
                      ? 'bg-indigo-50 text-indigo-700'
                      : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                  }`
                }
              >
                {label}
              </NavLink>
            ))}
          </div>
        </div>

        {isAuthenticated && user ? (
          <div className="flex items-center gap-3">
            <span className="flex size-8 items-center justify-center rounded-full bg-indigo-100 text-sm font-semibold text-indigo-700">
              {user.username[0].toUpperCase()}
            </span>
            <div className="hidden leading-tight sm:block">
              <p className="text-sm font-medium text-slate-900">{user.username}</p>
              <p className="text-xs text-slate-500">{user.role}</p>
            </div>
            <Button variant="secondary" size="sm" onClick={logout}>
              Logout
            </Button>
          </div>
        ) : (
          <Link to="/login" className={buttonClass('primary', 'sm')}>
            Login
          </Link>
        )}
      </nav>
    </header>
  )
}
