import { useState, useRef } from 'react'
import { X, Upload, FileText, CheckSquare, Square, Loader2 } from 'lucide-react'
import toast from 'react-hot-toast'
import type { Card, Category } from '../../types'

interface ParsedRow {
  id: string
  rawDate: string
  isoDate: string
  description: string
  value: number
  categoryId: number
  selected: boolean
}

interface Props {
  budgetId: number
  cards: Card[]
  categories: Category[]
  defaultCardId?: number
  onClose: () => void
  onImport: (launches: {
    monthlyBudgetId: number
    description: string
    cardId: number
    date: string
    categoryId: number
    totalInstallments: number
    value: number
    observation?: string
  }[]) => Promise<void>
}

const MONTH_MAP: Record<string, string> = {
  jan: '01', fev: '02', mar: '03', abr: '04', mai: '05', jun: '06',
  jul: '07', ago: '08', set: '09', out: '10', nov: '11', dez: '12',
}

function parseBrValue(raw: string): number {
  return parseFloat(raw.replace(/\./g, '').replace(',', '.'))
}

function parseDate(raw: string, referenceYear: number): string {
  // DD/MM/YYYY or DD/MM/YY or DD/MM
  const slashMatch = raw.match(/^(\d{1,2})\/(\d{1,2})(?:\/(\d{2,4}))?$/)
  if (slashMatch) {
    const d = slashMatch[1].padStart(2, '0')
    const m = slashMatch[2].padStart(2, '0')
    const y = slashMatch[3]
      ? slashMatch[3].length === 2 ? `20${slashMatch[3]}` : slashMatch[3]
      : String(referenceYear)
    return `${y}-${m}-${d}`
  }
  // DD MMM or DD MMM YYYY
  const monthMatch = raw.match(/^(\d{1,2})\s+([a-zA-Z]{3})(?:\s+(\d{4}))?$/)
  if (monthMatch) {
    const d = monthMatch[1].padStart(2, '0')
    const m = MONTH_MAP[monthMatch[2].toLowerCase()] ?? '01'
    const y = monthMatch[3] ?? String(referenceYear)
    return `${y}-${m}-${d}`
  }
  return `${referenceYear}-01-01`
}

function extractTransactions(text: string, referenceYear: number): ParsedRow[] {
  const lines = text.split('\n').map(l => l.trim()).filter(Boolean)
  const results: ParsedRow[] = []
  const seen = new Set<string>()

  // Patterns to try (most specific first):
  // 1. "DD/MM/YYYY ... VALUE" or "DD/MM ... VALUE" — common in C6, PicPay, VR
  // 2. "DD MMM ... VALUE" — common in Nubank
  const patterns = [
    /^(\d{1,2}\/\d{1,2}(?:\/\d{2,4})?)\s+(.+?)\s+([\d]{1,3}(?:\.\d{3})*,\d{2})\s*$/,
    /^(\d{1,2}\s+(?:jan|fev|mar|abr|mai|jun|jul|ago|set|out|nov|dez)(?:\s+\d{4})?)\s+(.+?)\s+([\d]{1,3}(?:\.\d{3})*,\d{2})\s*$/i,
  ]

  for (const line of lines) {
    for (const pattern of patterns) {
      const m = line.match(pattern)
      if (m) {
        const rawDate = m[1].trim()
        const description = m[2].trim()
        const rawValue = m[3]
        const value = parseBrValue(rawValue)

        if (value <= 0 || value > 100000) break
        // skip duplicates (same date+desc+value)
        const key = `${rawDate}|${description}|${rawValue}`
        if (seen.has(key)) break
        seen.add(key)

        results.push({
          id: crypto.randomUUID(),
          rawDate,
          isoDate: parseDate(rawDate, referenceYear),
          description,
          value,
          categoryId: 0,
          selected: true,
        })
        break
      }
    }
  }

  return results
}

