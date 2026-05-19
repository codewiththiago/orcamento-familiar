import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Plus, Pencil, Trash2, ChevronDown, ChevronUp, FileUp } from 'lucide-react'
import {
  PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import toast from 'react-hot-toast'
import {
  getMonthlyBudget, updateSalary, getCategorySummary,
  addExtraIncome, updateExtraIncome, deleteExtraIncome,
  addFixedExpense, updateFixedExpense, deleteFixedExpense,
  addCreditCardLaunch, updateCreditCardLaunch, deleteCreditCardLaunch,
} from '../api/budget'
import { getCards } from '../api/cards'
import { getCategories } from '../api/categories'
import { fmt, MONTH_NAMES } from '../utils/format'
import type { ExtraIncome, FixedExpense, CreditCardLaunch, InstallmentType } from '../types'
import ExtraIncomeModal from '../components/modals/ExtraIncomeModal'
import FixedExpenseModal from '../components/modals/FixedExpenseModal'
import CreditCardLaunchModal from '../components/modals/CreditCardLaunchModal'
import PdfImportModal from '../components/modals/PdfImportModal'

const DONUT_COLORS = ['#6366f1', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4', '#ec4899', '#84cc16', '#f97316', '#14b8a6']

export default function MonthlyView() {
  const { year, month } = useParams<{ year: string; month: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()

  const y = Number(year)
  const m = Number(month)

  const { data: budget, isLoading } = useQuery({
    queryKey: ['budget', y, m],
    queryFn: () => getMonthlyBudget(y, m),
  })

  const { data: cards = [] } = useQuery({ queryKey: ['cards'], queryFn: () => getCards() })
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories })
  const { data: summary = [] } = useQuery({
    queryKey: ['category-summary', budget?.id],
    queryFn: () => getCategorySummary(budget!.id),
    enabled: !!budget?.id,
  })

  // modals
  const [extraIncomeModal, setExtraIncomeModal] = useState<{ open: boolean; item?: ExtraIncome }>({ open: false })
  const [fixedExpenseModal, setFixedExpenseModal] = useState<{ open: boolean; item?: FixedExpense }>({ open: false })
  const [launchModal, setLaunchModal] = useState<{ open: boolean; item?: CreditCardLaunch }>({ open: false })
  const [pdfModal, setPdfModal] = useState(false)

  // filters for launches
  const [filterCard, setFilterCard] = useState<number | ''>('')
  const [filterCategory, setFilterCategory] = useState<number | ''>('')

  // salary editing
  const [editingSalary, setEditingSalary] = useState(false)
  const [salThiago, setSalThiago] = useState(0)
  const [salJuh, setSalJuh] = useState(0)

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['budget', y, m] })
    qc.invalidateQueries({ queryKey: ['category-summary', budget?.id] })
    qc.invalidateQueries({ queryKey: ['dashboard'] })
  }

  const salaryMutation = useMutation({
    mutationFn: () => updateSalary(y, m, salThiago, salJuh),
    onSuccess: () => { invalidate(); setEditingSalary(false); toast.success('Salários atualizados') },
    onError: () => toast.error('Erro ao atualizar salários'),
  })

  const delExtraIncome = useMutation({
    mutationFn: deleteExtraIncome,
    onSuccess: () => { invalidate(); toast.success('Removido') },
    onError: () => toast.error('Erro ao remover'),
  })

  const delFixedExpense = useMutation({
    mutationFn: deleteFixedExpense,
    onSuccess: () => { invalidate(); toast.success('Removido') },
    onError: () => toast.error('Erro ao remover'),
  })

  const delLaunch = useMutation({
    mutationFn: ({ id, deleteAll }: { id: number; deleteAll: boolean }) => deleteCreditCardLaunch(id, deleteAll),
    onSuccess: () => { invalidate(); toast.success('Removido') },
    onError: () => toast.error('Erro ao remover'),
  })

  if (isLoading || !budget) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="w-8 h-8 border-2 border-accent border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  const totalIncome = budget.salaryThiago + budget.salaryJuh + budget.extraIncomes.reduce((s, e) => s + e.value, 0)
  const totalPlanned = budget.fixedExpenses.reduce((s, e) => s + e.plannedValue, 0) + budget.creditCardLaunches.reduce((s, l) => s + l.value, 0)
  const totalActual = budget.fixedExpenses.reduce((s, e) => s + e.actualValue, 0) + budget.creditCardLaunches.reduce((s, l) => s + l.value, 0)
  const balancePlanned = totalIncome - totalPlanned
  const balanceActual = totalIncome - totalActual

  const filteredLaunches = budget.creditCardLaunches.filter(l =>
    (filterCard === '' || l.cardId === filterCard) &&
    (filterCategory === '' || l.categoryId === filterCategory)
  )

  function startEditSalary() {
    setSalThiago(budget!.salaryThiago)
    setSalJuh(budget!.salaryJuh)
    setEditingSalary(true)
  }

  function handleDeleteLaunch(launch: CreditCardLaunch) {
    if (launch.totalInstallments > 1 && launch.groupId) {
      if (window.confirm(`Esta compra tem ${launch.totalInstallments} parcelas. Deseja remover esta e as próximas parcelas?`)) {
        delLaunch.mutate({ id: launch.id, deleteAll: true })
      } else if (window.confirm('Remover apenas esta parcela?')) {
        delLaunch.mutate({ id: launch.id, deleteAll: false })
      }
    } else {
      if (window.confirm('Confirmar exclusão?')) delLaunch.mutate({ id: launch.id, deleteAll: false })
    }
  }

  const balanceColor = balanceActual >= 0 ? 'text-green-400' : 'text-red-400'

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button onClick={() => navigate('/dashboard')} className="btn-ghost p-2">
          <ArrowLeft size={18} />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-slate-100">
            {MONTH_NAMES[m - 1]} {y}
          </h1>
          <p className="text-slate-400 text-sm">Visão detalhada do mês</p>
        </div>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {[
          { label: 'Receita', value: totalIncome, color: 'text-green-400' },
          { label: 'Desp. Prevista', value: totalPlanned, color: 'text-amber-400' },
          { label: 'Desp. Realizada', value: totalActual, color: 'text-red-400' },
          { label: 'Saldo Real', value: balanceActual, color: balanceColor },
        ].map(c => (
          <div key={c.label} className="card">
            <p className="text-xs text-slate-500 mb-1">{c.label}</p>
            <p className={`text-base font-bold ${c.color}`}>{fmt(c.value)}</p>
          </div>
        ))}
      </div>

      {/* SECTION 1: Receitas */}
      <Section title="Receitas">
        <div className="space-y-3">
          {/* Salaries */}
          <div className="bg-bg-tertiary/40 rounded-lg p-3">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Salários</span>
              {!editingSalary ? (
                <button onClick={startEditSalary} className="btn-ghost py-1 px-2 text-xs"><Pencil size={12} /> Editar</button>
              ) : (
                <div className="flex gap-2">
                  <button onClick={() => setEditingSalary(false)} className="btn-ghost py-1 px-2 text-xs">Cancelar</button>
                  <button onClick={() => salaryMutation.mutate()} disabled={salaryMutation.isPending} className="btn-primary py-1 px-2 text-xs">Salvar</button>
                </div>
              )}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="label">Thiago</label>
                {editingSalary ? (
                  <input type="number" className="input text-sm" value={salThiago} onChange={e => setSalThiago(Number(e.target.value))} step="0.01" />
                ) : (
                  <span className="text-sm font-medium text-green-400">{fmt(budget.salaryThiago)}</span>
                )}
              </div>
              <div>
                <label className="label">Juh</label>
                {editingSalary ? (
                  <input type="number" className="input text-sm" value={salJuh} onChange={e => setSalJuh(Number(e.target.value))} step="0.01" />
                ) : (
                  <span className="text-sm font-medium text-green-400">{fmt(budget.salaryJuh)}</span>
                )}
              </div>
            </div>
          </div>

          {/* Extra Incomes */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">Rendas Extras</span>
              <button onClick={() => setExtraIncomeModal({ open: true })} className="btn-ghost py-1 px-2 text-xs">
                <Plus size={12} /> Adicionar
              </button>
            </div>
            {budget.extraIncomes.length === 0 ? (
              <p className="text-slate-500 text-xs">Nenhuma renda extra cadastrada</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-700/30">
                    <th className="table-header text-left py-1.5">Descrição</th>
                    <th className="table-header text-right py-1.5">Valor</th>
                    <th className="w-16" />
                  </tr>
                </thead>
                <tbody>
                  {budget.extraIncomes.map(e => (
                    <tr key={e.id} className="border-b border-slate-700/20">
                      <td className="py-2 text-slate-200">{e.description}</td>
                      <td className="py-2 text-right text-green-400 font-medium">{fmt(e.value)}</td>
                      <td className="py-2 text-right">
                        <div className="flex gap-1 justify-end">
                          <button onClick={() => setExtraIncomeModal({ open: true, item: e })} className="p-1 text-slate-400 hover:text-slate-100"><Pencil size={13} /></button>
                          <button onClick={() => { if (window.confirm('Confirmar?')) delExtraIncome.mutate(e.id) }} className="p-1 text-slate-400 hover:text-red-400"><Trash2 size={13} /></button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr><td className="py-2 text-xs text-slate-400">Total Receita</td><td className="py-2 text-right font-bold text-green-400" colSpan={2}>{fmt(totalIncome)}</td></tr>
                </tfoot>
              </table>
            )}
          </div>
        </div>
      </Section>

      {/* SECTION 2: Despesas Fixas */}
      <Section title="Despesas Fixas">
        <div className="flex justify-end mb-3">
          <button onClick={() => setFixedExpenseModal({ open: true })} className="btn-primary py-1.5 px-3 text-xs">
            <Plus size={13} /> Adicionar
          </button>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm min-w-[600px]">
            <thead>
              <tr className="border-b border-slate-700/50">
                {['Conta', 'Tipo', 'Previsto', 'Realizado', 'Categoria', ''].map(h => (
                  <th key={h} className="table-header text-left py-2 px-2 first:pl-0">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {budget.fixedExpenses.map(e => {
                const over = e.actualValue > e.plannedValue && e.plannedValue > 0
                return (
                  <tr key={e.id} className="border-b border-slate-700/20 hover:bg-bg-tertiary/30 transition-colors">
                    <td className="py-2 px-2 pl-0 text-slate-200">{e.name}</td>
                    <td className="py-2 px-2 text-slate-400 text-xs">{e.installmentType === 0 ? 'Mensal' : 'Parcelado'}</td>
                    <td className="py-2 px-2 text-amber-400">{fmt(e.plannedValue)}</td>
                    <td className="py-2 px-2">
                      <span className={over ? 'text-red-400 font-medium' : 'text-slate-200'}>{fmt(e.actualValue)}</span>
                    </td>
                    <td className="py-2 px-2">
                      <span className="text-xs px-1.5 py-0.5 rounded bg-bg-tertiary text-slate-400">{e.categoryName}</span>
                    </td>
                    <td className="py-2 px-2">
                      <div className="flex gap-1 justify-end">
                        <button onClick={() => setFixedExpenseModal({ open: true, item: e })} className="p-1 text-slate-400 hover:text-slate-100"><Pencil size={13} /></button>
                        <button onClick={() => { if (window.confirm('Confirmar?')) delFixedExpense.mutate(e.id) }} className="p-1 text-slate-400 hover:text-red-400"><Trash2 size={13} /></button>
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
            {budget.fixedExpenses.length > 0 && (
              <tfoot>
                <tr className="border-t border-slate-700/50 font-medium text-slate-300">
                  <td className="py-2 pl-0" colSpan={2}>Total</td>
                  <td className="py-2 px-2 text-amber-400">{fmt(budget.fixedExpenses.reduce((s, e) => s + e.plannedValue, 0))}</td>
                  <td className="py-2 px-2 text-slate-200">{fmt(budget.fixedExpenses.reduce((s, e) => s + e.actualValue, 0))}</td>
                  <td colSpan={2} />
                </tr>
              </tfoot>
            )}
          </table>
          {budget.fixedExpenses.length === 0 && <p className="text-slate-500 text-sm py-4 text-center">Nenhuma despesa fixa</p>}
        </div>
      </Section>

      {/* SECTION 3: Lançamentos de Fatura */}
      <Section title="Lançamentos de Fatura">
        <div className="flex flex-wrap items-center gap-2 mb-3">
          <select className="input text-xs w-auto" value={filterCard} onChange={e => setFilterCard(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">Todos os cartões</option>
            {cards.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <select className="input text-xs w-auto" value={filterCategory} onChange={e => setFilterCategory(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">Todas as categorias</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <div className="ml-auto flex gap-2">
            <button onClick={() => setPdfModal(true)} className="btn-ghost py-1.5 px-3 text-xs">
              <FileUp size={13} /> Importar PDF
            </button>
            <button onClick={() => setLaunchModal({ open: true })} className="btn-primary py-1.5 px-3 text-xs">
              <Plus size={13} /> Adicionar
            </button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm min-w-[800px]">
            <thead>
              <tr className="border-b border-slate-700/50">
                {['Descrição', 'Cartão', 'Data', 'Categoria', 'Parcela', 'Valor', 'Obs.', ''].map(h => (
                  <th key={h} className="table-header text-left py-2 px-2 first:pl-0">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {filteredLaunches.map(l => (
                <tr key={l.id} className="border-b border-slate-700/20 hover:bg-bg-tertiary/30 transition-colors">
                  <td className="py-2 px-2 pl-0 text-slate-200">{l.description}</td>
                  <td className="py-2 px-2 text-slate-400 text-xs">{l.cardName}</td>
                  <td className="py-2 px-2 text-slate-400 text-xs">{new Date(l.date).toLocaleDateString('pt-BR')}</td>
                  <td className="py-2 px-2 text-xs"><span className="px-1.5 py-0.5 rounded bg-bg-tertiary text-slate-400">{l.categoryName}</span></td>
                  <td className="py-2 px-2 text-xs text-slate-400">
                    {l.totalInstallments > 1 ? `${l.currentInstallment}/${l.totalInstallments}` : '—'}
                  </td>
                  <td className="py-2 px-2 font-medium text-slate-200">{fmt(l.value)}</td>
                  <td className="py-2 px-2 text-xs text-slate-500 max-w-[120px] truncate">{l.observation || '—'}</td>
                  <td className="py-2 px-2">
                    <div className="flex gap-1 justify-end">
                      <button onClick={() => setLaunchModal({ open: true, item: l })} className="p-1 text-slate-400 hover:text-slate-100"><Pencil size={13} /></button>
                      <button onClick={() => handleDeleteLaunch(l)} className="p-1 text-slate-400 hover:text-red-400"><Trash2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
            {filteredLaunches.length > 0 && (
              <tfoot>
                <tr className="border-t border-slate-700/50 font-medium text-slate-300">
                  <td className="py-2 pl-0" colSpan={5}>Total</td>
                  <td className="py-2 px-2">{fmt(filteredLaunches.reduce((s, l) => s + l.value, 0))}</td>
                  <td colSpan={2} />
                </tr>
              </tfoot>
            )}
          </table>
          {filteredLaunches.length === 0 && <p className="text-slate-500 text-sm py-4 text-center">Nenhum lançamento</p>}
        </div>

        {/* Card usage */}
        {cards.length > 0 && (
          <div className="mt-4 grid grid-cols-2 sm:grid-cols-4 gap-2">
            {cards.filter(c => c.monthlyGoal).map(c => {
              const usage = budget.creditCardLaunches.filter(l => l.cardId === c.id).reduce((s, l) => s + l.value, 0)
              const pct = c.monthlyGoal ? Math.min((usage / c.monthlyGoal) * 100, 100) : 0
              const over = c.monthlyGoal ? usage > c.monthlyGoal : false
              return (
                <div key={c.id} className="bg-bg-tertiary/40 rounded-lg p-2.5">
                  <p className="text-xs font-medium text-slate-300">{c.name}</p>
                  <p className={`text-sm font-bold mt-0.5 ${over ? 'text-red-400' : 'text-slate-100'}`}>{fmt(usage)}</p>
                  {c.monthlyGoal && (
                    <>
                      <div className="w-full bg-bg-tertiary rounded-full h-1 mt-1.5">
                        <div className={`h-1 rounded-full transition-all ${over ? 'bg-red-500' : 'bg-accent'}`} style={{ width: `${pct}%` }} />
                      </div>
                      <p className="text-xs text-slate-500 mt-1">Meta: {fmt(c.monthlyGoal)}</p>
                    </>
                  )}
                </div>
              )
            })}
          </div>
        )}
      </Section>

      {/* SECTION 4: Resumo por Categoria */}
      <Section title="Resumo por Categoria">
        {summary.length === 0 ? (
          <p className="text-slate-500 text-sm text-center py-4">Sem dados de categoria</p>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-700/50">
                    <th className="table-header text-left py-2">Categoria</th>
                    <th className="table-header text-right py-2">Total</th>
                    <th className="table-header text-right py-2">%</th>
                  </tr>
                </thead>
                <tbody>
                  {summary.map((s, i) => {
                    const total = summary.reduce((acc, x) => acc + x.total, 0)
                    const pct = total > 0 ? ((s.total / total) * 100).toFixed(1) : '0'
                    return (
                      <tr key={s.categoryId} className="border-b border-slate-700/20">
                        <td className="py-2 flex items-center gap-2">
                          <span className="w-2 h-2 rounded-full shrink-0" style={{ backgroundColor: DONUT_COLORS[i % DONUT_COLORS.length] }} />
                          <span className="text-slate-300">{s.categoryName}</span>
                        </td>
                        <td className="py-2 text-right font-medium text-slate-200">{fmt(s.total)}</td>
                        <td className="py-2 text-right text-slate-400 text-xs">{pct}%</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
            <ResponsiveContainer width="100%" height={260}>
              <PieChart>
                <Pie data={summary.map(s => ({ name: s.categoryName, value: s.total }))} cx="50%" cy="50%" innerRadius={55} outerRadius={90} paddingAngle={2} dataKey="value">
                  {summary.map((_, i) => <Cell key={i} fill={DONUT_COLORS[i % DONUT_COLORS.length]} />)}
                </Pie>
                <Tooltip contentStyle={{ backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: 8, fontSize: 12 }} formatter={(v: number) => fmt(v)} />
                <Legend wrapperStyle={{ fontSize: 11, color: '#94a3b8' }} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        )}
      </Section>

      {/* Modals */}
      {extraIncomeModal.open && (
        <ExtraIncomeModal
          budgetId={budget.id}
          item={extraIncomeModal.item}
          onClose={() => setExtraIncomeModal({ open: false })}
          onSave={async (desc, val) => {
            if (extraIncomeModal.item) {
              await updateExtraIncome(extraIncomeModal.item.id, desc, val)
            } else {
              await addExtraIncome(budget.id, desc, val)
            }
            invalidate()
            setExtraIncomeModal({ open: false })
          }}
        />
      )}

      {fixedExpenseModal.open && (
        <FixedExpenseModal
          budgetId={budget.id}
          item={fixedExpenseModal.item}
          categories={categories}
          onClose={() => setFixedExpenseModal({ open: false })}
          onSave={async (data) => {
            if (fixedExpenseModal.item) {
              await updateFixedExpense(fixedExpenseModal.item.id, data)
            } else {
              await addFixedExpense({ monthlyBudgetId: budget.id, ...data })
            }
            invalidate()
            setFixedExpenseModal({ open: false })
          }}
        />
      )}

      {pdfModal && (
        <PdfImportModal
          budgetId={budget.id}
          cards={cards}
          categories={categories}
          defaultCardId={filterCard !== '' ? filterCard : undefined}
          onClose={() => setPdfModal(false)}
          onImport={async (launches) => {
            for (const l of launches) {
              await addCreditCardLaunch(l)
            }
            invalidate()
          }}
        />
      )}

      {launchModal.open && (
        <CreditCardLaunchModal
          budgetId={budget.id}
          item={launchModal.item}
          cards={cards}
          categories={categories}
          onClose={() => setLaunchModal({ open: false })}
          onSave={async (data) => {
            if (launchModal.item) {
              await updateCreditCardLaunch(launchModal.item.id, data)
            } else {
              await addCreditCardLaunch({ monthlyBudgetId: budget.id, ...data })
            }
            invalidate()
            setLaunchModal({ open: false })
          }}
        />
      )}
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  const [collapsed, setCollapsed] = useState(false)
  return (
    <div className="card">
      <button
        onClick={() => setCollapsed(c => !c)}
        className="w-full flex items-center justify-between mb-0 hover:text-slate-100 transition-colors"
      >
        <h2 className="text-base font-semibold text-slate-200">{title}</h2>
        {collapsed ? <ChevronDown size={16} className="text-slate-400" /> : <ChevronUp size={16} className="text-slate-400" />}
      </button>
      {!collapsed && <div className="mt-4">{children}</div>}
    </div>
  )
}
