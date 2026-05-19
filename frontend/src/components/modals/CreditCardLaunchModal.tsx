import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { Modal } from './ExtraIncomeModal'
import type { CreditCardLaunch, Card, Category } from '../../types'
import { format } from 'date-fns'

interface Props {
  budgetId: number
  item?: CreditCardLaunch
  cards: Card[]
  categories: Category[]
  onClose: () => void
  onSave: (data: {
    description: string; cardId: number; date: string;
    categoryId: number; totalInstallments: number; value: number; observation?: string
  }) => Promise<void>
}

interface Form {
  description: string; cardId: number; date: string;
  categoryId: number; totalInstallments: number; value: number; observation: string
}

export default function CreditCardLaunchModal({ item, cards, categories, onClose, onSave }: Props) {
  const isEdit = !!item
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<Form>({
    defaultValues: {
      description: item?.description ?? '',
      cardId: item?.cardId ?? cards[0]?.id ?? 0,
      date: item ? format(new Date(item.date), 'yyyy-MM-dd') : format(new Date(), 'yyyy-MM-dd'),
      categoryId: item?.categoryId ?? categories[0]?.id ?? 0,
      totalInstallments: item?.totalInstallments ?? 1,
      value: item?.value ?? 0,
      observation: item?.observation ?? '',
    }
  })

  async function onSubmit(data: Form) {
    try {
      await onSave({
        description: data.description,
        cardId: Number(data.cardId),
        date: data.date,
        categoryId: Number(data.categoryId),
        totalInstallments: Number(data.totalInstallments),
        value: Number(data.value),
        observation: data.observation || undefined,
      })
      toast.success(isEdit ? 'Atualizado!' : 'Adicionado!')
    } catch {
      toast.error('Erro ao salvar')
    }
  }

  return (
    <Modal title={isEdit ? 'Editar Lançamento' : 'Novo Lançamento'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Descrição</label>
          <input {...register('description', { required: true })} className="input" placeholder="Ex: Supermercado" />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Cartão</label>
            <select {...register('cardId')} className="input">
              {cards.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
          <div>
            <label className="label">Data</label>
            <input {...register('date')} type="date" className="input" />
          </div>
        </div>
        <div>
          <label className="label">Categoria</label>
          <select {...register('categoryId')} className="input">
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Valor (R$)</label>
            <input {...register('value', { min: 0.01 })} type="number" step="0.01" className="input" />
          </div>
          {!isEdit && (
            <div>
              <label className="label">Nº Parcelas</label>
              <input {...register('totalInstallments', { min: 1 })} type="number" min={1} className="input" />
            </div>
          )}
        </div>
        <div>
          <label className="label">Observação (opcional)</label>
          <input {...register('observation')} className="input" placeholder="..." />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-ghost">Cancelar</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">{isSubmitting ? '...' : 'Salvar'}</button>
        </div>
      </form>
    </Modal>
  )
}
