import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CalendarClock, Loader2 } from 'lucide-react'
import { getFutureCommitments } from '../api/insights'
import { fmt } from '../utils/format'

export default function FutureCommitments() {
  const now = new Date()
  const [months, setMonths] = useState(6)

  const { data: commitments = [], isLoading } = useQuery({
    queryKey: ['commitments', now.getFullYear(), now.getMonth() + 1, months],
    queryFn: () => getFutureCommitments(now.getFullYear(), now.getMonth() + 1, months),
  })

  const grandTotal = commitments.reduce((s, c) => s + c.total, 0)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-100">Compromissos Futuros</h1>
          <p className="text-slate-400 text-sm mt-0.5">Quanto dos próximos meses já está comprometido</p>
        </div>
        <select className="input w-auto text-sm" value={months} onChange={e => setMonths(Number(e.target.value))}>
          <option value={3}>3 meses</option>
          <option value={6}>6 meses</option>
          <option value={12}>12 meses</option>
        </select>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center h-40">
          <Loader2 size={22} className="animate-spin text-accent" />
        </div>
      ) : (
        <div className="card overflow-x-auto">
          <table className="w-full text-sm min-w-[560px]">
            <thead>
              <tr className="border-b border-slate-700/50">
                {['Mês', 'Parcelas', 'Lançamentos de cartão', 'Despesas fixas', 'Total'].map(h => (
                  <th key={h} className="table-header text-left py-2 px-2 first:pl-0">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {commitments.map(c => (
                <tr key={`${c.year}-${c.month}`} className="border-b border-slate-700/20 hover:bg-bg-tertiary/30">
                  <td className="py-2.5 px-2 pl-0 font-medium text-slate-200">
                    {c.monthName} {c.year}
                    {now.getFullYear() === c.year && now.getMonth() + 1 === c.month && (
                      <span className="ml-2 px-1.5 py-0.5 rounded bg-accent/20 text-accent text-xs">atual</span>
                    )}
                  </td>
                  <td className="py-2.5 px-2 text-amber-400">{fmt(c.installments)}</td>
                  <td className="py-2.5 px-2 text-blue-400">{fmt(c.cardLaunches)}</td>
                  <td className="py-2.5 px-2 text-slate-300">{fmt(c.fixedExpenses)}</td>
                  <td className="py-2.5 px-2 font-bold text-slate-100">{fmt(c.total)}</td>
                </tr>
              ))}
            </tbody>
            {commitments.length > 0 && (
              <tfoot>
                <tr className="border-t-2 border-slate-600 font-semibold text-slate-200">
                  <td className="py-2.5 pl-0">Total no período</td>
                  <td className="py-2.5 px-2 text-amber-400">{fmt(commitments.reduce((s, c) => s + c.installments, 0))}</td>
                  <td className="py-2.5 px-2 text-blue-400">{fmt(commitments.reduce((s, c) => s + c.cardLaunches, 0))}</td>
                  <td className="py-2.5 px-2">{fmt(commitments.reduce((s, c) => s + c.fixedExpenses, 0))}</td>
                  <td className="py-2.5 px-2">{fmt(grandTotal)}</td>
                </tr>
              </tfoot>
            )}
          </table>

          <div className="mt-4 flex items-center gap-2 text-xs text-slate-500">
            <CalendarClock size={14} />
            <span>
              Parcelas: transações parceladas do novo modelo · Cartão: lançamentos de fatura · Despesas fixas: valores previstos no orçamento mensal
            </span>
          </div>
        </div>
      )}
    </div>
  )
}