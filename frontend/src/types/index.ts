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

// ---- Financial accounts ----

export type FinancialAccountType = 0 | 1 | 2 | 3 | 4 | 5
// 0 CheckingAccount | 1 DigitalWallet | 2 CreditCard | 3 PrepaidCard | 4 Cash | 5 Other

export interface FinancialAccount {
  id: number
  familyId: string
  name: string
  institution?: string
  type: FinancialAccountType
  ownerUserId?: string
  ownerUserName?: string
  initialBalance: number
  active: boolean
  balance: number
  createdAt: string
  updatedAt: string
}

// ---- Transactions ----

export type TransactionType = 0 | 1 | 2 // 0 Income | 1 Expense | 2 Transfer
export type TransactionStatus = 0 | 1 // 0 Pending | 1 Confirmed

export interface Transaction {
  id: number
  familyId: string
  financialAccountId: number
  financialAccountName: string
  categoryId?: number
  categoryName: string
  type: TransactionType
  description: string
  normalizedDescription: string
  amount: number
  transactionDate: string
  status: TransactionStatus
  externalId?: string
  importId?: number
  transactionHash: string
  installmentGroupId?: number
  currentInstallment: number
  totalInstallments: number
  observation?: string
  createdAt: string
  updatedAt: string
}

// ---- Imports ----

export type ImportFormat = 0 | 1 | 2 // 0 Csv | 1 Ofx | 2 Pdf

export interface ImportPreviewItem {
  description: string
  normalizedDescription: string
  amount: number
  transactionDate: string
  type: TransactionType
  externalId?: string
  transactionHash: string
  categoryId?: number
  categoryName: string
  isDuplicate: boolean
  isCategorized: boolean
}

export interface ImportPreview {
  totalFound: number
  newCount: number
  duplicateCount: number
  categorizedCount: number
  needsReviewCount: number
  items: ImportPreviewItem[]
}

export interface ConfirmImportItem {
  description: string
  amount: number
  transactionDate: string
  type: TransactionType
  externalId?: string
  categoryId?: number
}

export interface ImportResult {
  importId: number
  imported: number
  duplicates: number
  failed: number
  total: number
}

export interface ImportRecord {
  id: number
  familyId: string
  financialAccountId: number
  financialAccountName: string
  fileName: string
  fileHash: string
  format: ImportFormat
  importedAt: string
  importedByUserName?: string
  totalRecords: number
  importedRecords: number
  duplicateRecords: number
  failedRecords: number
}

// ---- Categorization rules ----

export type RuleMatchType = 0 | 1 | 2 | 3 // 0 Exact | 1 Contains | 2 StartsWith | 3 Regex

export interface CategorizationRule {
  id: number
  familyId: string
  financialAccountId?: number
  financialAccountName: string
  pattern: string
  matchType: RuleMatchType
  categoryId: number
  categoryName: string
  priority: number
  active: boolean
  createdAt: string
}

// ---- Insights ----

export interface AccountSpending {
  accountId: number
  accountName: string
  accountType: FinancialAccountType
  spent: number
}

export interface MonthlyInsights {
  year: number
  month: number
  income: number
  spent: number
  committed: number
  available: number
  byAccount: AccountSpending[]
  byCategory: CategorySummary[]
  recentTransactions: Transaction[]
  topExpenses: Transaction[]
}

export interface FutureCommitment {
  year: number
  month: number
  monthName: string
  installments: number
  cardLaunches: number
  fixedExpenses: number
  total: number
}
