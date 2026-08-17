using OrcamentoFamiliar.Application.DTOs.CategorizationRules;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface ICategorizationService
{
    Task<List<CategorizationRuleDto>> GetRulesAsync();
    Task<CategorizationRuleDto> CreateRuleAsync(CreateCategorizationRuleDto dto);
    Task<CategorizationRuleDto> UpdateRuleAsync(int id, UpdateCategorizationRuleDto dto);
    Task DeleteRuleAsync(int id);

    Task<int?> CategorizeAsync(string normalizedDescription, int financialAccountId);
    Task<Dictionary<string, int?>> CategorizeBulkAsync(IReadOnlyCollection<(string NormalizedDescription, int FinancialAccountId)> items);
}