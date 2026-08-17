import { api } from './client'
import type { Transaction, TransactionType } from '../types'

export interface TransactionPayload {
  financialAccountId: number
  categoryId?: number
  description: string
  amount: number
  transactionDate: string
  type: TransactionType
  totalInstallments?: number
  observation?: string
}

export async function getTransactions(params: {
  from?: string
  to?: string
  accountId?: number
  categoryId?: number
  type?: TransactionType
  limit?: number
} = {}): Promise<Transaction[]> {
  const search = new URLSearchParams()
  if (params.from) search.append('from', params.from)
  if (params.to) search.append('to', params.to)
  if (params.accountId) search.append('accountId', String(params.accountId))
  if (params.categoryId) search.append('categoryId', String(params.categoryId))
  if (params.type !== undefined) search.append('type', String(params.type))
  if (params.limit) search.append('limit', String(params.limit))
  const { data } = await api.get(`/transactions?${search}`)
  return data
}

export async function createTransaction(payload: TransactionPayload): Promise<Transaction[]> {
  const { data } = await api.post('/transactions', payload)
  return data
}

export async function updateTransaction(id: number, payload: Omit<TransactionPayload, 'financialAccountId'>): Promise<Transaction> {
  const { data } = await api.put(`/transactions/${id}`, payload)
  return data
}

export async function deleteTransaction(id: number, deleteFuture = false): Promise<void> {
  await api.delete(`/transactions/${id}?deleteFuture=${deleteFuture}`)
}