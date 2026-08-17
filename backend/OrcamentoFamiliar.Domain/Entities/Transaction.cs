namespace OrcamentoFamiliar.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int FinancialAccountId { get; set; }
    public FinancialAccount FinancialAccount { get; set; } = null!;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Enums.TransactionType Type { get; set; } = Enums.TransactionType.Expense;
    public string Description { get; set; } = string.Empty;
    public string NormalizedDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public Enums.TransactionStatus Status { get; set; } = Enums.TransactionStatus.Confirmed;
    public string? ExternalId { get; set; }
    public int? ImportId { get; set; }
    public Import? Import { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public int? InstallmentGroupId { get; set; }
    public InstallmentGroup? InstallmentGroup { get; set; }
    public int CurrentInstallment { get; set; } = 1;
    public int TotalInstallments { get; set; } = 1;
    public string? Observation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}