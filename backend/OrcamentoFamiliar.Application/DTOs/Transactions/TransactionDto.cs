using System.ComponentModel.DataAnnotations;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Application.DTOs.Transactions;

public class TransactionDto
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public int FinancialAccountId { get; set; }
    public string FinancialAccountName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string NormalizedDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionStatus Status { get; set; }
    public string? ExternalId { get; set; }
    public int? ImportId { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public int? InstallmentGroupId { get; set; }
    public int CurrentInstallment { get; set; }
    public int TotalInstallments { get; set; }
    public string? Observation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTransactionDto
{
    [Range(1, int.MaxValue)]
    public int FinancialAccountId { get; set; }

    public int? CategoryId { get; set; }

    [Required, MinLength(1)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Range(1, int.MaxValue)]
    public int TotalInstallments { get; set; } = 1;

    public string? Observation { get; set; }
}

public class UpdateTransactionDto
{
    public int? CategoryId { get; set; }

    [Required, MinLength(1)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public TransactionType Type { get; set; }

    public string? Observation { get; set; }
}

public class TransactionQueryDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? AccountId { get; set; }
    public int? CategoryId { get; set; }
    public TransactionType? Type { get; set; }
    public int Limit { get; set; } = 500;
}