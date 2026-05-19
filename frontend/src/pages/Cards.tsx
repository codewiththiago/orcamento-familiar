import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil, Trash2, CreditCard } from 'lucide-react'
import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { getCards, createCard, updateCard, deleteCard } from '../api/cards'
import { fmt } from '../utils/format'
import { Modal } from '../components/modals/ExtraIncomeModal'
import type { Card } from '../types'

interface CardForm {
  name: string; limit: number | ''; closingDay: number; dueDay: number; monthlyGoal: number | ''
}

function CardModal({ item, onClose, onSave }: {
  item?: Card; onClose: () => void
  onSave: (data: CardForm) => Promise<void>
}) {
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<CardForm>({
    defaultValues: {
      name: item?.name ?? '',
      limit: item?.limit ?? '',
      closingDay: item?.closingDay ?? 10,
      dueDay: item?.dueDay ?? 15,
      monthlyGoal: item?.monthlyGoal ?? '',
    }
  })

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
    const payload = {
      name: data.name,
      limit: data.limit !== '' ? Number(data.limit) : undefined,
      closingDay: Number(data.closingDay),
      dueDay: Number(data.dueDay),
      monthlyGoal: data.monthlyGoal !== '' ? Number(data.monthlyGoal) : undefined,
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
          <p className="text-slate-400 text-sm mt-0.5">Gerencie seus cartões de crédito</p>
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
            const usagePct = card.monthlyGoal ? Math.min((card.currentMonthUsage / card.monthlyGoal) * 100, 100) : null
            const over = card.monthlyGoal ? card.currentMonthUsage > card.monthlyGoal : false
            return (
              <div key={card.id} className="card hover:border-slate-600 transition-colors">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center gap-2">
                    <div className="w-8 h-8 rounded-lg bg-accent/20 flex items-center justify-center">
                      <CreditCard size={15} className="text-accent" />
                    </div>
                    <h3 className="font-semibold text-slate-100">{card.name}</h3>
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
