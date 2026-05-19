namespace OrcamentoFamiliar.Domain.Entities;

public class XPWallet
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public MonthlyBudget MonthlyBudget { get; set; } = null!;
    public decimal PlannedValue { get; set; }
    public decimal ActualValue { get; set; }
}
