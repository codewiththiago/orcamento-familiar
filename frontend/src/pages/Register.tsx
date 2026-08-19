import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Loader2, Check, X, Eye, EyeOff, Home, Users, Copy } from 'lucide-react'
import toast from 'react-hot-toast'
import { register as apiRegister, getRegistrationStatus } from '../api/auth'
import { useAuth } from '../contexts/AuthContext'
import type { User } from '../types'

interface FormValues {
  name: string
  email: string
  password: string
  confirmPassword: string
  familyName: string
  inviteCode: string
  pin: string
}

function PasswordRequirement({ met, label }: { met: boolean; label: string }) {
  return (
    <div className={`flex items-center gap-1.5 text-xs ${met ? 'text-green-400' : 'text-slate-500'}`}>
      {met ? <Check size={11} /> : <X size={11} />}
      {label}
    </div>
  )
}

function NewFamilySuccess({ data, onContinue }: { data: User; onContinue: () => void }) {
  const [showPin, setShowPin] = useState(false)

  function copy(text: string, label: string) {
    navigator.clipboard.writeText(text).then(() => toast.success(label))
  }

  return (
    <div className="space-y-4">
      <div className="text-center">
        <div className="w-12 h-12 bg-green-500/15 rounded-full flex items-center justify-center mx-auto mb-2">
          <Check size={22} className="text-green-400" />
        </div>
        <h2 className="font-bold text-slate-100">Família criada!</h2>
        <p className="text-xs text-slate-400 mt-1">
          Compartilhe o código e o PIN com quem quiser adicionar à família. Anote agora — o PIN não será exibido novamente.
        </p>
      </div>

      <div className="bg-bg-tertiary/60 rounded-xl p-4 space-y-3">
        <div className="flex items-center gap-3">
          <div className="flex-1">
            <p className="text-xs text-slate-500 mb-1">Código</p>
            <p className="text-2xl font-bold tracking-[0.3em] text-slate-100">{data.familyCode}</p>
          </div>
          <div className="flex-1">
            <p className="text-xs text-slate-500 mb-1">PIN</p>
            <div className="flex items-center gap-2">
              <p className="text-2xl font-bold tracking-[0.2em] text-accent">
                {showPin ? data.familyPin : '••••'}
              </p>
              <button onClick={() => setShowPin(v => !v)} className="text-slate-400 hover:text-slate-200">
                {showPin ? <EyeOff size={14} /> : <Eye size={14} />}
              </button>
            </div>
          </div>
        </div>
        <div className="flex gap-2">
          <button onClick={() => copy(data.familyCode ?? '', 'Código copiado!')} className="btn-ghost text-xs flex-1 justify-center">
            <Copy size={13} /> Copiar código
          </button>
          <button onClick={() => copy(data.familyPin ?? '', 'PIN copiado!')} className="btn-ghost text-xs flex-1 justify-center">
            <Copy size={13} /> Copiar PIN
          </button>
        </div>
      </div>

      <button onClick={onContinue} className="btn-primary w-full justify-center py-2.5">
        Começar a usar
      </button>
    </div>
  )
}

