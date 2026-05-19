import { useForm } from 'react-hook-form'
import toast from 'react-hot-toast'
import { Modal } from './ExtraIncomeModal'
import type { FixedExpense, Category, InstallmentType } from '../../types'

interface Props {
  budgetId: number
  item?: FixedExpense
  categories: Category[]
  onClose: () => void
  onSave: (data: {
    name: string; installmentType: InstallmentType;
    plannedValue: number; actualValue: number; categoryId: number
  }) => Promise<void>
}

interface Form {
  name: string
  installmentType: InstallmentType
  plannedValue: number
  actualValue: number
  categoryId: number
}

export default function FixedExpenseModal({ item, categories, onClose, onSave }: Props) {
  const { register, handleSubmit, formState: { isSubmitting, errors } } = useForm<Form>({
    defaultValues: {
      name: item?.name ?? '',
      installmentType: item?.installmentType ?? 0,
      plannedValue: item?.plannedValue ?? 0,
      actualValue: item?.actualValue ?? 0,
      categoryId: item?.categoryId ?? categories[0]?.id ?? 0,
    }
  })

  async function onSubmit(data: Form) {
    try {
      await onSave({
        ...data,
        installmentType: Number(data.installmentType) as InstallmentType,
        plannedValue: Number(data.plannedValue),
        actualValue: Number(data.actualValue),
        categoryId: Number(data.categoryId),
      })
      toast.success(item ? 'Atualizado!' : 'Adicionado!')
    } catch {
      toast.error('Erro ao salvar')
    }
  }

  return (
    <Modal title={item ? 'Editar Despesa Fixa' : 'Nova Despesa Fixa'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Nome</label>
          <input {...register('name', { required: 'Obrigatório' })} className="input" placeholder="Ex: Aluguel" />
          {errors.name && <p className="text-red-400 text-xs mt-1">{errors.name.message}</p>}
        </div>
        <div>
          <label className="label">Tipo</label>
          <select {...register('installmentType')} className="input">
            <option value={0}>Mensal</option>
            <option value={1}>Parcelado</option>
          </select>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Previsto (R$)</label>
            <input {...register('plannedValue', { min: 0 })} type="number" step="0.01" className="input" />
          </div>
          <div>
            <label className="label">Realizado (R$)</label>
            <input {...register('actualValue', { min: 0 })} type="number" step="0.01" className="input" />
          </div>
        </div>
        <div>
          <label className="label">Categoria</label>
          <select {...register('categoryId', { required: true })} className="input">
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-ghost">Cancelar</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">{isSubmitting ? '...' : 'Salvar'}</button>
        </div>
      </form>
    </Modal>
  )
}
