using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Application.DTOs.Imports;

public class ParsedTransaction
{
    public string Description { get; set; } = string.Empty;
    public string? Memo { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public string? ExternalId { get; set; }
}

public class ParsedTransactionDto
{
    public string Description { get; set; } = string.Empty;
    public string NormalizedDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public string? ExternalId { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public bool IsCategorized { get; set; }
}

public class ImportPreviewDto
{
    public int TotalFound { get; set; }
    public int NewCount { get; set; }
    public int DuplicateCount { get; set; }
    public int CategorizedCount { get; set; }
    public int NeedsReviewCount { get; set; }
    public List<ParsedTransactionDto> Items { get; set; } = [];
}

public class ConfirmImportItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public string? ExternalId { get; set; }
    public int? CategoryId { get; set; }
}

public class ConfirmImportRequestDto
{
    public int FinancialAccountId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public ImportFormat Format { get; set; } = ImportFormat.Csv;
    public string? Institution { get; set; }
    public List<ConfirmImportItemDto> Items { get; set; } = [];
}

public class ImportResultDto
{
    public int ImportId { get; set; }
    public int Imported { get; set; }
    public int Duplicates { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }
}

public class ImportDto
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public int FinancialAccountId { get; set; }
    public string FinancialAccountName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public ImportFormat Format { get; set; }
    public DateTime ImportedAt { get; set; }
    public string? ImportedByUserName { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int DuplicateRecords { get; set; }
    public int FailedRecords { get; set; }
}