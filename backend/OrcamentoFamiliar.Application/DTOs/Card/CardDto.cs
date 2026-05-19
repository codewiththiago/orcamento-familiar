using System.ComponentModel.DataAnnotations;

namespace OrcamentoFamiliar.Application.DTOs.Card;

public class CardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal? MonthlyGoal { get; set; }
    public decimal CurrentMonthUsage { get; set; }
}

public class CreateCardDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public decimal? Limit { get; set; }

    [Range(1, 31)]
    public int ClosingDay { get; set; }

    [Range(1, 31)]
    public int DueDay { get; set; }

    public decimal? MonthlyGoal { get; set; }
}

public class UpdateCardDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public decimal? Limit { get; set; }

    [Range(1, 31)]
    public int ClosingDay { get; set; }

    [Range(1, 31)]
    public int DueDay { get; set; }

    public decimal? MonthlyGoal { get; set; }
}
