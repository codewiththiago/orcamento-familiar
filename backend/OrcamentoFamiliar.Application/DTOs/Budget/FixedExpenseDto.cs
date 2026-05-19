using OrcamentoFamiliar.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OrcamentoFamiliar.Application.DTOs.Budget;

public class FixedExpenseDto
{
    public int Id { get; set; }
    public int MonthlyBudgetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public InstallmentType InstallmentType { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal ActualValue { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CreateFixedExpenseDto
{
    public int MonthlyBudgetId { get; set; }

    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public InstallmentType InstallmentType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PlannedValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ActualValue { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class UpdateFixedExpenseDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public InstallmentType InstallmentType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PlannedValue { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ActualValue { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
