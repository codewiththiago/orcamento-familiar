using System.ComponentModel.DataAnnotations;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Application.DTOs.Accounts;

public class FinancialAccountDto
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public FinancialAccountType Type { get; set; }
    public string? OwnerUserId { get; set; }
    public string? OwnerUserName { get; set; }
    public decimal InitialBalance { get; set; }
    public bool Active { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateFinancialAccountDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public string? Institution { get; set; }

    public FinancialAccountType Type { get; set; } = FinancialAccountType.CheckingAccount;

    public string? OwnerUserId { get; set; }

    public decimal InitialBalance { get; set; }

    public bool Active { get; set; } = true;
}

public class UpdateFinancialAccountDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public string? Institution { get; set; }

    public FinancialAccountType Type { get; set; }

    public string? OwnerUserId { get; set; }

    public decimal InitialBalance { get; set; }

    public bool Active { get; set; } = true;
}