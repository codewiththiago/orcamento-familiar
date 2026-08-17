using OrcamentoFamiliar.Application.DTOs.Insights;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface IInsightsService
{
    Task<MonthlyInsightsDto> GetMonthlyAsync(int year, int month);
    Task<List<FutureCommitmentDto>> GetCommitmentsAsync(int startYear, int startMonth, int months);
}