namespace OrcamentoFamiliar.Domain.Entities;

public class InstallmentGroup
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int? FinancialAccountId { get; set; }
    public FinancialAccount? FinancialAccount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal InstallmentValue { get; set; }
    public int TotalInstallments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Transaction> Transactions { get; set; } = [];
}