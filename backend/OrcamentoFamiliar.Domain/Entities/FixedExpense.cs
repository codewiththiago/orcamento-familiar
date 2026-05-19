using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Domain.Entities;

public class FixedExpense
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public MonthlyBudget MonthlyBudget { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public InstallmentType InstallmentType { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal ActualValue { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
