namespace OrcamentoFamiliar.Domain.Entities;

public class MonthlyBudget
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal SalaryThiago { get; set; }
    public decimal SalaryJuh { get; set; }

    public ICollection<ExtraIncome> ExtraIncomes { get; set; } = [];
    public ICollection<FixedExpense> FixedExpenses { get; set; } = [];
    public ICollection<CreditCardLaunch> CreditCardLaunches { get; set; } = [];
}
