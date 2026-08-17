import { useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Upload, FileText, Loader2, CheckCircle2, XCircle, Filter, Trash2, Plus } from 'lucide-react'
import toast from 'react-hot-toast'
import { previewImport, confirmImport, getImportHistory } from '../api/imports'
import { getAccounts } from '../api/accounts'
import { getCategories } from '../api/categories'
import { createCategorizationRule } from '../api/categorizationRules'
import { fmt } from '../utils/format'
import type { ConfirmImportItem, ImportFormat, ImportPreview, ImportPreviewItem, ImportResult, TransactionType } from '../types'

const FORMAT_LABELS = { 0: 'CSV', 1: 'OFX', 2: 'PDF' }
const INSTITUTIONS = [
  { value: '', label: 'Genérico' },
  { value: 'c6', label: 'C6' },
  { value: 'picpay', label: 'PicPay' },
  { value: 'nubank', label: 'Nubank' },
]

interface PreviewRow {
  item: ImportPreviewItem
  selected: boolean
  categoryId: number
  ruleCreated: boolean
  edited: boolean
}

export default function Imports() {
  const qc = useQueryClient()
  const fileRef = useRef<HTMLInputElement>(null)
  const [fileName, setFileName] = useState('')
  const [accountId, setAccountId] = useState<number>(0)
  const [format, setFormat] = useState<ImportFormat>(0)
  const [institution, setInstitution] = useState('')
  const [loading, setLoading] = useState(false)
  const [preview, setPreview] = useState<ImportPreview | null>(null)
  const [rows, setRows] = useState<PreviewRow[]>([])
  const [confirming, setConfirming] = useState(false)
  const [result, setResult] = useState<ImportResult | null>(null)

  const { data: accounts = [] } = useQuery({ queryKey: ['accounts'], queryFn: getAccounts })
  const { data: categories = [] } = useQuery({ queryKey: ['categories'], queryFn: getCategories })
  const { data: history = [], isLoading: loadingHistory } = useQuery({
    queryKey: ['import-history'],
    queryFn: getImportHistory,
  })

  async function handleFile(file: File) {
    if (!accountId) { toast.error('Selecione a conta financeira'); return }
    setFileName(file.name)
    setLoading(true)
    setResult(null)
    try {
      const data = await previewImport(file, accountId, format, institution)
      setPreview(data)
      setRows(data.items.map(item => ({
        item,
        selected: !item.isDuplicate,
        categoryId: item.categoryId ?? 0,
        ruleCreated: false,
        edited: false,
      })))
      if (data.totalFound === 0) {
        toast.error('Nenhuma transação encontrada no arquivo. Verifique o formato.')
      } else {
        toast.success(`${data.totalFound} transações encontradas`)
      }
    } catch (err: any) {
      toast.error(err?.response?.data?.message ?? 'Erro ao processar o arquivo')
      setRows([])
      setPreview(null)
    } finally {
      setLoading(false)
    }
  }

  function updateRow(index: number, patch: Partial<PreviewRow>) {
    setRows(rows.map((r, i) => i === index ? { ...r, ...patch } : r))
  }

  function toggleRow(index: number) {
    updateRow(index, { selected: !rows[index].selected })
  }

  const allSelected = rows.length > 0 && rows.every(r => r.selected)
  function toggleAll() {
    setRows(rows.map(r => ({ ...r, selected: !allSelected })))
  }

  async function handleCreateRule(row: PreviewRow, index: number) {
    if (!row.categoryId) return
    try {
      await createCategorizationRule({
        pattern: row.item.description,
        matchType: 1,
        categoryId: row.categoryId,
        priority: 100,
        financialAccountId: accountId || undefined,
      })
      updateRow(index, { ruleCreated: true })
      toast.success('Regra criada para futuras transações semelhantes')
    } catch {
      toast.error('Erro ao criar regra')
    }
  }

  async function handleConfirm() {
    const selected = rows.filter(r => r.selected)
    if (selected.length === 0) { toast.error('Selecione ao menos uma transação'); return }

    setConfirming(true)
    try {
      const items: ConfirmImportItem[] = selected.map(r => ({
        description: r.item.description,
        amount: r.item.amount,
        transactionDate: r.item.transactionDate.substring(0, 10),
        type: r.item.type,
        externalId: r.item.externalId,
        categoryId: r.categoryId || undefined,
      }))

      const res = await confirmImport({
        financialAccountId: accountId,
        fileName,
        format,
        institution: institution || undefined,
        items,
      })
      setResult(res)
      setPreview(null)
      setRows([])
      setFileName('')
      qc.invalidateQueries({ queryKey: ['import-history'] })
      qc.invalidateQueries({ queryKey: ['transactions'] })
      qc.invalidateQueries({ queryKey: ['accounts'] })
      qc.invalidateQueries({ queryKey: ['insights'] })
      toast.success(`${res.imported} transações importadas`)
    } catch {
      toast.error('Erro ao confirmar importação')
    } finally {
      setConfirming(false)
    }
  }

  const selectedCount = rows.filter(r => r.selected).length
  const selectedTotal = rows.filter(r => r.selected).reduce((s, r) => s + r.item.amount, 0)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-100">Importar Extrato</h1>
        <p className="text-slate-400 text-sm mt-0.5">CSV ou OFX → revisão → importação sem duplicatas</p>
      </div>

      {/* Upload controls */}
      <div className="card space-y-3">
        <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
          <div>
            <label className="label">Conta financeira</label>
            <select className="input" value={accountId} onChange={e => setAccountId(Number(e.target.value))}>
              <option value={0}>— Selecionar —</option>
              {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
            </select>
          </div>
          <div>
            <label className="label">Formato</label>
            <select className="input" value={format} onChange={e => setFormat(Number(e.target.value) as ImportFormat)}>
              <option value={0}>CSV</option>
              <option value={1}>OFX</option>
            </select>
          </div>
          <div>
            <label className="label">Instituição</label>
            <select className="input" value={institution} onChange={e => setInstitution(e.target.value)}>
              {INSTITUTIONS.map(i => <option key={i.value} value={i.value}>{i.label}</option>)}
            </select>
          </div>
          <div className="flex items-end">
            <button
              onClick={() => fileRef.current?.click()}
              disabled={loading}
              className="btn-primary w-full justify-center"
            >
              {loading ? <Loader2 size={15} className="animate-spin" /> : <Upload size={15} />}
              {loading ? 'Processando…' : fileName ? 'Trocar arquivo' : 'Selecionar arquivo'}
            </button>
            <input ref={fileRef} type="file" accept=".csv,.ofx,.qfx" className="hidden"
              onChange={e => { if (e.target.files?.[0]) handleFile(e.target.files[0]) }} />
          </div>
        </div>
        {fileName && (
          <div className="flex items-center gap-2 text-xs text-slate-400 bg-bg-tertiary/40 rounded px-3 py-2">
            <FileText size={14} />
            <span>{fileName}</span>
          </div>
        )}
      </div>

      {/* Result summary */}
      {result && (
        <div className="card border-green-600/40">
          <div className="flex flex-wrap items-center gap-4 text-sm">
            <span className="flex items-center gap-1.5 text-green-400"><CheckCircle2 size={15} /> {result.imported} importadas</span>
            {result.duplicates > 0 && <span className="flex items-center gap-1.5 text-amber-400"><XCircle size={15} /> {result.duplicates} duplicadas ignoradas</span>}
            {result.failed > 0 && <span className="flex items-center gap-1.5 text-red-400"><XCircle size={15} /> {result.failed} com erro</span>}
          </div>
        </div>
      )}

      {/* Preview */}
      {preview && (
        <div className="card space-y-4">
          <div className="flex flex-wrap items-center gap-3">
            <h2 className="text-base font-semibold text-slate-200">Pré-visualização</h2>
            <div className="flex flex-wrap gap-2 text-xs">
              <span className="px-2 py-1 rounded bg-bg-tertiary text-slate-300">{preview.totalFound} encontradas</span>
              <span className="px-2 py-1 rounded bg-green-500/10 text-green-400">{preview.newCount} novas</span>
              <span className="px-2 py-1 rounded bg-amber-500/10 text-amber-400">{preview.duplicateCount} duplicadas</span>
              <span className="px-2 py-1 rounded bg-blue-500/10 text-blue-400">{preview.categorizedCount} categorizadas</span>
              <span className="px-2 py-1 rounded bg-red-500/10 text-red-400">{preview.needsReviewCount} revisar</span>
            </div>
          </div>

          <div className="overflow-x-auto rounded-lg border border-slate-700/40 max-h-[420px] overflow-y-auto">
            <table className="w-full text-sm min-w-[820px]">
              <thead className="bg-bg-tertiary/60 sticky top-0">
                <tr>
                  <th className="p-2 w-8">
                    <button onClick={toggleAll} className="text-slate-400 hover:text-slate-100">
                      {allSelected ? <CheckCircle2 size={15} /> : <XCircle size={15} />}
                    </button>
                  </th>
                  <th className="table-header text-left p-2">Data</th>
                  <th className="table-header text-left p-2">Descrição</th>
                  <th className="table-header text-left p-2">Categoria</th>
                  <th className="table-header text-left p-2">Tipo</th>
                  <th className="table-header text-right p-2">Valor</th>
                  <th className="w-10" />
                </tr>
              </thead>
              <tbody>
                {rows.map((row, i) => (
                  <tr key={i} className={`border-t border-slate-700/20 ${row.item.isDuplicate ? 'opacity-45 bg-amber-500/5' : !row.selected ? 'opacity-40' : ''}`}>
                    <td className="p-2">
                      <input type="checkbox" className="accent-accent" checked={row.selected}
                        onChange={() => toggleRow(i)} disabled={row.item.isDuplicate} />
                    </td>
                    <td className="p-2">
                      <input type="date" className="input text-xs py-1 w-32" value={row.item.transactionDate.substring(0, 10)}
                        onChange={e => updateRow(i, { item: { ...row.item, transactionDate: e.target.value } })} />
                    </td>
                    <td className="p-2">
                      <input type="text" className="input text-xs py-1 w-full min-w-[150px]" value={row.item.description}
                        onChange={e => updateRow(i, { item: { ...row.item, description: e.target.value } })} />
                    </td>
                    <td className="p-2">
                      <div className="flex items-center gap-1">
                        <select className="input text-xs py-1 w-full min-w-[130px]" value={row.categoryId}
                          onChange={e => updateRow(i, { categoryId: Number(e.target.value), edited: true, ruleCreated: false })}>
                          <option value={0}>— Selecionar —</option>
                          {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                        </select>
                        {row.edited && row.categoryId !== 0 && !row.ruleCreated && (
                          <button title="Criar regra para futuras transações semelhantes"
                            onClick={() => handleCreateRule(row, i)}
                            className="p-1 text-accent hover:bg-accent/10 rounded shrink-0">
                            <Plus size={13} />
                          </button>
                        )}
                      </div>
                    </td>
                    <td className="p-2">
                      <select className="input text-xs py-1 w-full min-w-[90px]" value={row.item.type}
                        onChange={e => updateRow(i, { item: { ...row.item, type: Number(e.target.value) as TransactionType } })}>
                        <option value={1}>Despesa</option>
                        <option value={0}>Receita</option>
                        <option value={2}>Transferência</option>
                      </select>
                    </td>
                    <td className="p-2 text-right">
                      <input type="number" step="0.01" className="input text-xs py-1 w-24 text-right" value={row.item.amount}
                        onChange={e => updateRow(i, { item: { ...row.item, amount: parseFloat(e.target.value) || 0 } })} />
                    </td>
                    <td className="p-2 text-right">
                      {row.item.isDuplicate
                        ? <span title="Já importada" className="text-amber-400"><XCircle size={14} /></span>
                        : row.item.isCategorized
                          ? <span title="Categorizada automaticamente" className="text-green-400"><CheckCircle2 size={14} /></span>
                          : <span title="Precisa de revisão" className="text-red-400"><Filter size={14} /></span>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between gap-4">
            <span className="text-sm text-slate-400">
              {selectedCount} selecionada{selectedCount !== 1 ? 's' : ''} ·{' '}
              <span className="text-slate-200 font-medium">{fmt(selectedTotal)}</span>
            </span>
            <div className="flex gap-2">
              <button onClick={() => { setPreview(null); setRows([]); setFileName('') }} className="btn-ghost">Cancelar</button>
              <button onClick={handleConfirm} disabled={confirming || selectedCount === 0} className="btn-primary">
                {confirming && <Loader2 size={14} className="animate-spin" />}
                Confirmar ({selectedCount})
              </button>
            </div>
          </div>
        </div>
      )}

      {/* History */}
      <div className="card">
        <h2 className="text-base font-semibold text-slate-200 mb-4">Histórico de importações</h2>
        {loadingHistory ? (
          <div className="flex items-center justify-center py-6"><Loader2 size={18} className="animate-spin text-accent" /></div>
        ) : history.length === 0 ? (
          <p className="text-slate-500 text-sm py-4 text-center">Nenhuma importação ainda</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm min-w-[640px]">
              <thead>
                <tr className="border-b border-slate-700/50">
                  {['Arquivo', 'Conta', 'Formato', 'Quando', 'Total', 'Importadas', 'Duplicadas'].map(h => (
                    <th key={h} className="table-header text-left py-2 px-2 first:pl-0">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {history.map(h => (
                  <tr key={h.id} className="border-b border-slate-700/20">
                    <td className="py-2 px-2 pl-0 text-slate-200">{h.fileName}</td>
                    <td className="py-2 px-2 text-slate-400 text-xs">{h.financialAccountName}</td>
                    <td className="py-2 px-2 text-xs text-slate-400">{FORMAT_LABELS[h.format]}</td>
                    <td className="py-2 px-2 text-xs text-slate-400">{new Date(h.importedAt).toLocaleString('pt-BR')}</td>
                    <td className="py-2 px-2 text-slate-300">{h.totalRecords}</td>
                    <td className="py-2 px-2 text-green-400">{h.importedRecords}</td>
                    <td className="py-2 px-2 text-amber-400">{h.duplicateRecords}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}