namespace OrcamentoFamiliar.Domain.Entities;

public class Import
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public int FinancialAccountId { get; set; }
    public FinancialAccount FinancialAccount { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public Enums.ImportFormat Format { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string? ImportedByUserId { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int DuplicateRecords { get; set; }
    public int FailedRecords { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
}