export default function Register() {
  const navigate = useNavigate()
  const { user, loginWithData } = useAuth()

  const [loading, setLoading] = useState(true)
  const [hasFamilies, setHasFamilies] = useState(false)
  const [mode, setMode] = useState<'create' | 'join'>('create')
  const [showPassword, setShowPassword] = useState(false)
  const [createdUser, setCreatedUser] = useState<User | null>(null)

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm<FormValues>()
  const password = watch('password', '')

  useEffect(() => {
    if (user) { navigate('/dashboard'); return }

    getRegistrationStatus()
      .then(s => {
        setHasFamilies(s.hasFamilies)
        setMode(s.hasFamilies ? 'create' : 'create')
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [user])

  async function onSubmit(values: FormValues) {
    if (values.password !== values.confirmPassword) {
      toast.error('As senhas não coincidem')
      return
    }
    try {
      const data = await apiRegister({
        name: values.name,
        email: values.email,
        password: values.password,
        familyMode: mode,
        familyName: mode === 'create' ? values.familyName : undefined,
        inviteCode: mode === 'join' ? values.inviteCode.toUpperCase() : undefined,
        pin: mode === 'join' ? values.pin : undefined,
      })
      loginWithData(data)
      if (mode === 'create' && data.familyCode) {
        setCreatedUser(data)
      } else {
        toast.success(`Bem-vindo, ${data.name}!`)
        navigate('/dashboard')
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      toast.error(msg ?? (mode === 'join' ? 'Código ou PIN inválidos.' : 'Erro ao criar conta. Tente novamente.'))
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-bg-primary">
        <Loader2 size={28} className="animate-spin text-accent" />
      </div>
    )
  }

  if (createdUser) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-bg-primary p-4">
        <div className="w-full max-w-sm">
          <div className="text-center mb-6">
            <img src="/logo-app.png" alt="Orçamento Familiar" className="w-14 h-14 rounded-2xl object-contain mx-auto mb-3" />
            <h1 className="text-xl font-bold text-slate-100">Orçamento Familiar</h1>
          </div>
          <div className="card">
            <NewFamilySuccess data={createdUser} onContinue={() => navigate('/dashboard')} />
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-bg-primary p-4">
      <div className="w-full max-w-sm">
        <div className="text-center mb-6">
          <img src="/logo-app.png" alt="Orçamento Familiar" className="w-14 h-14 rounded-2xl object-contain mx-auto mb-3" />
          <h1 className="font-bold text-slate-100">Criar conta</h1>
          <p className="text-xs text-slate-400 mt-1">Comece uma família nova ou entre em uma existente</p>
        </div>

        <div className="card">
          <div className="grid grid-cols-2 gap-2 mb-4">
            <button
              type="button"
              onClick={() => setMode('create')}
              className={`flex items-center justify-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium transition-all ${
                mode === 'create' ? 'bg-accent/20 text-accent' : 'bg-bg-tertiary/40 text-slate-400 hover:text-slate-200'
              }`}
            >
              <Home size={14} /> Criar família
            </button>
            <button
              type="button"
              onClick={() => setMode('join')}
              disabled={!hasFamilies}
              className={`flex items-center justify-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium transition-all disabled:opacity-40 disabled:cursor-not-allowed ${
                mode === 'join' ? 'bg-accent/20 text-accent' : 'bg-bg-tertiary/40 text-slate-400 hover:text-slate-200'
              }`}
            >
              <Users size={14} /> Entrar na família
            </button>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
              <label className="label">Nome</label>
              <input
                className="input"
                placeholder="Seu nome"
                {...register('name', { required: 'Nome obrigatório' })}
              />
              {errors.name && <p className="text-red-400 text-xs mt-1">{errors.name.message}</p>}
            </div>

            <div>
              <label className="label">E-mail</label>
              <input
                className="input"
                type="email"
                placeholder="seu@email.com"
                {...register('email', { required: 'E-mail obrigatório' })}
              />
              {errors.email && <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>}
            </div>

            <div>
              <label className="label">Senha</label>
              <div className="relative">
                <input
                  className="input pr-10"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Ex: minhasenha1"
                  {...register('password', {
                    required: 'Senha obrigatória',
                    validate: (v) => {
                      if (v.length < 6) return 'Mínimo 6 caracteres'
                      if (!/[a-zA-Z]/.test(v)) return 'Precisa ter pelo menos 1 letra'
                      if (!/[0-9]/.test(v)) return 'Precisa ter pelo menos 1 número'
                      return true
                    },
                  })}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(v => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200"
                >
                  {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
              {password && (
                <div className="mt-2 space-y-0.5">
                  <PasswordRequirement met={password.length >= 6} label="Mínimo 6 caracteres" />
                  <PasswordRequirement met={/[a-zA-Z]/.test(password)} label="Pelo menos 1 letra" />
                  <PasswordRequirement met={/[0-9]/.test(password)} label="Pelo menos 1 número" />
                </div>
              )}
              {errors.password && <p className="text-red-400 text-xs mt-1">{errors.password.message}</p>}
            </div>

            <div>
              <label className="label">Confirmar senha</label>
              <input
                className="input"
                type="password"
                placeholder="Repita a senha"
                {...register('confirmPassword', { required: 'Confirmação obrigatória' })}
              />
              {errors.confirmPassword && <p className="text-red-400 text-xs mt-1">{errors.confirmPassword.message}</p>}
            </div>

            {mode === 'create' && (
              <div>
                <label className="label">Nome da família</label>
                <input
                  className="input"
                  placeholder="Ex: Família Silva"
                  {...register('familyName')}
                />
              </div>
            )}

            {mode === 'join' && (
              <div className="space-y-3">
                <p className="text-xs text-slate-400 font-medium">Acesso à família</p>
                <div className="flex gap-2">
                  <div className="flex-1">
                    <label className="label">Código</label>
                    <input
                      className="input uppercase tracking-widest"
                      placeholder="XXXXXX"
                      maxLength={6}
                      {...register('inviteCode', { required: 'Código obrigatório' })}
                    />
                    {errors.inviteCode && <p className="text-red-400 text-xs mt-1">{errors.inviteCode.message}</p>}
                  </div>
                  <div className="w-24">
                    <label className="label">PIN</label>
                    <input
                      className="input tracking-widest"
                      placeholder="0000"
                      maxLength={4}
                      type="password"
                      {...register('pin', { required: 'PIN obrigatório' })}
                    />
                    {errors.pin && <p className="text-red-400 text-xs mt-1">{errors.pin.message}</p>}
                  </div>
                </div>
              </div>
            )}

            <button type="submit" disabled={isSubmitting} className="btn-primary w-full mt-2">
              {isSubmitting ? <Loader2 size={16} className="animate-spin" /> : 'Criar conta'}
            </button>
          </form>

          <div className="mt-4 pt-4 border-t border-slate-700/50 text-center">
            <button onClick={() => navigate('/login')} className="text-xs text-slate-400 hover:text-slate-200 transition-colors">
              Já tenho conta → Entrar
            </button>
          </div>
        </div>

        <p className="text-center text-xs text-slate-600 mt-6">by Querência Labs</p>
      </div>
    </div>
  )
}