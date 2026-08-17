using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Domain.Entities;

public class Card
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public CardType CardType { get; set; } = CardType.Credit;
    public decimal? Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal? MonthlyGoal { get; set; }

    // Prepaid-only fields
    public decimal? MonthlyCredit { get; set; }
    public int? CreditSinceYear { get; set; }
    public int? CreditSinceMonth { get; set; }
    public decimal? InitialBalance { get; set; }
}