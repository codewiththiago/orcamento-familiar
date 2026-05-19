import { api } from './client'
import type { Card } from '../types'

export async function getCards(year?: number, month?: number): Promise<Card[]> {
  const params = new URLSearchParams()
  if (year) params.append('year', String(year))
  if (month) params.append('month', String(month))
  const { data } = await api.get(`/cards?${params}`)
  return data
}

export async function createCard(payload: {
  name: string; limit?: number; closingDay: number; dueDay: number; monthlyGoal?: number
}): Promise<Card> {
  const { data } = await api.post('/cards', payload)
  return data
}

export async function updateCard(id: number, payload: {
  name: string; limit?: number; closingDay: number; dueDay: number; monthlyGoal?: number
}): Promise<Card> {
  const { data } = await api.put(`/cards/${id}`, payload)
  return data
}

export async function deleteCard(id: number): Promise<void> {
  await api.delete(`/cards/${id}`)
}
