namespace OrcamentoFamiliar.Domain.Entities;

public class MonthlyBudget
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Salary1 { get; set; }
    public decimal Salary2 { get; set; }

    public ICollection<ExtraIncome> ExtraIncomes { get; set; } = [];
    public ICollection<FixedExpense> FixedExpenses { get; set; } = [];
    public ICollection<CreditCardLaunch> CreditCardLaunches { get; set; } = [];
}