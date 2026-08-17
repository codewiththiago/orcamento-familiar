import { api } from './client'
import type { FinancialAccount, FinancialAccountType } from '../types'

export interface AccountPayload {
  name: string
  institution?: string
  type: FinancialAccountType
  initialBalance: number
  active?: boolean
}

export async function getAccounts(): Promise<FinancialAccount[]> {
  const { data } = await api.get('/accounts')
  return data
}

export async function createAccount(payload: AccountPayload): Promise<FinancialAccount> {
  const { data } = await api.post('/accounts', payload)
  return data
}

export async function updateAccount(id: number, payload: AccountPayload): Promise<FinancialAccount> {
  const { data } = await api.put(`/accounts/${id}`, payload)
  return data
}

export async function deleteAccount(id: number): Promise<void> {
  await api.delete(`/accounts/${id}`)
}