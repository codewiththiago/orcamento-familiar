using OrcamentoFamiliar.Application.DTOs.Budget;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface IBudgetService
{
    Task<DashboardDto> GetDashboardAsync(int year);
    Task<MonthlyBudgetDto> GetOrCreateMonthlyBudgetAsync(int year, int month);
    Task<MonthlyBudgetDto> UpdateSalaryAsync(int year, int month, UpdateSalaryDto dto);

    Task<ExtraIncomeDto> AddExtraIncomeAsync(CreateExtraIncomeDto dto);
    Task<ExtraIncomeDto> UpdateExtraIncomeAsync(int id, UpdateExtraIncomeDto dto);
    Task DeleteExtraIncomeAsync(int id);

    Task<FixedExpenseDto> AddFixedExpenseAsync(CreateFixedExpenseDto dto);
    Task<FixedExpenseDto> UpdateFixedExpenseAsync(int id, UpdateFixedExpenseDto dto);
    Task DeleteFixedExpenseAsync(int id);

    Task<List<CreditCardLaunchDto>> AddCreditCardLaunchAsync(CreateCreditCardLaunchDto dto);
    Task<CreditCardLaunchDto> UpdateCreditCardLaunchAsync(int id, UpdateCreditCardLaunchDto dto);
    Task DeleteCreditCardLaunchAsync(int id, bool deleteAll = false);

    Task<List<CategorySummaryDto>> GetCategorySummaryAsync(int budgetId);
}
