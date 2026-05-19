using OrcamentoFamiliar.Application.DTOs.Category;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task DeleteAsync(int id);
}
