using OrcamentoFamiliar.Application.DTOs.Transactions;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionDto>> QueryAsync(TransactionQueryDto query);
    Task<TransactionDto> GetByIdAsync(int id);
    Task<List<TransactionDto>> CreateAsync(CreateTransactionDto dto);
    Task<TransactionDto> UpdateAsync(int id, UpdateTransactionDto dto);
    Task DeleteAsync(int id, bool deleteFuture = false);
}