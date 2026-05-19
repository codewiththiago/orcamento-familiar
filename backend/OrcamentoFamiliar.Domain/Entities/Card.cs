namespace OrcamentoFamiliar.Domain.Entities;

public class Card
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public decimal? MonthlyGoal { get; set; }
}
