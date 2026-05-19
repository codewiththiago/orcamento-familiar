namespace OrcamentoFamiliar.Application.DTOs.Budget;

public class XPWalletDto
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal ActualValue { get; set; }
}

public class UpdateXPWalletDto
{
    public decimal PlannedValue { get; set; }
    public decimal ActualValue { get; set; }
}
