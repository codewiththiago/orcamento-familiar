import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Copy, Trash2, UserPlus, Users, Mail, CheckCircle, Loader2 } from 'lucide-react'
import toast from 'react-hot-toast'
import { getUsers, getInvites, createInvite, deleteInvite } from '../api/auth'

function copyToClipboard(text: string) {
  navigator.clipboard.writeText(text).then(() => toast.success('Link copiado!'))
}

function inviteLink(token: string) {
  return `${window.location.origin}/register?token=${token}`
}

function timeLeft(expiresAt: string) {
  const diff = new Date(expiresAt).getTime() - Date.now()
  if (diff <= 0) return 'Expirado'
  const h = Math.floor(diff / 3600000)
  const d = Math.floor(h / 24)
  return d > 0 ? `${d}d restantes` : `${h}h restantes`
}

export default function Settings() {
  const qc = useQueryClient()
  const [email, setEmail] = useState('')

  const { data: users = [], isLoading: loadingUsers } = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
  })

  const { data: invites = [], isLoading: loadingInvites } = useQuery({
    queryKey: ['invites'],
    queryFn: getInvites,
  })

  const inviteMutation = useMutation({
    mutationFn: () => createInvite(email.trim()),
    onSuccess: (invite) => {
      qc.invalidateQueries({ queryKey: ['invites'] })
      setEmail('')
      const link = inviteLink(invite.token)
      copyToClipboard(link)
      toast.success('Convite criado! Link copiado para a área de transferência.')
    },
    onError: () => toast.error('Erro ao criar convite'),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteInvite,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['invites'] })
      toast.success('Convite removido')
    },
    onError: () => toast.error('Erro ao remover convite'),
  })

  function handleInvite(e: React.FormEvent) {
    e.preventDefault()
    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      toast.error('Informe um e-mail válido')
      return
    }
    inviteMutation.mutate()
  }

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <h1 className="text-2xl font-bold text-slate-100">Configurações</h1>
        <p className="text-slate-400 text-sm mt-0.5">Membros da família e convites</p>
      </div>

      {/* Invite form */}
      <div className="card space-y-4">
        <div className="flex items-center gap-2">
          <UserPlus size={18} className="text-accent" />
          <h2 className="text-base font-semibold text-slate-200">Convidar membro</h2>
        </div>
        <p className="text-sm text-slate-400">
          O convite é válido por 7 dias. O link gerado dá acesso à tela de registro.
          Envie o link diretamente para a pessoa convidada.
        </p>
        <form onSubmit={handleInvite} className="flex gap-2">
          <input
            type="email"
            className="input flex-1"
            placeholder="email@exemplo.com"
            value={email}
            onChange={e => setEmail(e.target.value)}
          />
          <button type="submit" disabled={inviteMutation.isPending} className="btn-primary shrink-0">
            {inviteMutation.isPending ? <Loader2 size={15} className="animate-spin" /> : <UserPlus size={15} />}
            Convidar
          </button>
        </form>
      </div>

      {/* Pending invites */}
      <div className="card space-y-4">
        <div className="flex items-center gap-2">
          <Mail size={18} className="text-amber-400" />
          <h2 className="text-base font-semibold text-slate-200">Convites pendentes</h2>
        </div>

        {loadingInvites ? (
          <div className="flex items-center justify-center py-6">
            <Loader2 size={20} className="animate-spin text-accent" />
          </div>
        ) : invites.length === 0 ? (
          <p className="text-slate-500 text-sm py-2">Nenhum convite pendente</p>
        ) : (
          <div className="space-y-2">
            {invites.map(inv => (
              <div key={inv.id} className="flex items-center gap-3 bg-bg-tertiary/40 rounded-lg px-3 py-2.5">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-slate-200 truncate">{inv.email}</p>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Convidado por {inv.createdByName} · {timeLeft(inv.expiresAt)}
                  </p>
                </div>
                <button
                  onClick={() => copyToClipboard(inviteLink(inv.token))}
                  className="p-1.5 text-slate-400 hover:text-slate-100 transition-colors"
                  title="Copiar link"
                >
                  <Copy size={14} />
                </button>
                <button
                  onClick={() => { if (window.confirm('Remover convite?')) deleteMutation.mutate(inv.id) }}
                  className="p-1.5 text-slate-400 hover:text-red-400 transition-colors"
                  title="Remover convite"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Users */}
      <div className="card space-y-4">
        <div className="flex items-center gap-2">
          <Users size={18} className="text-green-400" />
          <h2 className="text-base font-semibold text-slate-200">Membros ativos</h2>
        </div>

        {loadingUsers ? (
          <div className="flex items-center justify-center py-6">
            <Loader2 size={20} className="animate-spin text-accent" />
          </div>
        ) : (
          <div className="space-y-2">
            {users.map(u => (
              <div key={u.id} className="flex items-center gap-3 bg-bg-tertiary/40 rounded-lg px-3 py-2.5">
                <div className="w-8 h-8 rounded-full bg-accent/20 flex items-center justify-center shrink-0">
                  <span className="text-sm font-bold text-accent">{u.name[0].toUpperCase()}</span>
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-slate-200">{u.name}</p>
                  <p className="text-xs text-slate-500 truncate">{u.email}</p>
                </div>
                <CheckCircle size={14} className="text-green-400 shrink-0" />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
