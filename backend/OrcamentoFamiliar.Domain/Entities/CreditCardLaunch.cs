namespace OrcamentoFamiliar.Domain.Entities;

public class CreditCardLaunch
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public MonthlyBudget MonthlyBudget { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int CurrentInstallment { get; set; } = 1;
    public int TotalInstallments { get; set; } = 1;
    public decimal Value { get; set; }
    public string? Observation { get; set; }
    public Guid? GroupId { get; set; }
}
