import { api } from './client'
import type {
  MonthlyBudget, Dashboard, ExtraIncome, FixedExpense,
  CreditCardLaunch, CategorySummary, InstallmentType
} from '../types'

export async function getDashboard(year: number): Promise<Dashboard> {
  const { data } = await api.get(`/dashboard?year=${year}`)
  return data
}

export async function getMonthlyBudget(year: number, month: number): Promise<MonthlyBudget> {
  const { data } = await api.get(`/budget/${year}/${month}`)
  return data
}

export async function updateSalary(year: number, month: number, salary1: number, salary2: number) {
  const { data } = await api.put(`/budget/${year}/${month}/salary`, { salary1, salary2 })
  return data as MonthlyBudget
}

// Extra Income
export async function addExtraIncome(monthlyBudgetId: number, description: string, value: number): Promise<ExtraIncome> {
  const { data } = await api.post('/budget/extra-income', { monthlyBudgetId, description, value })
  return data
}

export async function updateExtraIncome(id: number, description: string, value: number): Promise<ExtraIncome> {
  const { data } = await api.put(`/budget/extra-income/${id}`, { description, value })
  return data
}

export async function deleteExtraIncome(id: number): Promise<void> {
  await api.delete(`/budget/extra-income/${id}`)
}

// Fixed Expense
export async function addFixedExpense(payload: {
  monthlyBudgetId: number; name: string; installmentType: InstallmentType;
  plannedValue: number; actualValue: number; categoryId: number
}): Promise<FixedExpense> {
  const { data } = await api.post('/budget/fixed-expense', payload)
  return data
}

export async function updateFixedExpense(id: number, payload: {
  name: string; installmentType: InstallmentType;
  plannedValue: number; actualValue: number; categoryId: number
}): Promise<FixedExpense> {
  const { data } = await api.put(`/budget/fixed-expense/${id}`, payload)
  return data
}

export async function deleteFixedExpense(id: number): Promise<void> {
  await api.delete(`/budget/fixed-expense/${id}`)
}

// Credit Card Launch
export async function addCreditCardLaunch(payload: {
  monthlyBudgetId: number; description: string; cardId: number; date: string;
  categoryId: number; totalInstallments: number; value: number; observation?: string
}): Promise<CreditCardLaunch[]> {
  const { data } = await api.post('/budget/launch', payload)
  return data
}

export async function updateCreditCardLaunch(id: number, payload: {
  description: string; cardId: number; date: string;
  categoryId: number; value: number; observation?: string
}): Promise<CreditCardLaunch> {
  const { data } = await api.put(`/budget/launch/${id}`, payload)
  return data
}

export async function deleteCreditCardLaunch(id: number, deleteAll = false): Promise<void> {
  await api.delete(`/budget/launch/${id}?deleteAll=${deleteAll}`)
}

export async function getCategorySummary(budgetId: number): Promise<CategorySummary[]> {
  const { data } = await api.get(`/budget/${budgetId}/category-summary`)
  return data
}

