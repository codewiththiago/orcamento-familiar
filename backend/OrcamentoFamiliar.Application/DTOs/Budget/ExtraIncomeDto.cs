using System.ComponentModel.DataAnnotations;

namespace OrcamentoFamiliar.Application.DTOs.Budget;

public class ExtraIncomeDto
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class CreateExtraIncomeDto
{
    public int MonthlyBudgetId { get; set; }

    [Required, MinLength(1)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Value { get; set; }
}

public class UpdateExtraIncomeDto
{
    [Required, MinLength(1)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Value { get; set; }
}
