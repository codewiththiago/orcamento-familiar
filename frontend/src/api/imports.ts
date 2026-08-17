import { api } from './client'
import type { ConfirmImportItem, ImportFormat, ImportPreview, ImportRecord, ImportResult } from '../types'

export async function previewImport(file: File, financialAccountId: number, format: ImportFormat, institution: string): Promise<ImportPreview> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('financialAccountId', String(financialAccountId))
  formData.append('format', String(format))
  if (institution) formData.append('institution', institution)

  const { data } = await api.post('/imports/preview', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 60000,
  })
  return data
}

export async function confirmImport(payload: {
  financialAccountId: number
  fileName: string
  format: ImportFormat
  institution?: string
  items: ConfirmImportItem[]
}): Promise<ImportResult> {
  const { data } = await api.post('/imports/confirm', payload)
  return data
}

export async function getImportHistory(): Promise<ImportRecord[]> {
  const { data } = await api.get('/imports')
  return data
}