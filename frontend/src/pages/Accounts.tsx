import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, Landmark, Wallet, CreditCard, Banknote, Smartphone, Building2 } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { getAccounts, createAccount, updateAccount, deleteAccount, type AccountPayload } from '../api/accounts'
import { fmt } from '../utils/format'
import { Modal } from '../components/modals/ExtraIncomeModal'
import type { FinancialAccount, FinancialAccountType } from '../types'

const TYPE_LABELS: Record<FinancialAccountType, string> = {
  0: 'Conta Corrente',
  1: 'Carteira Digital',
  2: 'Cartão de Crédito',
  3: 'Cartão Pré-pago',
  4: 'Dinheiro',
  5: 'Outra',
}

const TYPE_ICONS: Record<FinancialAccountType, React.ElementType> = {
  0: Landmark,
  1: Smartphone,
  2: CreditCard,
  3: Banknote,
  4: Banknote,
  5: Building2,
}

interface AccountForm {
  name: string
  institution: string
  type: FinancialAccountType
  initialBalance: number
}

function AccountModal({ item, onClose, onSave }: {
  item?: FinancialAccount
  onClose: () => void
  onSave: (data: AccountPayload) => Promise<void>
}) {
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<AccountForm>({
    defaultValues: {
      name: item?.name ?? '',
      institution: item?.institution ?? '',
      type: item?.type ?? 0,
      initialBalance: item?.initialBalance ?? 0,
    }
  })

  async function onSubmit(data: AccountForm) {
    try {
      await onSave({
        name: data.name,
        institution: data.institution || undefined,
        type: Number(data.type) as FinancialAccountType,
        initialBalance: Number(data.initialBalance),
      })
      toast.success(item ? 'Conta atualizada!' : 'Conta criada!')
    } catch {
      toast.error('Erro ao salvar conta')
    }
  }

  return (
    <Modal title={item ? 'Editar Conta' : 'Nova Conta'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Nome</label>
          <input {...register('name', { required: true })} className="input" placeholder="Ex: Conta C6" />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Instituição</label>
            <input {...register('institution')} className="input" placeholder="Ex: C6 Bank" />
          </div>
          <div>
            <label className="label">Tipo</label>
            <select {...register('type')} className="input">
              {(Object.keys(TYPE_LABELS) as unknown as FinancialAccountType[]).map(t => (
                <option key={t} value={t}>{TYPE_LABELS[t]}</option>
              ))}
            </select>
          </div>
        </div>
        <div>
          <label className="label">Saldo inicial (R$)</label>
          <input {...register('initialBalance')} type="number" step="0.01" className="input" placeholder="0,00" />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-ghost">Cancelar</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">{isSubmitting ? '...' : 'Salvar'}</button>
        </div>
      </form>
    </Modal>
  )
}

export default function Accounts() {
  const qc = useQueryClient()
  const [modal, setModal] = useState<{ open: boolean; item?: FinancialAccount }>({ open: false })

  const { data: accounts = [], isLoading } = useQuery({
    queryKey: ['accounts'],
    queryFn: getAccounts,
  })

  const deleteMutation = useMutation({
    mutationFn: deleteAccount,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['accounts'] }); toast.success('Conta removida') },
    onError: () => toast.error('Não foi possível remover (pode ter transações associadas)'),
  })

  async function handleSave(data: AccountPayload) {
    if (modal.item) {
      await updateAccount(modal.item.id, data)
    } else {
      await createAccount(data)
    }
    qc.invalidateQueries({ queryKey: ['accounts'] })
    setModal({ open: false })
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-100">Contas</h1>
          <p className="text-slate-400 text-sm mt-0.5">Contas financeiras para importar e acompanhar</p>
        </div>
        <button onClick={() => setModal({ open: true })} className="btn-primary">
          <Plus size={16} /> Nova Conta
        </button>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center h-40">
          <div className="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
        </div>
      ) : accounts.length === 0 ? (
        <div className="card text-center py-10">
          <p className="text-slate-400 text-sm">Nenhuma conta cadastrada. Adicione C6, PicPay, Nubank ou outra.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {accounts.map(account => {
            const Icon = TYPE_ICONS[account.type]
            return (
              <div key={account.id} className={`card transition-colors ${!account.active ? 'opacity-60' : 'hover:border-slate-600'}`}>
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2">
                    <div className="w-9 h-9 rounded-lg bg-accent/15 flex items-center justify-center">
                      <Icon size={17} className="text-accent" />
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-100">{account.name}</h3>
                      <span className="text-xs text-slate-500">
                        {account.institution || TYPE_LABELS[account.type]}
                      </span>
                    </div>
                  </div>
                  <div className="flex gap-1">
                    <button onClick={() => setModal({ open: true, item: account })} className="p-1.5 text-slate-400 hover:text-slate-100 rounded">
                      <Pencil size={14} />
                    </button>
                    <button onClick={() => { if (window.confirm(`Remover a conta "${account.name}"?`)) deleteMutation.mutate(account.id) }} className="p-1.5 text-slate-400 hover:text-red-400 rounded">
                      <Trash2 size={14} />
                    </button>
                  </div>
                </div>
                <div className="space-y-1.5 text-sm">
                  <div className="flex justify-between">
                    <span className="text-slate-400">Saldo atual</span>
                    <span className={`font-semibold ${account.balance >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                      {fmt(account.balance)}
                    </span>
                  </div>
                  <div className="flex justify-between text-xs">
                    <span className="text-slate-500">Saldo inicial</span>
                    <span className="text-slate-400">{fmt(account.initialBalance)}</span>
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {modal.open && (
        <AccountModal
          item={modal.item}
          onClose={() => setModal({ open: false })}
          onSave={handleSave}
        />
      )}
    </div>
  )
}