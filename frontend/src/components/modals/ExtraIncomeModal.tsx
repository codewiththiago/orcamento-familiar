import { useForm } from 'react-hook-form'
import { X } from 'lucide-react'
import type { ExtraIncome } from '../../types'
import toast from 'react-hot-toast'

interface Props {
  budgetId: number
  item?: ExtraIncome
  onClose: () => void
  onSave: (description: string, value: number) => Promise<void>
}

interface Form { description: string; value: number }

export default function ExtraIncomeModal({ item, onClose, onSave }: Props) {
  const { register, handleSubmit, formState: { isSubmitting, errors } } = useForm<Form>({
    defaultValues: { description: item?.description ?? '', value: item?.value ?? 0 }
  })

  async function onSubmit(data: Form) {
    try {
      await onSave(data.description, Number(data.value))
      toast.success(item ? 'Atualizado!' : 'Adicionado!')
    } catch {
      toast.error('Erro ao salvar')
    }
  }

  return (
    <Modal title={item ? 'Editar Renda Extra' : 'Nova Renda Extra'} onClose={onClose}>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
        <div>
          <label className="label">Descrição</label>
          <input {...register('description', { required: 'Obrigatório' })} className="input" placeholder="Ex: Freelance" />
          {errors.description && <p className="text-red-400 text-xs mt-1">{errors.description.message}</p>}
        </div>
        <div>
          <label className="label">Valor (R$)</label>
          <input {...register('value', { required: true, min: 0.01 })} type="number" step="0.01" className="input" />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <button type="button" onClick={onClose} className="btn-ghost">Cancelar</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">{isSubmitting ? '...' : 'Salvar'}</button>
        </div>
      </form>
    </Modal>
  )
}

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-bg-secondary border border-slate-700/50 rounded-xl w-full max-w-md shadow-2xl">
        <div className="flex items-center justify-between p-4 border-b border-slate-700/50">
          <h3 className="font-semibold text-slate-100">{title}</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-100"><X size={18} /></button>
        </div>
        <div className="p-4">{children}</div>
      </div>
    </div>
  )
}
