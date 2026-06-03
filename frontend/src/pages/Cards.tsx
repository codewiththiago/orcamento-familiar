import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, CreditCard, Wallet } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { getCards, createCard, updateCard, deleteCard } from '../api/cards'
import { fmt } from '../utils/format'
import { Modal } from '../components/modals/ExtraIncomeModal'
import type { Card } from '../types'

const MONTH_NAMES = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']

interface CardForm {
  name: string
  cardType: 0 | 1
  limit: number | ''
  closingDay: number
  dueDay: number
  monthlyGoal: number | ''
  monthlyCredit: number | ''
  creditSinceYear: number | ''
  creditSinceMonth: number | ''
  initialBalance: number | ''
}

function CardModal({ item, onClose, onSave }: {
  item?: Card; onClose: () => void
  onSave: (data: CardForm) => Promise<void>
}) {
  const now = new Date()
  const { register, handleSubmit, watch, formState: { isSubmitting } } = useForm<CardForm>({
    defaultValues: {
      name: item?.name ?? '',
      cardType: item?.cardType ?? 0,
      limit: item?.limit ?? '',
      closingDay: item?.closingDay ?? 10,
      dueDay: item?.dueDay ?? 15,
      monthlyGoal: item?.monthlyGoal ?? '',
      monthlyCredit: item?.monthlyCredit ?? '',
      creditSinceYear: item?.creditSinceYear ?? now.getFullYear(),
      creditSinceMonth: item?.creditSinceMonth ?? (now.getMonth() + 1),
      initialBalance: item?.initialBalance ?? '',
    }
  })

  const cardType = watch('cardType')
  const isPrepaid = Number(cardType) === 1

  async function onSubmit(data: CardForm) {
    try {
      await onSave(data)
      toast.success(item ? 'Cartão atualizado!' : 'Cartão criado!')
    } catch {
      toast.error('Erro ao salvar cartão')
    }
  }

  return (
    <Modal title={item ? 'Editar Cartão' : 'Novo Cartão'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Nome</label>
          <input {...register('name', { required: true })} className="input" placeholder="Ex: Nubank" />
        </div>

        <div>
          <label className="label">Tipo</label>
          <select {...register('cardType')} className="input">
            <option value={0}>Crédito</option>
            <option value={1}>Pré-pago</option>
          </select>
        </div>

        {!isPrepaid && (
          <>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="label">Limite (R$, opcional)</label>
                <input {...register('limit')} type="number" step="0.01" className="input" placeholder="—" />
              </div>
              <div>
                <label className="label">Meta Mensal (R$, opcional)</label>
                <input {...register('monthlyGoal')} type="number" step="0.01" className="input" placeholder="—" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="label">Dia de Fechamento</label>
                <input {...register('closingDay', { min: 1, max: 31 })} type="number" className="input" />
              </div>
              <div>
                <label className="label">Dia de Vencimento</label>
                <input {...register('dueDay', { min: 1, max: 31 })} type="number" className="input" />
              </div>
            </div>
          </>
        )}

        {isPrepaid && (
          <>
            <div>
              <label className="label">Crédito Mensal (R$)</label>
              <input {...register('monthlyCredit')} type="number" step="0.01" className="input" placeholder="Ex: 500" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="label">Desde mês</label>
                <select {...register('creditSinceMonth')} className="input">
                  {MONTH_NAMES.map((m, i) => (
                    <option key={i + 1} value={i + 1}>{m}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="label">Desde ano</label>
                <input {...register('creditSinceYear')} type="number" className="input" placeholder={String(now.getFullYear())} />
              </div>
            </div>
            <div>
              <label className="label">Saldo inicial (R$, opcional)</label>
              <input {...register('initialBalance')} type="number" step="0.01" className="input" placeholder="0" />
            </div>
          </>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-ghost">Cancelar</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">{isSubmitting ? '...' : 'Salvar'}</button>
        </div>
      </form>
    </Modal>
  )
}

export default function Cards() {
  const qc = useQueryClient()
  const [modal, setModal] = useState<{ open: boolean; item?: Card }>({ open: false })
  const now = new Date()

  const { data: cards = [], isLoading } = useQuery({
    queryKey: ['cards', now.getFullYear(), now.getMonth() + 1],
    queryFn: () => getCards(now.getFullYear(), now.getMonth() + 1),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteCard,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['cards'] }); toast.success('Cartão removido') },
    onError: () => toast.error('Erro ao remover'),
  })

  async function handleSave(data: CardForm) {
    const isPrepaid = Number(data.cardType) === 1
    const payload = {
      name: data.name,
      cardType: Number(data.cardType) as 0 | 1,
      limit: !isPrepaid && data.limit !== '' ? Number(data.limit) : undefined,
      closingDay: !isPrepaid ? Number(data.closingDay) : 1,
      dueDay: !isPrepaid ? Number(data.dueDay) : 1,
      monthlyGoal: !isPrepaid && data.monthlyGoal !== '' ? Number(data.monthlyGoal) : undefined,
      monthlyCredit: isPrepaid && data.monthlyCredit !== '' ? Number(data.monthlyCredit) : undefined,
      creditSinceYear: isPrepaid && data.creditSinceYear !== '' ? Number(data.creditSinceYear) : undefined,
      creditSinceMonth: isPrepaid && data.creditSinceMonth !== '' ? Number(data.creditSinceMonth) : undefined,
      initialBalance: isPrepaid && data.initialBalance !== '' ? Number(data.initialBalance) : undefined,
    }
    if (modal.item) {
      await updateCard(modal.item.id, payload)
    } else {
      await createCard(payload)
    }
    qc.invalidateQueries({ queryKey: ['cards'] })
    setModal({ open: false })
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-100">Cartões</h1>
          <p className="text-slate-400 text-sm mt-0.5">Gerencie seus cartões</p>
        </div>
        <button onClick={() => setModal({ open: true })} className="btn-primary">
          <Plus size={16} /> Novo Cartão
        </button>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center h-40">
          <div className="w-6 h-6 border-2 border-accent border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {cards.map(card => {
            const isPrepaid = card.cardType === 1
            const usagePct = card.monthlyGoal ? Math.min((card.currentMonthUsage / card.monthlyGoal) * 100, 100) : null
            const over = card.monthlyGoal ? card.currentMonthUsage > card.monthlyGoal : false
            const balanceLow = isPrepaid && card.currentBalance !== undefined && card.monthlyCredit
              ? card.currentBalance < card.monthlyCredit * 0.2
              : false

            return (
              <div key={card.id} className="card hover:border-slate-600 transition-colors">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2">
                    <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${isPrepaid ? 'bg-emerald-500/20' : 'bg-accent/20'}`}>
                      {isPrepaid
                        ? <Wallet size={15} className="text-emerald-400" />
                        : <CreditCard size={15} className="text-accent" />
                      }
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-100">{card.name}</h3>
                      <span className={`text-xs ${isPrepaid ? 'text-emerald-400' : 'text-slate-500'}`}>
                        {isPrepaid ? 'Pré-pago' : 'Crédito'}
                      </span>
                    </div>
                  </div>
                  <div className="flex gap-1">
                    <button onClick={() => setModal({ open: true, item: card })} className="p-1.5 text-slate-400 hover:text-slate-100 rounded">
                      <Pencil size={14} />
                    </button>
                    <button onClick={() => { if (window.confirm('Remover cartão?')) deleteMutation.mutate(card.id) }} className="p-1.5 text-slate-400 hover:text-red-400 rounded">
                      <Trash2 size={14} />
                    </button>
                  </div>
                </div>

                <div className="space-y-2 text-sm">
                  {isPrepaid ? (
                    <>
                      <div className="flex justify-between">
                        <span className="text-slate-400">Crédito mensal</span>
                        <span className="text-emerald-400 font-medium">{fmt(card.monthlyCredit ?? 0)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-slate-400">Gasto este mês</span>
                        <span className="text-slate-200">{fmt(card.currentMonthUsage)}</span>
                      </div>
                      {card.currentBalance !== undefined && (
                        <div className="flex justify-between">
                          <span className="text-slate-400">Saldo acumulado</span>
                          <span className={`font-semibold ${balanceLow ? 'text-amber-400' : 'text-emerald-400'}`}>
                            {fmt(card.currentBalance)}
                          </span>
                        </div>
                      )}
                    </>
                  ) : (
                    <>
                      {card.limit && (
                        <div className="flex justify-between">
                          <span className="text-slate-400">Limite</span>
                          <span className="text-slate-200">{fmt(card.limit)}</span>
                        </div>
                      )}
                      <div className="flex justify-between">
                        <span className="text-slate-400">Fechamento / Venc.</span>
                        <span className="text-slate-200">Dia {card.closingDay} / Dia {card.dueDay}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-slate-400">Uso este mês</span>
                        <span className={over ? 'text-red-400 font-medium' : 'text-slate-200'}>{fmt(card.currentMonthUsage)}</span>
                      </div>
                      {card.monthlyGoal && (
                        <>
                          <div className="flex justify-between">
                            <span className="text-slate-400">Meta mensal</span>
                            <span className="text-slate-200">{fmt(card.monthlyGoal)}</span>
                          </div>
                          <div>
                            <div className="flex justify-between text-xs text-slate-500 mb-1">
                              <span>{usagePct?.toFixed(0)}%</span>
                              <span>{over ? 'Acima da meta!' : 'Dentro da meta'}</span>
                            </div>
                            <div className="w-full bg-bg-tertiary rounded-full h-1.5">
                              <div
                                className={`h-1.5 rounded-full transition-all ${over ? 'bg-red-500' : 'bg-accent'}`}
                                style={{ width: `${usagePct}%` }}
                              />
                            </div>
                          </div>
                        </>
                      )}
                    </>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {modal.open && (
        <CardModal
          item={modal.item}
          onClose={() => setModal({ open: false })}
          onSave={handleSave}
        />
      )}
    </div>
  )
}
