import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { Wallet, Loader2 } from 'lucide-react'
import toast from 'react-hot-toast'
import { validateInvite, register as apiRegister } from '../api/auth'
import { setAccessToken } from '../api/client'
import { useAuth } from '../contexts/AuthContext'

interface FormValues {
  name: string
  email: string
  password: string
  confirmPassword: string
}

export default function Register() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const navigate = useNavigate()
  const { user } = useAuth()

  const [validating, setValidating] = useState(!!token)
  const [inviteEmail, setInviteEmail] = useState('')
  const [invalidToken, setInvalidToken] = useState(false)

  const { register, handleSubmit, watch, setValue, formState: { errors, isSubmitting } } = useForm<FormValues>()

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
      setAccessToken(data.accessToken)
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
                placeholder="Mínimo 6 caracteres"
                {...register('password', { required: 'Senha obrigatória', minLength: { value: 6, message: 'Mínimo 6 caracteres' } })}
              />
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
