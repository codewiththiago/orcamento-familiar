import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, TrendingUp, TrendingDown, Wallet, CreditCard, Tag } from 'lucide-react'
import {
  BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import { getDashboard } from '../api/budget'
import { getMonthlyInsights } from '../api/insights'
import { fmt } from '../utils/format'
import type { MonthSummary } from '../types'

function ValueCell({ value, compare }: { value: number; compare?: number }) {
  if (value === 0 && compare === undefined) return <span className="text-slate-500">—</span>
  const isOver = compare !== undefined && value > compare
  return (
    <span className={isOver ? 'text-red-400 font-medium' : value < 0 ? 'text-red-400' : value > 0 ? 'text-green-400' : 'text-slate-300'}>
      {fmt(value)}
    </span>
  )
}

const MONTH_ABBR = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']

const tooltipStyle = {
  contentStyle: { backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: 8, fontSize: 12 },
  labelStyle: { color: '#94a3b8' },
}

export default function Dashboard() {
  const [year, setYear] = useState(new Date().getFullYear())
  const navigate = useNavigate()

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', year],
    queryFn: () => getDashboard(year),
  })

  const now = new Date()
  const { data: insights } = useQuery({
    queryKey: ['insights', now.getFullYear(), now.getMonth() + 1],
    queryFn: () => getMonthlyInsights(now.getFullYear(), now.getMonth() + 1),
  })

  const chartData = (data?.months ?? []).map((m) => ({
    name: MONTH_ABBR[m.month - 1],
    Receita: m.totalIncome,
    Despesa: m.actualExpense,
    Saldo: m.actualBalance,
  }))

  function handleMonthClick(m: MonthSummary) {
    navigate(`/budget/${year}/${m.month}`)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-100">Dashboard</h1>
          <p className="text-slate-400 text-sm mt-0.5">Visão geral do ano</p>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={() => setYear(y => y - 1)} className="btn-ghost p-2">
            <ChevronLeft size={18} />
          </button>
          <span className="font-bold text-xl text-slate-100 min-w-[4rem] text-center">{year}</span>
          <button onClick={() => setYear(y => y + 1)} className="btn-ghost p-2">
            <ChevronRight size={18} />
          </button>
        </div>
      </div>

      {/* Monthly overview */}
      {insights && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            {[
              { label: 'Receita do mês', value: insights.income, color: 'text-green-400' },
              { label: 'Gasto até agora', value: insights.spent, color: 'text-red-400' },
              { label: 'Valor comprometido', value: insights.committed, color: 'text-amber-400' },
              { label: 'Valor disponível', value: insights.available, color: insights.available >= 0 ? 'text-accent' : 'text-red-400' },
            ].map(c => (
              <div key={c.label} className="card">
                <p className="text-xs text-slate-500 mb-1">{c.label}</p>
                <p className={`text-lg font-bold ${c.color}`}>{fmt(c.value)}</p>
              </div>
            ))}
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="card">
              <h2 className="text-sm font-semibold text-slate-300 mb-3 flex items-center gap-2">
                <CreditCard size={15} className="text-accent" /> Gasto por conta
              </h2>
              {insights.byAccount.length === 0 ? (
                <p className="text-slate-500 text-sm py-4 text-center">Sem gastos neste mês</p>
              ) : (
                <div className="space-y-2">
                  {insights.byAccount.map(a => (
                    <div key={a.accountId} className="flex items-center justify-between text-sm">
                      <span className="text-slate-300">{a.accountName}</span>
                      <span className="text-slate-100 font-medium">{fmt(a.spent)}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
            <div className="card">
              <h2 className="text-sm font-semibold text-slate-300 mb-3 flex items-center gap-2">
                <Tag size={15} className="text-green-400" /> Gasto por categoria
              </h2>
              {insights.byCategory.length === 0 ? (
                <p className="text-slate-500 text-sm py-4 text-center">Sem gastos neste mês</p>
              ) : (
                <div className="space-y-2 max-h-56 overflow-y-auto">
                  {insights.byCategory.map(c => (
                    <div key={c.categoryId ?? 'none'} className="flex items-center justify-between text-sm">
                      <span className="text-slate-300">{c.categoryName}</span>
                      <span className="text-slate-100 font-medium">{fmt(c.total)}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Annual table */}
      <div className="card overflow-x-auto">
        <h2 className="text-sm font-semibold text-slate-300 mb-4">Resumo Anual</h2>
        {isLoading ? (
          <div className="flex items-center justify-center h-40">
            <div className="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <table className="w-full text-sm min-w-[800px]">
            <thead>
              <tr className="border-b border-slate-700/50">
                {['Mês', 'Receita', 'Desp. Prevista', 'Desp. Realizada', 'Saldo Previsto', 'Saldo Real'].map(h => (
                  <th key={h} className="table-header text-left py-2 px-3 first:pl-0">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data?.months.map((m) => (
                <tr
                  key={m.month}
                  onClick={() => handleMonthClick(m)}
                  className="border-b border-slate-700/30 hover:bg-bg-tertiary/50 cursor-pointer transition-colors"
                >
                  <td className="py-2.5 px-3 pl-0 font-medium text-slate-200">{m.monthName}</td>
                  <td className="py-2.5 px-3"><ValueCell value={m.totalIncome} /></td>
                  <td className="py-2.5 px-3"><ValueCell value={m.plannedExpense} /></td>
                  <td className="py-2.5 px-3"><ValueCell value={m.actualExpense} compare={m.plannedExpense} /></td>
                  <td className="py-2.5 px-3"><ValueCell value={m.plannedBalance} /></td>
                  <td className="py-2.5 px-3"><ValueCell value={m.actualBalance} /></td>
                </tr>
              ))}
            </tbody>
            {data && (
              <tfoot>
                <tr className="border-t-2 border-slate-600 font-semibold text-slate-200">
                  <td className="py-2.5 pl-0">Total</td>
                  <td className="py-2.5 px-3">{fmt(data.months.reduce((s, m) => s + m.totalIncome, 0))}</td>
                  <td className="py-2.5 px-3">{fmt(data.months.reduce((s, m) => s + m.plannedExpense, 0))}</td>
                  <td className="py-2.5 px-3">{fmt(data.months.reduce((s, m) => s + m.actualExpense, 0))}</td>
                  <td className="py-2.5 px-3">{fmt(data.months.reduce((s, m) => s + m.plannedBalance, 0))}</td>
                  <td className="py-2.5 px-3">{fmt(data.months.reduce((s, m) => s + m.actualBalance, 0))}</td>
                </tr>
              </tfoot>
            )}
          </table>
        )}
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="card">
          <h2 className="text-sm font-semibold text-slate-300 mb-4 flex items-center gap-2">
            <TrendingDown size={16} className="text-accent" /> Receita vs Despesa
          </h2>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={chartData} barSize={10}>
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
              <XAxis dataKey="name" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 10 }} axisLine={false} tickLine={false} tickFormatter={v => `R$${(v/1000).toFixed(0)}k`} />
              <Tooltip {...tooltipStyle} formatter={(v: number) => fmt(v)} />
              <Legend wrapperStyle={{ fontSize: 12, color: '#94a3b8' }} />
              <Bar dataKey="Receita" fill="#6366f1" radius={[3, 3, 0, 0]} />
              <Bar dataKey="Despesa" fill="#ef4444" radius={[3, 3, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="card">
          <h2 className="text-sm font-semibold text-slate-300 mb-4 flex items-center gap-2">
            <TrendingUp size={16} className="text-green-400" /> Evolução do Saldo
          </h2>
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
              <XAxis dataKey="name" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fill: '#94a3b8', fontSize: 10 }} axisLine={false} tickLine={false} tickFormatter={v => `R$${(v/1000).toFixed(0)}k`} />
              <Tooltip {...tooltipStyle} formatter={(v: number) => fmt(v)} />
              <Line dataKey="Saldo" stroke="#22c55e" strokeWidth={2} dot={{ fill: '#22c55e', r: 3 }} activeDot={{ r: 5 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Recent & top expenses */}
      {insights && (insights.recentTransactions.length > 0 || insights.topExpenses.length > 0) && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <div className="card">
            <h2 className="text-sm font-semibold text-slate-300 mb-3 flex items-center gap-2">
              <Wallet size={15} className="text-accent" /> Gastos recentes
            </h2>
            <div className="space-y-2">
              {insights.recentTransactions.slice(0, 6).map(t => (
                <div key={t.id} className="flex items-center justify-between text-sm gap-3">
                  <div className="min-w-0">
                    <p className="text-slate-200 truncate">{t.description}</p>
                    <p className="text-xs text-slate-500">
                      {new Date(t.transactionDate).toLocaleDateString('pt-BR', { timeZone: 'UTC', day: '2-digit', month: '2-digit' })} · {t.financialAccountName}
                    </p>
                  </div>
                  <span className={`shrink-0 font-medium ${t.type === 0 ? 'text-green-400' : 'text-slate-100'}`}>
                    {t.type === 0 ? '+' : '−'}{fmt(t.amount)}
                  </span>
                </div>
              ))}
            </div>
          </div>
          <div className="card">
            <h2 className="text-sm font-semibold text-slate-300 mb-3 flex items-center gap-2">
              <TrendingDown size={15} className="text-red-400" /> Maiores gastos do mês
            </h2>
            <div className="space-y-2">
              {insights.topExpenses.slice(0, 6).map(t => (
                <div key={t.id} className="flex items-center justify-between text-sm gap-3">
                  <div className="min-w-0">
                    <p className="text-slate-200 truncate">{t.description}</p>
                    <p className="text-xs text-slate-500">
                      {new Date(t.transactionDate).toLocaleDateString('pt-BR', { timeZone: 'UTC', day: '2-digit', month: '2-digit' })} · {t.financialAccountName}
                    </p>
                  </div>
                  <span className="shrink-0 font-medium text-red-400">{fmt(t.amount)}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
