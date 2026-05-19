using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Category;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    public CategoryService(AppDbContext context) => _context = context;

    public async Task<List<CategoryDto>> GetAllAsync() =>
        await _context.Categories.OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = new Category { Name = dto.Name };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id) ?? throw new KeyNotFoundException();
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
