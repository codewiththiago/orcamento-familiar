using System.ComponentModel.DataAnnotations;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Application.DTOs.CategorizationRules;

public class CategorizationRuleDto
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public int? FinancialAccountId { get; set; }
    public string? FinancialAccountName { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public RuleMatchType RuleMatchType { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategorizationRuleDto
{
    public int? FinancialAccountId { get; set; }

    [Required, MinLength(1)]
    public string Pattern { get; set; } = string.Empty;

    public RuleMatchType RuleMatchType { get; set; } = RuleMatchType.Contains;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public int Priority { get; set; } = 100;

    public bool Active { get; set; } = true;
}

public class UpdateCategorizationRuleDto
{
    public int? FinancialAccountId { get; set; }

    [Required, MinLength(1)]
    public string Pattern { get; set; } = string.Empty;

    public RuleMatchType RuleMatchType { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public int Priority { get; set; }

    public bool Active { get; set; } = true;
}