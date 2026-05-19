namespace OrcamentoFamiliar.Application.DTOs.Budget;

public class MonthlyBudgetDto
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Salary1 { get; set; }
    public decimal Salary2 { get; set; }
    public List<ExtraIncomeDto> ExtraIncomes { get; set; } = [];
    public List<FixedExpenseDto> FixedExpenses { get; set; } = [];
    public List<CreditCardLaunchDto> CreditCardLaunches { get; set; } = [];
}

public class UpdateSalaryDto
{
    public decimal Salary1 { get; set; }
    public decimal Salary2 { get; set; }
}