async function parsePdf(file: File, referenceYear: number): Promise<ParsedRow[]> {
  const pdfjsLib = await import('pdfjs-dist')
  pdfjsLib.GlobalWorkerOptions.workerSrc = new URL(
    'pdfjs-dist/build/pdf.worker.min.mjs',
    import.meta.url,
  ).href

  const arrayBuffer = await file.arrayBuffer()
  const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise
  let fullText = ''

  for (let i = 1; i <= pdf.numPages; i++) {
    const page = await pdf.getPage(i)
    const content = await page.getTextContent()
    const pageText = content.items
      .map((item: any) => item.str)
      .join(' ')
    fullText += pageText + '\n'
  }

  // Reconstruct lines: split on sequences that look like date starts
  const reconstructed = fullText
    .replace(/(\d{1,2}\/\d{1,2})/g, '\n$1')
    .replace(/(\d{1,2}\s+(?:jan|fev|mar|abr|mai|jun|jul|ago|set|out|nov|dez))/gi, '\n$1')

  return extractTransactions(reconstructed, referenceYear)
}

export default function PdfImportModal({ budgetId, cards, categories, defaultCardId, onClose, onImport }: Props) {
  const fileRef = useRef<HTMLInputElement>(null)
  const [cardId, setCardId] = useState<number>(defaultCardId ?? (cards[0]?.id ?? 0))
  const [rows, setRows] = useState<ParsedRow[]>([])
  const [loading, setLoading] = useState(false)
  const [importing, setImporting] = useState(false)
  const [fileName, setFileName] = useState('')
  const [refYear, setRefYear] = useState(new Date().getFullYear())

  async function handleFile(file: File) {
    if (!file.name.toLowerCase().endsWith('.pdf')) {
      toast.error('Selecione um arquivo PDF')
      return
    }
    setFileName(file.name)
    setLoading(true)
    try {
      const parsed = await parsePdf(file, refYear)
      if (parsed.length === 0) {
        toast.error('Nenhuma transação encontrada no PDF. Verifique o formato.')
      } else {
        toast.success(`${parsed.length} transações encontradas`)
      }
      setRows(parsed)
    } catch {
      toast.error('Erro ao processar o PDF')
    } finally {
      setLoading(false)
    }
  }

  function toggleAll() {
    const allSelected = rows.every(r => r.selected)
    setRows(rows.map(r => ({ ...r, selected: !allSelected })))
  }

  function toggleRow(id: string) {
    setRows(rows.map(r => r.id === id ? { ...r, selected: !r.selected } : r))
  }

  function updateRow(id: string, field: keyof ParsedRow, value: any) {
    setRows(rows.map(r => r.id === id ? { ...r, [field]: value } : r))
  }

  async function handleImport() {
    const selected = rows.filter(r => r.selected)
    if (selected.length === 0) { toast.error('Selecione ao menos uma transação'); return }
    if (!cardId) { toast.error('Selecione o cartão'); return }
    const missing = selected.filter(r => !r.categoryId)
    if (missing.length > 0) { toast.error('Atribua uma categoria a todas as transações selecionadas'); return }

    setImporting(true)
    try {
      await onImport(selected.map(r => ({
        monthlyBudgetId: budgetId,
        description: r.description,
        cardId,
        date: r.isoDate,
        categoryId: r.categoryId,
        totalInstallments: 1,
        value: r.value,
      })))
      toast.success(`${selected.length} lançamentos importados`)
      onClose()
    } catch {
      toast.error('Erro ao importar lançamentos')
    } finally {
      setImporting(false)
    }
  }

  const selectedCount = rows.filter(r => r.selected).length
  const totalSelected = rows.filter(r => r.selected).reduce((s, r) => s + r.value, 0)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
      <div className="bg-bg-secondary rounded-xl shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-slate-700/50">
          <h2 className="text-lg font-semibold text-slate-100">Importar Fatura PDF</h2>
          <button onClick={onClose} className="p-1 text-slate-400 hover:text-slate-100"><X size={18} /></button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {/* Controls row */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label className="label">Cartão</label>
              <select className="input" value={cardId} onChange={e => setCardId(Number(e.target.value))}>
                {cards.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Ano de referência</label>
              <input type="number" className="input" value={refYear}
                onChange={e => setRefYear(Number(e.target.value))} min={2020} max={2099} />
            </div>
            <div className="flex items-end">
              <button
                onClick={() => fileRef.current?.click()}
                disabled={loading}
                className="btn-primary w-full flex items-center justify-center gap-2"
              >
                {loading ? <Loader2 size={15} className="animate-spin" /> : <Upload size={15} />}
                {loading ? 'Processando…' : fileName ? 'Trocar arquivo' : 'Selecionar PDF'}
              </button>
              <input ref={fileRef} type="file" accept=".pdf" className="hidden"
                onChange={e => { if (e.target.files?.[0]) handleFile(e.target.files[0]) }} />
            </div>
          </div>

          {fileName && !loading && (
            <div className="flex items-center gap-2 text-xs text-slate-400 bg-bg-tertiary/40 rounded px-3 py-2">
              <FileText size={14} />
              <span>{fileName}</span>
              {rows.length > 0 && <span className="ml-auto">{rows.length} transações encontradas</span>}
            </div>
          )}

          {/* Transactions table */}
          {rows.length > 0 && (
            <div className="overflow-x-auto rounded-lg border border-slate-700/40">
              <table className="w-full text-sm min-w-[640px]">
                <thead className="bg-bg-tertiary/60">
                  <tr>
                    <th className="p-2 w-8">
                      <button onClick={toggleAll} className="text-slate-400 hover:text-slate-100">
                        {rows.every(r => r.selected) ? <CheckSquare size={15} /> : <Square size={15} />}
                      </button>
                    </th>
                    <th className="table-header text-left p-2">Data</th>
                    <th className="table-header text-left p-2">Descrição</th>
                    <th className="table-header text-left p-2">Categoria</th>
                    <th className="table-header text-right p-2">Valor</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map(row => (
                    <tr key={row.id} className={`border-t border-slate-700/20 ${!row.selected ? 'opacity-40' : ''}`}>
                      <td className="p-2">
                        <button onClick={() => toggleRow(row.id)} className="text-slate-400 hover:text-slate-100">
                          {row.selected ? <CheckSquare size={15} className="text-accent" /> : <Square size={15} />}
                        </button>
                      </td>
                      <td className="p-2">
                        <input
                          type="date"
                          className="input text-xs py-1 w-32"
                          value={row.isoDate}
                          onChange={e => updateRow(row.id, 'isoDate', e.target.value)}
                        />
                      </td>
                      <td className="p-2">
                        <input
                          type="text"
                          className="input text-xs py-1 w-full min-w-[160px]"
                          value={row.description}
                          onChange={e => updateRow(row.id, 'description', e.target.value)}
                        />
                      </td>
                      <td className="p-2">
                        <select
                          className="input text-xs py-1 w-full min-w-[140px]"
                          value={row.categoryId}
                          onChange={e => updateRow(row.id, 'categoryId', Number(e.target.value))}
                        >
                          <option value={0}>— Selecionar —</option>
                          {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                        </select>
                      </td>
                      <td className="p-2 text-right">
                        <input
                          type="number"
                          className="input text-xs py-1 w-24 text-right"
                          value={row.value}
                          step="0.01"
                          onChange={e => updateRow(row.id, 'value', parseFloat(e.target.value) || 0)}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Footer */}
        {rows.length > 0 && (
          <div className="border-t border-slate-700/50 p-4 flex items-center justify-between gap-4">
            <span className="text-sm text-slate-400">
              {selectedCount} selecionada{selectedCount !== 1 ? 's' : ''} ·{' '}
              <span className="text-slate-200 font-medium">
                R$ {totalSelected.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}
              </span>
            </span>
            <div className="flex gap-2">
              <button onClick={onClose} className="btn-ghost">Cancelar</button>
              <button onClick={handleImport} disabled={importing || selectedCount === 0} className="btn-primary">
                {importing ? <Loader2 size={14} className="animate-spin" /> : null}
                Importar {selectedCount > 0 ? `(${selectedCount})` : ''}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
