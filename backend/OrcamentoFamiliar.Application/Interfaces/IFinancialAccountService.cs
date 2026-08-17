using OrcamentoFamiliar.Application.DTOs.Accounts;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface IFinancialAccountService
{
    Task<List<FinancialAccountDto>> GetAllAsync();
    Task<FinancialAccountDto> CreateAsync(CreateFinancialAccountDto dto);
    Task<FinancialAccountDto> UpdateAsync(int id, UpdateFinancialAccountDto dto);
    Task DeleteAsync(int id);
}