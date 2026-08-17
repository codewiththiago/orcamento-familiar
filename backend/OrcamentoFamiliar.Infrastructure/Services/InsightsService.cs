using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Insights;
using OrcamentoFamiliar.Application.DTOs.Transactions;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class InsightsService : IInsightsService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;

    private static readonly string[] MonthNames =
        ["Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
         "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];

    public InsightsService(AppDbContext context, ICurrentFamily currentFamily)
    {
        _context = context;
        _currentFamily = currentFamily;
    }

    public async Task<MonthlyInsightsDto> GetMonthlyAsync(int year, int month)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var transactions = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.FinancialAccount)
            .Include(t => t.Category)
            .Where(t => t.FamilyId == familyId
                && t.Status == TransactionStatus.Confirmed
                && t.TransactionDate >= start
                && t.TransactionDate < end)
            .ToListAsync();

        var budget = await _context.MonthlyBudgets
            .AsNoTracking()
            .Include(b => b.ExtraIncomes)
            .Include(b => b.FixedExpenses)
            .Include(b => b.CreditCardLaunches)
            .FirstOrDefaultAsync(b => b.FamilyId == familyId && b.Year == year && b.Month == month);

        var incomeTransactions = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);
        var installmentExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense && t.TotalInstallments > 1)
            .Sum(t => t.Amount);
        var regularExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense && t.TotalInstallments == 1)
            .Sum(t => t.Amount);

        var legacyIncome = budget == null ? 0m
            : budget.Salary1 + budget.Salary2 + budget.ExtraIncomes.Sum(e => e.Value);
        var legacyActual = budget == null ? 0m
            : budget.FixedExpenses.Sum(f => f.ActualValue) + budget.CreditCardLaunches.Sum(l => l.Value);
        var legacyPlannedRemaining = budget == null ? 0m
            : budget.FixedExpenses.Sum(f => f.PlannedValue - f.ActualValue);

        var income = legacyIncome + incomeTransactions;
        var spent = legacyActual + regularExpenses;
        var committed = legacyPlannedRemaining + installmentExpenses;
        var available = income - spent - committed;

        var byAccount = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.FinancialAccountId, t.FinancialAccount!.Name, t.FinancialAccount.Type })
            .Select(g => new AccountSpendingDto
            {
                AccountId = g.Key.FinancialAccountId,
                AccountName = g.Key.Name,
                AccountType = g.Key.Type,
                Spent = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Spent)
            .ToList();

        var byCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => new { t.CategoryId, Name = t.Category?.Name ?? "Sem categoria" })
            .Select(g => new CategorySpendingDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Total = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var recent = transactions
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Take(10)
            .Select(MapDto)
            .ToList();

        var topExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .OrderByDescending(t => t.Amount)
            .Take(10)
            .Select(MapDto)
            .ToList();

        return new MonthlyInsightsDto
        {
            Year = year,
            Month = month,
            Income = income,
            Spent = spent,
            Committed = committed,
            Available = available,
            ByAccount = byAccount,
            ByCategory = byCategory,
            RecentTransactions = recent,
            TopExpenses = topExpenses
        };
    }

    public async Task<List<FutureCommitmentDto>> GetCommitmentsAsync(int startYear, int startMonth, int months)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        months = Math.Clamp(months, 1, 24);

        var start = new DateTime(startYear, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(months);

        var budgets = await _context.MonthlyBudgets
            .AsNoTracking()
            .Include(b => b.FixedExpenses)
            .Include(b => b.CreditCardLaunches)
            .Where(b => b.FamilyId == familyId && b.Year >= startYear && b.Year <= end.AddMonths(-1).Year)
            .ToListAsync();

        var installmentTransactions = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.FamilyId == familyId
                && t.Status == TransactionStatus.Confirmed
                && t.TotalInstallments > 1
                && t.TransactionDate >= start
                && t.TransactionDate < end)
            .ToListAsync();

        var results = new List<FutureCommitmentDto>();

        for (var offset = 0; offset < months; offset++)
        {
            var current = start.AddMonths(offset);
            var next = current.AddMonths(1);

            var budget = budgets.FirstOrDefault(b => b.Year == current.Year && b.Month == current.Month);

            var installments = installmentTransactions
                .Where(t => t.TransactionDate >= current && t.TransactionDate < next)
                .Sum(t => t.Amount);
            var fixedExpenses = budget?.FixedExpenses.Sum(f => f.PlannedValue) ?? 0m;
            var cardLaunches = budget?.CreditCardLaunches.Sum(l => l.Value) ?? 0m;

            results.Add(new FutureCommitmentDto
            {
                Year = current.Year,
                Month = current.Month,
                MonthName = MonthNames[current.Month - 1],
                Installments = installments,
                FixedExpenses = fixedExpenses,
                CardLaunches = cardLaunches,
                Total = installments + fixedExpenses + cardLaunches
            });
        }

        return results;
    }

    private static TransactionDto MapDto(Domain.Entities.Transaction t) => new()
    {
        Id = t.Id,
        FamilyId = t.FamilyId,
        FinancialAccountId = t.FinancialAccountId,
        FinancialAccountName = t.FinancialAccount?.Name ?? "",
        CategoryId = t.CategoryId,
        CategoryName = t.Category?.Name ?? "",
        Type = t.Type,
        Description = t.Description,
        NormalizedDescription = t.NormalizedDescription,
        Amount = t.Amount,
        TransactionDate = t.TransactionDate,
        Status = t.Status,
        ExternalId = t.ExternalId,
        ImportId = t.ImportId,
        TransactionHash = t.TransactionHash,
        InstallmentGroupId = t.InstallmentGroupId,
        CurrentInstallment = t.CurrentInstallment,
        TotalInstallments = t.TotalInstallments,
        Observation = t.Observation,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}