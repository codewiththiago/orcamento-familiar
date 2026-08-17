import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Wallet, Eye, EyeOff, ArrowLeft, KeyRound } from 'lucide-react'
import { useAuth } from '../contexts/AuthContext'
import { resetPassword } from '../api/auth'
import toast from 'react-hot-toast'

interface FormData {
  email: string
  password: string
}

interface ForgotFormData {
  email: string
  inviteCode: string
  pin: string
  newPassword: string
  confirmPassword: string
}

function ForgotPasswordForm({ onBack, onSuccess }: {
  onBack: () => void
  onSuccess: (email: string) => void
}) {
  const [showPassword, setShowPassword] = useState(false)
  const { register, handleSubmit, watch, formState: { isSubmitting, errors } } = useForm<ForgotFormData>()

  async function onSubmit(data: ForgotFormData) {
    try {
      await resetPassword({
        email: data.email,
        inviteCode: data.inviteCode,
        pin: data.pin,
        newPassword: data.newPassword,
      })
      toast.success('Senha redefinida! Faça login com a nova senha.')
      onSuccess(data.email)
    } catch {
      toast.error('Código ou PIN inválidos, ou e-mail não cadastrado.')
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label className="label">Email cadastrado</label>
        <input
          {...register('email', { required: 'Email obrigatório' })}
          type="email"
          className="input"
          placeholder="seu@email.com"
          autoComplete="email"
        />
        {errors.email && <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="label">Código da família</label>
          <input
            {...register('inviteCode', { required: 'Obrigatório' })}
            className="input uppercase tracking-widest"
            placeholder="ABC123"
            maxLength={6}
          />
          {errors.inviteCode && <p className="text-red-400 text-xs mt-1">{errors.inviteCode.message}</p>}
        </div>
        <div>
          <label className="label">PIN</label>
          <input
            {...register('pin', { required: 'Obrigatório', pattern: { value: /^\d{4}$/, message: '4 dígitos' } })}
            type="password"
            inputMode="numeric"
            className="input tracking-widest"
            placeholder="••••"
            maxLength={4}
          />
          {errors.pin && <p className="text-red-400 text-xs mt-1">{errors.pin.message}</p>}
        </div>
      </div>

      <div>
        <label className="label">Nova senha</label>
        <div className="relative">
          <input
            {...register('newPassword', { required: 'Obrigatório', minLength: { value: 6, message: 'Mínimo 6 caracteres' } })}
            type={showPassword ? 'text' : 'password'}
            className="input pr-10"
            placeholder="••••••••"
            autoComplete="new-password"
          />
          <button
            type="button"
            onClick={() => setShowPassword(v => !v)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200"
          >
            {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
          </button>
        </div>
        {errors.newPassword && <p className="text-red-400 text-xs mt-1">{errors.newPassword.message}</p>}
      </div>

      <div>
        <label className="label">Confirmar nova senha</label>
        <input
          {...register('confirmPassword', {
            required: 'Obrigatório',
            validate: v => v === watch('newPassword') || 'As senhas não coincidem',
          })}
          type={showPassword ? 'text' : 'password'}
          className="input"
          placeholder="••••••••"
          autoComplete="new-password"
        />
        {errors.confirmPassword && <p className="text-red-400 text-xs mt-1">{errors.confirmPassword.message}</p>}
      </div>

      <button type="submit" disabled={isSubmitting} className="btn-primary w-full justify-center py-2.5">
        {isSubmitting ? (
          <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
        ) : 'Redefinir senha'}
      </button>

      <button
        type="button"
        onClick={onBack}
        className="w-full flex items-center justify-center gap-1 text-xs text-slate-400 hover:text-slate-200 transition-colors"
      >
        <ArrowLeft size={13} /> Voltar para o login
      </button>
    </form>
  )
}

function LoginForm({ initialEmail, onForgot }: {
  initialEmail: string
  onForgot: () => void
}) {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [showPassword, setShowPassword] = useState(false)
  const { register, handleSubmit, formState: { isSubmitting, errors } } = useForm<FormData>({
    defaultValues: { email: initialEmail },
  })

  async function onSubmit(data: FormData) {
    try {
      await login(data.email, data.password)
      navigate('/dashboard')
    } catch {
      toast.error('Email ou senha inválidos')
    }
  }

  return (
    <>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">Email</label>
          <input
            {...register('email', { required: 'Email obrigatório' })}
            type="email"
            className="input"
            placeholder="seu@email.com"
            autoComplete="email"
          />
          {errors.email && <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>}
        </div>

        <div>
          <label className="label">Senha</label>
          <div className="relative">
            <input
              {...register('password', { required: 'Senha obrigatória' })}
              type={showPassword ? 'text' : 'password'}
              className="input pr-10"
              placeholder="••••••••"
              autoComplete="current-password"
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200"
            >
              {showPassword ? <EyeOff size={16} /> : <Eye size={16} />}
            </button>
          </div>
          {errors.password && <p className="text-red-400 text-xs mt-1">{errors.password.message}</p>}
        </div>

        <button type="submit" disabled={isSubmitting} className="btn-primary w-full justify-center py-2.5">
          {isSubmitting ? (
            <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
          ) : 'Entrar'}
        </button>
      </form>

      <div className="mt-4 pt-4 border-t border-slate-700/50 space-y-2 text-center">
        <button
          onClick={onForgot}
          className="flex items-center justify-center gap-1 mx-auto text-xs text-slate-400 hover:text-slate-200 transition-colors"
        >
          <KeyRound size={13} /> Esqueci minha senha
        </button>
        <button
          onClick={() => navigate('/register')}
          className="text-xs text-slate-400 hover:text-slate-200 transition-colors"
        >
          Primeiro acesso? Criar conta →
        </button>
      </div>
    </>
  )
}

export default function Login() {
  const [mode, setMode] = useState<'login' | 'forgot'>('login')
  const [prefilledEmail, setPrefilledEmail] = useState('')

  function handleForgotSuccess(email: string) {
    setPrefilledEmail(email)
    setMode('login')
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-bg-primary p-4">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <div className="w-14 h-14 bg-accent rounded-2xl flex items-center justify-center mx-auto mb-4">
            <Wallet size={28} className="text-white" />
          </div>
          <h1 className="text-2xl font-bold text-slate-100">Orçamento Familiar</h1>
          <p className="text-slate-400 text-sm mt-1">
            {mode === 'login' ? 'Faça login para continuar' : 'Recuperar senha'}
          </p>
        </div>

        <div className="card">
          {mode === 'login' ? (
            <LoginForm key={prefilledEmail} initialEmail={prefilledEmail} onForgot={() => setMode('forgot')} />
          ) : (
            <ForgotPasswordForm onBack={() => setMode('login')} onSuccess={handleForgotSuccess} />
          )}
        </div>

        <p className="text-center text-xs text-slate-600 mt-6">by Querência Labs</p>
      </div>
    </div>
  )
}