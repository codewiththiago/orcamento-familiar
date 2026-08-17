import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, ArrowLeft, ArrowRight } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { format, startOfMonth, endOfMonth } from 'date-fns'
import { getTransactions, createTransaction, updateTransaction, deleteTransaction } from '../api/transactions'
import { getAccounts } from '../api/accounts'
import { getCategories } from '../api/categories'
import { fmt } from '../utils/format'
import { Modal } from '../components/modals/ExtraIncomeModal'
import type { Transaction, TransactionType, Category, FinancialAccount } from '../types'

const MONTH_ABBR = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']

interface TransactionForm {
  description: string
  financialAccountId: number
  categoryId: number
  type: TransactionType
  amount: number
  transactionDate: string
  totalInstallments: number
  observation: string
}

function TransactionModal({ item, accounts, categories, onClose, onSave }: {
  item?: Transaction
  accounts: FinancialAccount[]
  categories: Category[]
  onClose: () => void
  onSave: (data: Omit<TransactionForm, 'type' | 'categoryId'> & { type: TransactionType; categoryId?: number }) => Promise<void>
}) {
  const isEdit = !!item
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<TransactionForm>({
    defaultValues: {
      description: item?.description ?? '',
      financialAccountId: item?.financialAccountId ?? accounts[0]?.id ?? 0,
      categoryId: item?.categoryId ?? 0,
      type: item?.type ?? 1,
      amount: item?.amount ?? 0,
      transactionDate: item ? item.transactionDate.substring(0, 10) : format(new Date(), 'yyyy-MM-dd'),
      totalInstallments: item?.totalInstallments ?? 1,
      observation: item?.observation ?? '',
    }
  })

  async function onSubmit(data: TransactionForm) {
    try {
      await onSave({
        ...data,
        type: Number(data.type) as TransactionType,
        financialAccountId: Number(data.financialAccountId),
        categoryId: Number(data.categoryId) || undefined,
        totalInstallments: Number(data.totalInstallments),
        amount: Number(data.amount),
      })
      toast.success(isEdit ? 'Atualizado!' : 'Adicionado!')
    } catch {
      toast.error('Erro ao salvar')
    }
  }

  return (
    <Modal title={isEdit ? 'Editar Transação' : 'Nova Transação'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Descrição</label>
          <input {...register('description', { required: true })} className="input" placeholder="Ex: Supermercado" />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Tipo</label>
            <select {...register('type')} className="input">
              <option value={1}>Despesa</option>
              <option value={0}>Receita</option>
              <option value={2}>Transferência</option>
            </select>
          </div>
          <div>
            <label className="label">Conta</label>
            <select {...register('financialAccountId')} className="input">
              {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
            </select>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Data</label>
            <input {...register('transactionDate')} type="date" className="input" />
          </div>
          <div>
            <label className="label">Valor (R$)</label>
            <input {...register('amount', { min: 0.01 })} type="number" step="0.01" className="input" />
          </div>
        </div>
        <div>
          <label className="label">Categoria</label>
          <select {...register('categoryId')} className="input">
            <option value={0}>— Sem categoria —</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Nº Parcelas</label>
            <input {...register('totalInstallments', { min: 1 })} type="number" min={1} disabled={isEdit} className="input" />
          </div>
          <div>
            <label className="label">Observação</label>
            <input {...register('observation')} className="input" placeholder="—" />
          </div>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-ghost">Cancelar</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">{isSubmitting ? '...' : 'Salvar'}</button>
        </div>
      </form>
    </Modal>
  )
}

export default function Transactions() {
  const qc = useQueryClient()
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const [accountFilter, setAccountFilter] = useState<number | ''>('')
  const [categoryFilter, setCategoryFilter] = useState<number | ''>('')
  const [typeFilter, setTypeFilter] = useState<TransactionType | ''>('')
  const [modal, setModal] = useState<{ open: boolean; item?: Transaction }>({ open: false })

  const from = format(startOfMonth(new Date(year, month - 1, 1)), 'yyyy-MM-dd')
  const to = format(endOfMonth(new Date(year, month - 1, 1)), 'yyyy-MM-dd')

  const { data: transactions = [], isLoading } = useQuery({
    queryKey: ['transactions', from, to, accountFilter, categoryFilter, typeFilter],
    queryFn: () => getTransactions({
      from,
      to,
      accountId: accountFilter === '' ? undefined : accountFilter,
      categoryId: categoryFilter === '' ? undefined : categoryFilter,
      type: typeFilter === '' ? undefined : typeFilter,
      limit: 500,
    }),
  })

  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts })
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories })

  const totals = useMemo(() => ({
    income: transactions.filter(t => t.type === 0).reduce((s, t) => s + t.amount, 0),
    expense: transactions.filter(t => t.type === 1).reduce((s, t) => s + t.amount, 0),
  }), [transactions])

  const deleteMutation = useMutation({
    mutationFn: ({ id, deleteFuture }: { id: number; deleteFuture: boolean }) => deleteTransaction(id, deleteFuture),
    onSuccess: () => { invalidate(); toast.success('Removido') },
    onError: () => toast.error('Erro ao remover'),
  })

  function invalidate() {
    qc.invalidateQueries({ queryKey: ['transactions'] })
    qc.invalidateQueries({ queryKey: ['accounts'] })
    qc.invalidateQueries({ queryKey: ['insights'] })
  }

  async function handleSave(data: Omit<TransactionForm, 'type' | 'categoryId'> & { type: TransactionType; categoryId?: number }) {
    if (modal.item) {
      const { type, description, categoryId, amount, transactionDate, observation } = data
      await updateTransaction(modal.item.id, { type, description, categoryId, amount, transactionDate, observation })
    } else {
      await createTransaction(data)
    }
    invalidate()
    setModal({ open: false })
  }

  function handleDelete(t: Transaction) {
    if (t.totalInstallments > 1) {
      if (window.confirm(`Esta compra tem ${t.totalInstallments} parcelas. Deseja remover esta e as próximas?`)) {
        deleteMutation.mutate({ id: t.id, deleteFuture: true })
      } else if (window.confirm('Remover apenas esta parcela?')) {
        deleteMutation.mutate({ id: t.id, deleteFuture: false })
      }
    } else {
      if (window.confirm('Confirmar exclusão?')) deleteMutation.mutate({ id: t.id, deleteFuture: false })
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-100">Transações</h1>
          <p className="text-slate-400 text-sm mt-0.5">Movimentações por mês</p>
        </div>
        <button onClick={() => setModal({ open: true })} className="btn-primary">
          <Plus size={16} /> Nova Transação
        </button>
      </div>

      {/* Controls */}
      <div className="card flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-1">
          <button onClick={() => { if (month === 1) { setMonth(12); setYear(y => y - 1) } else setMonth(m => m - 1) }} className="btn-ghost p-2"><ArrowLeft size={15} /></button>
          <span className="font-semibold text-slate-100 min-w-[6.5rem] text-center">{MONTH_ABBR[month - 1]} {year}</span>
          <button onClick={() => { if (month === 12) { setMonth(1); setYear(y => y + 1) } else setMonth(m => m + 1) }} className="btn-ghost p-2"><ArrowRight size={15} /></button>
        </div>
        <select className="input text-xs w-auto" value={accountFilter} onChange={e => setAccountFilter(e.target.value === '' ? '' : Number(e.target.value))}>
          <option value="">Todas as contas</option>
          {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
        </select>
        <select className="input text-xs w-auto" value={categoryFilter} onChange={e => setCategoryFilter(e.target.value === '' ? '' : Number(e.target.value))}>
          <option value="">Todas as categorias</option>
          {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        <select className="input text-xs w-auto" value={typeFilter} onChange={e => setTypeFilter(e.target.value === '' ? '' : Number(e.target.value) as TransactionType)}>
          <option value="">Todos os tipos</option>
          <option value={1}>Despesas</option>
          <option value={0}>Receitas</option>
          <option value={2}>Transferências</option>
        </select>
        <div className="ml-auto flex items-center gap-4 text-sm">
          <span className="text-green-400">Receitas: {fmt(totals.income)}</span>
          <span className="text-red-400">Despesas: {fmt(totals.expense)}</span>
        </div>
      </div>

      {/* Table */}
      <div className="card overflow-x-auto">
        {isLoading ? (
          <div className="flex items-center justify-center h-40">
            <div className="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
          </div>
        ) : transactions.length === 0 ? (
          <p className="text-slate-500 text-sm py-8 text-center">Nenhuma transação neste mês</p>
        ) : (
          <table className="w-full text-sm min-w-[760px]">
            <thead>
              <tr className="border-b border-slate-700/50">
                {['Data', 'Descrição', 'Conta', 'Categoria', 'Tipo', 'Parcela', 'Valor', ''].map(h => (
                  <th key={h} className="table-header text-left py-2 px-2 first:pl-0">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {transactions.map(t => (
                <tr key={t.id} className="border-b border-slate-700/20 hover:bg-bg-tertiary/30 transition-colors">
                  <td className="py-2 px-2 pl-0 text-slate-400 text-xs">{new Date(t.transactionDate).toLocaleDateString('pt-BR', { timeZone: 'UTC' })}</td>
                  <td className="py-2 px-2 text-slate-200">{t.description}</td>
                  <td className="py-2 px-2 text-slate-400 text-xs">{t.financialAccountName}</td>
                  <td className="py-2 px-2 text-xs">
                    {t.categoryName
                      ? <span className="px-1.5 py-0.5 rounded bg-bg-tertiary text-slate-400">{t.categoryName}</span>
                      : <span className="text-amber-400/80">— revisar —</span>}
                  </td>
                  <td className="py-2 px-2 text-xs">
                    <span className={`px-1.5 py-0.5 rounded ${t.type === 0 ? 'bg-green-500/10 text-green-400' : t.type === 2 ? 'bg-blue-500/10 text-blue-400' : 'bg-red-500/10 text-red-400'}`}>
                      {t.type === 0 ? 'Receita' : t.type === 2 ? 'Transferência' : 'Despesa'}
                    </span>
                  </td>
                  <td className="py-2 px-2 text-xs text-slate-500">
                    {t.totalInstallments > 1 ? `${t.currentInstallment}/${t.totalInstallments}` : '—'}
                  </td>
                  <td className={`py-2 px-2 font-medium ${t.type === 0 ? 'text-green-400' : t.type === 2 ? 'text-blue-400' : 'text-slate-200'}`}>
                    {t.type === 0 ? '+' : t.type === 2 ? '↔' : '−'} {fmt(t.amount)}
                  </td>
                  <td className="py-2 px-2">
                    <div className="flex gap-1 justify-end">
                      <button onClick={() => setModal({ open: true, item: t })} className="p-1 text-slate-400 hover:text-slate-100"><Pencil size={13} /></button>
                      <button onClick={() => handleDelete(t)} className="p-1 text-slate-400 hover:text-red-400"><Trash2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modal.open && (
        <TransactionModal
          item={modal.item}
          accounts={accounts}
          categories={categories}
          onClose={() => setModal({ open: false })}
          onSave={handleSave}
        />
      )}
    </div>
  )
}