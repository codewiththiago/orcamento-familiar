using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Domain.Entities;

public class FinancialAccount
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public FinancialAccountType Type { get; set; } = FinancialAccountType.CheckingAccount;
    public string? OwnerUserId { get; set; }
    public decimal InitialBalance { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}