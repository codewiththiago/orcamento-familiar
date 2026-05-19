using OrcamentoFamiliar.Application.DTOs.Card;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface ICardService
{
    Task<List<CardDto>> GetAllAsync(int? year = null, int? month = null);
    Task<CardDto> GetByIdAsync(int id);
    Task<CardDto> CreateAsync(CreateCardDto dto);
    Task<CardDto> UpdateAsync(int id, UpdateCardDto dto);
    Task DeleteAsync(int id);
}
