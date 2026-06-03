export interface User {
  userId: string
  name: string
  email: string
  accessToken: string
}

export interface MonthSummary {
  month: number
  monthName: string
  totalIncome: number
  plannedExpense: number
  actualExpense: number
  plannedBalance: number
  actualBalance: number
  hasData: boolean
}

export interface Dashboard {
  year: number
  months: MonthSummary[]
}

export interface ExtraIncome {
  id: number
  monthlyBudgetId: number
  description: string
  value: number
}

export type InstallmentType = 0 | 1 // 0 = Monthly, 1 = Installment

export interface FixedExpense {
  id: number
  monthlyBudgetId: number
  name: string
  installmentType: InstallmentType
  plannedValue: number
  actualValue: number
  categoryId: number
  categoryName: string
}

export interface CreditCardLaunch {
  id: number
  monthlyBudgetId: number
  description: string
  cardId: number
  cardName: string
  date: string
  categoryId: number
  categoryName: string
  currentInstallment: number
  totalInstallments: number
  value: number
  observation?: string
  groupId?: string
}

export interface MonthlyBudget {
  id: number
  year: number
  month: number
  salary1: number
  salary2: number
  extraIncomes: ExtraIncome[]
  fixedExpenses: FixedExpense[]
  creditCardLaunches: CreditCardLaunch[]
}

export interface Card {
  id: number
  name: string
  cardType: 0 | 1  // 0 = credit, 1 = prepaid
  limit?: number
  closingDay: number
  dueDay: number
  monthlyGoal?: number
  currentMonthUsage: number
  monthlyCredit?: number
  creditSinceYear?: number
  creditSinceMonth?: number
  initialBalance?: number
  currentBalance?: number
}

export interface Category {
  id: number
  name: string
}

export interface UserInfo {
  id: string
  name: string
  email: string
}

export interface CategorySummary {
  categoryId: number
  categoryName: string
  total: number
}
