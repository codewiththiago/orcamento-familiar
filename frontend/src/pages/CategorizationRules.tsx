import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, Tag } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { getCategorizationRules, createCategorizationRule, updateCategorizationRule, deleteCategorizationRule, type RulePayload } from '../api/categorizationRules'
import { getCategories } from '../api/categories'
import { getAccounts } from '../api/accounts'
import { Modal } from '../components/modals/ExtraIncomeModal'
import type { CategorizationRule, RuleMatchType, Category, FinancialAccount } from '../types'

const MATCH_LABELS: Record<RuleMatchType, string> = {
  0: 'Exata',
  1: 'Contém',
  2: 'Começa com',
  3: 'Regex',
}

interface RuleForm {
  pattern: string
  matchType: RuleMatchType
  categoryId: number
  financialAccountId: number
  priority: number
  active: boolean
}

function RuleModal({ item, categories, accounts, onClose, onSave }: {
  item?: CategorizationRule
  categories: Category[]
  accounts: FinancialAccount[]
  onClose: () => void
  onSave: (data: RulePayload) => Promise<void>
}) {
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<RuleForm>({
    defaultValues: {
      pattern: item?.pattern ?? '',
      matchType: item?.matchType ?? 1,
      categoryId: item?.categoryId ?? categories[0]?.id ?? 0,
      financialAccountId: item?.financialAccountId ?? 0,
      priority: item?.priority ?? 100,
      active: item?.active ?? true,
    }
  })

  async function onSubmit(data: RuleForm) {
    try {
      await onSave({
        pattern: data.pattern,
        matchType: Number(data.matchType) as RuleMatchType,
        categoryId: Number(data.categoryId),
        financialAccountId: Number(data.financialAccountId) || undefined,
        priority: Number(data.priority),
        active: data.active,
      })
      toast.success(item ? 'Regra atualizada!' : 'Regra criada!')
    } catch {
      toast.error('Erro ao salvar regra')
    }
  }

  return (
    <Modal title={item ? 'Editar Regra' : 'Nova Regra'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Padrão (texto no extrato)</label>
          <input {...register('pattern', { required: true })} className="input" placeholder="Ex: POSTO, ANTHROPIC, OPENROUTER" />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Correspondência</label>
            <select {...register('matchType')} className="input">
              {(Object.keys(MATCH_LABELS) as unknown as RuleMatchType[]).map(m => (
                <option key={m} value={m}>{MATCH_LABELS[m]}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="label">Prioridade</label>
            <input {...register('priority')} type="number" className="input" />
          </div>
        </div>
        <div>
          <label className="label">Categoria</label>
          <select {...register('categoryId')} className="input">
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div>
          <label className="label">Conta específica (opcional)</label>
          <select {...register('financialAccountId')} className="input">
            <option value={0}>Todas as contas (global)</option>
            {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
          </select>
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-300">
          <input {...register('active')} type="checkbox" className="accent-accent" />
          Regra ativa
        </label>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-ghost">Cancelar</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">{isSubmitting ? '...' : 'Salvar'}</button>
        </div>
      </form>
    </Modal>
  )
}

export default function CategorizationRules() {
  const qc = useQueryClient()
  const [modal, setModal] = useState<{ open: boolean; item?: CategorizationRule }>({ open: false })

  const { data: rules = [], isLoading } = useQuery({
    queryKey: ['categorization-rules'],
    queryFn: getCategorizationRules,
  })
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories })
  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts })

  const deleteMutation = useMutation({
    mutationFn: deleteCategorizationRule,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['categorization-rules'] }); toast.success('Regra removida') },
    onError: () => toast.error('Erro ao remover'),
  })

  async function handleSave(data: RulePayload) {
    if (modal.item) {
      await updateCategorizationRule(modal.item.id, data)
    } else {
      await createCategorizationRule(data)
    }
    qc.invalidateQueries({ queryKey: ['categorization-rules'] })
    setModal({ open: false })
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-100">Regras de Categorização</h1>
          <p className="text-slate-400 text-sm mt-0.5">Categorize automaticamente gastos recorrentes</p>
        </div>
        <button onClick={() => setModal({ open: true })} className="btn-primary">
          <Plus size={16} /> Nova Regra
        </button>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center h-40">
          <div className="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
        </div>
      ) : rules.length === 0 ? (
        <div className="card text-center py-10">
          <Tag size={28} className="mx-auto text-slate-600 mb-2" />
          <p className="text-slate-400 text-sm">Nenhuma regra ainda. Ex.: <span className="text-slate-300">POSTO → Combustível</span>, <span className="text-slate-300">OPENROUTER → IA / Desenvolvimento</span></p>
        </div>
      ) : (
        <div className="card overflow-x-auto">
          <table className="w-full text-sm min-w-[720px]">
            <thead>
              <tr className="border-b border-slate-700/50">
                {['Padrão', 'Correspondência', 'Categoria', 'Conta', 'Prioridade', 'Ativa', ''].map(h => (
                  <th key={h} className="table-header text-left py-2 px-2 first:pl-0">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rules.map(rule => (
                <tr key={rule.id} className={`border-b border-slate-700/20 ${!rule.active ? 'opacity-50' : ''}`}>
                  <td className="py-2 px-2 pl-0 text-slate-200 font-mono text-xs">{rule.pattern}</td>
                  <td className="py-2 px-2 text-slate-400 text-xs">{MATCH_LABELS[rule.matchType]}</td>
                  <td className="py-2 px-2"><span className="px-1.5 py-0.5 rounded bg-bg-tertiary text-slate-400">{rule.categoryName}</span></td>
                  <td className="py-2 px-2 text-xs text-slate-400">{rule.financialAccountName || 'Global'}</td>
                  <td className="py-2 px-2 text-xs text-slate-400">{rule.priority}</td>
                  <td className="py-2 px-2 text-xs">{rule.active ? <span className="text-green-400">Sim</span> : <span className="text-slate-500">Não</span>}</td>
                  <td className="py-2 px-2">
                    <div className="flex gap-1 justify-end">
                      <button onClick={() => setModal({ open: true, item: rule })} className="p-1 text-slate-400 hover:text-slate-100"><Pencil size={13} /></button>
                      <button onClick={() => { if (window.confirm('Remover regra?')) deleteMutation.mutate(rule.id) }} className="p-1 text-slate-400 hover:text-red-400"><Trash2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {modal.open && (
        <RuleModal
          item={modal.item}
          categories={categories}
          accounts={accounts}
          onClose={() => setModal({ open: false })}
          onSave={handleSave}
        />
      )}
    </div>
  )
}