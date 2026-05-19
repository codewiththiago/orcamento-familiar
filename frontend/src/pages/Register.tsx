import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Wallet, Loader2, Check, X } from 'lucide-react'
import toast from 'react-hot-toast'
import { validateInvite, register as apiRegister } from '../api/auth'
import { useAuth } from '../contexts/AuthContext'

interface FormValues {
  name: string
  email: string
  password: string
  confirmPassword: string
}

function PasswordRequirement({ met, label }: { met: boolean; label: string }) {
  return (
    <div className={`flex items-center gap-1.5 text-xs ${met ? 'text-green-400' : 'text-slate-500'}`}>
      {met ? <Check size={11} /> : <X size={11} />}
      {label}
    </div>
  )
}

export default function Register() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const navigate = useNavigate()
  const { user, loginWithData } = useAuth()

  const [validating, setValidating] = useState(!!token)
  const [inviteEmail, setInviteEmail] = useState('')
  const [invalidToken, setInvalidToken] = useState(false)

  const { register, handleSubmit, watch, setValue, formState: { errors, isSubmitting } } = useForm<FormValues>()
  const password = watch('password', '')

  useEffect(() => {
    if (user) { navigate('/dashboard'); return }
    if (!token) { setValidating(false); return }

    validateInvite(token).then(info => {
      if (!info.isValid) {
        setInvalidToken(true)
      } else {
        setInviteEmail(info.email)
        setValue('email', info.email)
      }
      setValidating(false)
    })
  }, [token, user])

  async function onSubmit(values: FormValues) {
    if (values.password !== values.confirmPassword) {
      toast.error('As senhas não coincidem')
      return
    }
    try {
      const data = await apiRegister({
        token: token || undefined,
        name: values.name,
        email: values.email,
        password: values.password,
      })
      loginWithData(data)
      toast.success(`Bem-vindo, ${data.name}!`)
      navigate('/dashboard')
    } catch {
      toast.error('Erro ao criar conta. Verifique os dados e tente novamente.')
    }
  }

  if (validating) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-bg-primary">
        <Loader2 size={28} className="animate-spin text-accent" />
      </div>
    )
  }

  if (invalidToken) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-bg-primary p-4">
        <div className="card max-w-sm w-full text-center space-y-4">
          <p className="text-red-400 font-medium">Convite inválido ou expirado.</p>
          <button onClick={() => navigate('/login')} className="btn-ghost text-sm">Voltar ao login</button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-bg-primary p-4">
      <div className="w-full max-w-sm">
        <div className="flex items-center gap-3 mb-8 justify-center">
          <div className="w-10 h-10 bg-accent rounded-xl flex items-center justify-center">
            <Wallet size={20} className="text-white" />
          </div>
          <div>
            <h1 className="font-bold text-slate-100">Orçamento Familiar</h1>
            <p className="text-xs text-slate-400">Criar conta</p>
          </div>
        </div>

        <div className="card">
          {token ? (
            <p className="text-xs text-accent bg-accent/10 rounded-lg px-3 py-2 mb-4 text-center">
              Você foi convidado para {inviteEmail}
            </p>
          ) : (
            <p className="text-xs text-slate-500 bg-bg-tertiary rounded-lg px-3 py-2 mb-4 text-center">
              Primeiro acesso ao sistema. Preencha seus dados para criar a conta.
            </p>
          )}

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
                readOnly={!!inviteEmail}
                {...register('email', { required: 'E-mail obrigatório' })}
              />
              {errors.email && <p className="text-red-400 text-xs mt-1">{errors.email.message}</p>}
            </div>

            <div>
              <label className="label">Senha</label>
              <input
                className="input"
                type="password"
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
      </div>
    </div>
  )
}
