using OrcamentoFamiliar.Application.DTOs.Transactions;

namespace OrcamentoFamiliar.Application.DTOs.Insights;

public class MonthlyInsightsDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Spent { get; set; }
    public decimal Committed { get; set; }
    public decimal Available { get; set; }
    public List<AccountSpendingDto> ByAccount { get; set; } = [];
    public List<CategorySpendingDto> ByCategory { get; set; } = [];
    public List<TransactionDto> RecentTransactions { get; set; } = [];
    public List<TransactionDto> TopExpenses { get; set; } = [];
}

public class AccountSpendingDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public Domain.Enums.FinancialAccountType AccountType { get; set; }
    public decimal Spent { get; set; }
}

public class CategorySpendingDto
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class FutureCommitmentDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Installments { get; set; }
    public decimal CardLaunches { get; set; }
    public decimal FixedExpenses { get; set; }
    public decimal Total { get; set; }
}