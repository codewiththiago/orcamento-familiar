using System.ComponentModel.DataAnnotations;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Application.DTOs.Card;

public class CardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CardType CardType { get; set; }
    public decimal? Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal? MonthlyGoal { get; set; }
    public decimal CurrentMonthUsage { get; set; }
    public decimal? MonthlyCredit { get; set; }
    public int? CreditSinceYear { get; set; }
    public int? CreditSinceMonth { get; set; }
    public decimal? InitialBalance { get; set; }
    public decimal? CurrentBalance { get; set; }
}

public class CreateCardDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public CardType CardType { get; set; } = CardType.Credit;

    public decimal? Limit { get; set; }

    public int ClosingDay { get; set; }

    public int DueDay { get; set; }

    public decimal? MonthlyGoal { get; set; }

    public decimal? MonthlyCredit { get; set; }
    public int? CreditSinceYear { get; set; }
    public int? CreditSinceMonth { get; set; }
    public decimal? InitialBalance { get; set; }
}

public class UpdateCardDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;

    public CardType CardType { get; set; } = CardType.Credit;

    public decimal? Limit { get; set; }

    public int ClosingDay { get; set; }

    public int DueDay { get; set; }

    public decimal? MonthlyGoal { get; set; }

    public decimal? MonthlyCredit { get; set; }
    public int? CreditSinceYear { get; set; }
    public int? CreditSinceMonth { get; set; }
    public decimal? InitialBalance { get; set; }
}
