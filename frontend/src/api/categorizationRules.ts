import { api } from './client'
import type { CategorizationRule, RuleMatchType } from '../types'

export interface RulePayload {
  financialAccountId?: number
  pattern: string
  matchType: RuleMatchType
  categoryId: number
  priority: number
  active?: boolean
}

export async function getCategorizationRules(): Promise<CategorizationRule[]> {
  const { data } = await api.get('/categorization-rules')
  return data
}

export async function createCategorizationRule(payload: RulePayload): Promise<CategorizationRule> {
  const { data } = await api.post('/categorization-rules', payload)
  return data
}

export async function updateCategorizationRule(id: number, payload: RulePayload): Promise<CategorizationRule> {
  const { data } = await api.put(`/categorization-rules/${id}`, payload)
  return data
}

export async function deleteCategorizationRule(id: number): Promise<void> {
  await api.delete(`/categorization-rules/${id}`)
}