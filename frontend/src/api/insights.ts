import { api } from './client'
import type { FutureCommitment, MonthlyInsights } from '../types'

export async function getMonthlyInsights(year: number, month: number): Promise<MonthlyInsights> {
  const { data } = await api.get(`/insights/monthly/${year}/${month}`)
  return data
}

export async function getFutureCommitments(year: number, month: number, months = 6): Promise<FutureCommitment[]> {
  const { data } = await api.get(`/insights/commitments?year=${year}&month=${month}&months=${months}`)
  return data
}