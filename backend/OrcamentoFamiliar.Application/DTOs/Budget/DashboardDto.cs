namespace OrcamentoFamiliar.Application.DTOs.Budget;

public class DashboardDto
{
    public int Year { get; set; }
    public List<MonthSummaryDto> Months { get; set; } = [];
}

public class MonthSummaryDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal PlannedExpense { get; set; }
    public decimal ActualExpense { get; set; }
    public decimal PlannedBalance { get; set; }
    public decimal ActualBalance { get; set; }
    public bool HasData { get; set; }
}
