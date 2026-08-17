import { NavLink, useNavigate } from 'react-router-dom'
import { LayoutDashboard, CreditCard, LogOut, Wallet, Settings, ArrowLeftRight, FileUp, Tag, CalendarClock, Smartphone } from 'lucide-react'
import { useAuth } from '../../contexts/AuthContext'
import { useInstallPrompt } from '../../hooks/useInstallPrompt'
import toast from 'react-hot-toast'

const navItems = [
  { to: '/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/accounts', icon: Wallet, label: 'Contas' },
  { to: '/transactions', icon: ArrowLeftRight, label: 'Transações' },
  { to: '/imports', icon: FileUp, label: 'Importar' },
  { to: '/cards', icon: CreditCard, label: 'Cartões' },
  { to: '/rules', icon: Tag, label: 'Regras' },
  { to: '/commitments', icon: CalendarClock, label: 'Compromissos' },
  { to: '/settings', icon: Settings, label: 'Configurações' },
]

export default function Sidebar() {
  const { user, logout } = useAuth()
  const { canInstall, promptInstall } = useInstallPrompt()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/login')
    toast.success('Até logo!')
  }

  return (
    <aside className="w-16 sm:w-56 bg-bg-secondary border-r border-slate-700/50 flex flex-col shrink-0">
      <div className="p-4 border-b border-slate-700/50">
        <div className="flex items-center gap-3">
          <img
            src="/logo-app.png"
            alt="Orçamento Familiar"
            className="w-9 h-9 rounded-lg object-contain shrink-0"
          />
          <span className="hidden sm:block font-semibold text-sm text-slate-100 leading-tight">
            Orçamento<br />Familiar
          </span>
        </div>
      </div>

      <nav className="flex-1 p-2 space-y-1">
        {navItems.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all ${
                isActive
                  ? 'bg-accent/20 text-accent'
                  : 'text-slate-400 hover:text-slate-100 hover:bg-bg-tertiary'
              }`
            }
          >
            <Icon size={18} className="shrink-0" />
            <span className="hidden sm:block">{label}</span>
          </NavLink>
        ))}

        {canInstall && (
          <button
            onClick={promptInstall}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-accent bg-accent/10 hover:bg-accent/20 transition-all"
          >
            <Smartphone size={18} className="shrink-0" />
            <span className="hidden sm:block">Instalar app</span>
          </button>
        )}
      </nav>

      <div className="hidden sm:block px-4 pb-2 text-center">
        <span className="text-xs text-slate-600">by Querência Labs</span>
      </div>

      <div className="p-3 border-t border-slate-700/50">
        <div className="hidden sm:flex items-center gap-2 px-2 mb-2">
          <div className="w-6 h-6 rounded-full bg-accent/30 flex items-center justify-center text-xs font-bold text-accent">
            {user?.name[0]}
          </div>
          <span className="text-xs text-slate-400 truncate">{user?.name}</span>
        </div>
        <button
          onClick={handleLogout}
          className="w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-400 hover:text-red-400 hover:bg-red-500/10 transition-all"
        >
          <LogOut size={16} className="shrink-0" />
          <span className="hidden sm:block">Sair</span>
        </button>
      </div>
    </aside>
  )
}
