using System.ComponentModel.DataAnnotations;

namespace OrcamentoFamiliar.Application.DTOs.Budget;

public class CreditCardLaunchDto
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CardId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentInstallment { get; set; }
    public int TotalInstallments { get; set; }
    public decimal Value { get; set; }
    public string? Observation { get; set; }
    public Guid? GroupId { get; set; }
}

public class CreateCreditCardLaunchDto
{
    public int MonthlyBudgetId { get; set; }

    [Required, MinLength(1)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CardId { get; set; }

    public DateTime Date { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int TotalInstallments { get; set; } = 1;

    [Range(0.01, double.MaxValue)]
    public decimal Value { get; set; }

    public string? Observation { get; set; }
}

public class UpdateCreditCardLaunchDto
{
    [Required, MinLength(1)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CardId { get; set; }

    public DateTime Date { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Value { get; set; }

    public string? Observation { get; set; }
}

public class CategorySummaryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
