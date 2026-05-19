namespace OrcamentoFamiliar.Domain.Entities;

public class ExtraIncome
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public MonthlyBudget MonthlyBudget { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
