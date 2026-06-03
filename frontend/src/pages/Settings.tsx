import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Users, CheckCircle, Loader2, RefreshCw, Copy, KeyRound, Eye, EyeOff, Lock } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { getUsers, getFamilyCode, regenerateFamilyCode, changePassword } from '../api/auth'

function copyToClipboard(text: string, label = 'Copiado!') {
  navigator.clipboard.writeText(text).then(() => toast.success(label))
}

interface PasswordForm {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

function ChangePasswordSection() {
  const [showCurrent, setShowCurrent] = useState(false)
  const [showNew, setShowNew] = useState(false)
  const { register, handleSubmit, reset, watch, formState: { isSubmitting, errors } } = useForm<PasswordForm>()

  async function onSubmit(data: PasswordForm) {
    try {
      await changePassword(data.currentPassword, data.newPassword)
      toast.success('Senha alterada com sucesso!')
      reset()
    } catch {
      toast.error('Senha atual incorreta.')
    }
  }

  return (
    <div className="card space-y-4">
      <div className="flex items-center gap-2">
        <Lock size={18} className="text-amber-400" />
        <h2 className="text-base font-semibold text-slate-200">Trocar senha</h2>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Senha atual</label>
          <div className="relative">
            <input
              {...register('currentPassword', { required: 'Obrigatório' })}
              type={showCurrent ? 'text' : 'password'}
              className="input pr-10"
              placeholder="••••••••"
              autoComplete="current-password"
            />
            <button type="button" onClick={() => setShowCurrent(v => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200">
              {showCurrent ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
          {errors.currentPassword && <p className="text-red-400 text-xs mt-1">{errors.currentPassword.message}</p>}
        </div>

        <div>
          <label className="label">Nova senha</label>
          <div className="relative">
            <input
              {...register('newPassword', { required: 'Obrigatório', minLength: { value: 6, message: 'Mínimo 6 caracteres' } })}
              type={showNew ? 'text' : 'password'}
              className="input pr-10"
              placeholder="••••••••"
              autoComplete="new-password"
            />
            <button type="button" onClick={() => setShowNew(v => !v)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200">
              {showNew ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
          {errors.newPassword && <p className="text-red-400 text-xs mt-1">{errors.newPassword.message}</p>}
        </div>

        <div>
          <label className="label">Confirmar nova senha</label>
          <input
            {...register('confirmPassword', {
              required: 'Obrigatório',
              validate: v => v === watch('newPassword') || 'As senhas não coincidem'
            })}
            type={showNew ? 'text' : 'password'}
            className="input"
            placeholder="••••••••"
            autoComplete="new-password"
          />
          {errors.confirmPassword && <p className="text-red-400 text-xs mt-1">{errors.confirmPassword.message}</p>}
        </div>

        <div className="flex justify-end">
          <button type="submit" disabled={isSubmitting} className="btn-primary">
            {isSubmitting ? <Loader2 size={15} className="animate-spin" /> : 'Salvar nova senha'}
          </button>
        </div>
      </form>
    </div>
  )
}

export default function Settings() {
  const qc = useQueryClient()
  const [newAccess, setNewAccess] = useState<{ inviteCode: string; pin: string } | null>(null)
  const [showPin, setShowPin] = useState(false)

  const { data: users = [], isLoading: loadingUsers } = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
  })

  const { data: familyCode, isLoading: loadingCode } = useQuery({
    queryKey: ['family-code'],
    queryFn: getFamilyCode,
  })

  const regenerateMutation = useMutation({
    mutationFn: regenerateFamilyCode,
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['family-code'] })
      setNewAccess(data)
      setShowPin(true)
      toast.success('Novo acesso gerado! Anote o PIN — ele não será mostrado novamente.')
    },
    onError: () => toast.error('Erro ao gerar acesso'),
  })

  return (
    <div className="space-y-6 max-w-2xl">
      <div>
        <h1 className="text-2xl font-bold text-slate-100">Configurações</h1>
        <p className="text-slate-400 text-sm mt-0.5">Membros e acesso à família</p>
      </div>

      {/* Family Access */}
      <div className="card space-y-4">
        <div className="flex items-center gap-2">
          <KeyRound size={18} className="text-accent" />
          <h2 className="text-base font-semibold text-slate-200">Código de acesso</h2>
        </div>
        <p className="text-sm text-slate-400">
          Compartilhe o código e o PIN com quem quiser adicionar à família.
          O PIN é secreto — gere um novo acesso se precisar revogar.
        </p>

        {loadingCode ? (
          <div className="flex items-center justify-center py-4">
            <Loader2 size={20} className="animate-spin text-accent" />
          </div>
        ) : newAccess ? (
          <div className="space-y-3">
            <div className="bg-bg-tertiary/60 rounded-xl p-4 space-y-3">
              <p className="text-xs text-amber-400 font-medium">Anote o PIN agora — não será exibido novamente.</p>
              <div className="flex items-center gap-3">
                <div className="flex-1">
                  <p className="text-xs text-slate-500 mb-1">Código</p>
                  <p className="text-2xl font-bold tracking-[0.3em] text-slate-100">{newAccess.inviteCode}</p>
                </div>
                <div className="flex-1">
                  <p className="text-xs text-slate-500 mb-1">PIN</p>
                  <div className="flex items-center gap-2">
                    <p className="text-2xl font-bold tracking-[0.2em] text-accent">
                      {showPin ? newAccess.pin : '••••'}
                    </p>
                    <button onClick={() => setShowPin(v => !v)} className="text-slate-400 hover:text-slate-200">
                      {showPin ? <EyeOff size={14} /> : <Eye size={14} />}
                    </button>
                  </div>
                </div>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => copyToClipboard(newAccess.inviteCode, 'Código copiado!')}
                  className="btn-ghost text-xs flex-1 justify-center"
                >
                  <Copy size={13} /> Copiar código
                </button>
                <button
                  onClick={() => copyToClipboard(newAccess.pin, 'PIN copiado!')}
                  className="btn-ghost text-xs flex-1 justify-center"
                >
                  <Copy size={13} /> Copiar PIN
                </button>
              </div>
            </div>
          </div>
        ) : familyCode?.hasCode ? (
          <div className="bg-bg-tertiary/60 rounded-xl p-4 space-y-3">
            <div>
              <p className="text-xs text-slate-500 mb-1">Código atual</p>
              <div className="flex items-center gap-3">
                <p className="text-2xl font-bold tracking-[0.3em] text-slate-100">{familyCode.inviteCode}</p>
                <button
                  onClick={() => copyToClipboard(familyCode.inviteCode, 'Código copiado!')}
                  className="p-1.5 text-slate-400 hover:text-slate-100 transition-colors"
                >
                  <Copy size={14} />
                </button>
              </div>
            </div>
            <p className="text-xs text-slate-500">PIN não exibido por segurança. Gere um novo acesso para obter um novo PIN.</p>
          </div>
        ) : (
          <p className="text-slate-500 text-sm py-2">Nenhum código configurado. Gere um para permitir novos cadastros.</p>
        )}

        <button
          onClick={() => regenerateMutation.mutate()}
          disabled={regenerateMutation.isPending}
          className="btn-primary"
        >
          {regenerateMutation.isPending
            ? <Loader2 size={15} className="animate-spin" />
            : <RefreshCw size={15} />}
          {familyCode?.hasCode ? 'Gerar novo acesso' : 'Gerar acesso'}
        </button>
      </div>

      <ChangePasswordSection />

      {/* Members */}
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
