using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Domain.Entities;

public class CategorizationRule
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int? FinancialAccountId { get; set; }
    public FinancialAccount? FinancialAccount { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public RuleMatchType RuleMatchType { get; set; } = RuleMatchType.Contains;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int Priority { get; set; } = 100;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}