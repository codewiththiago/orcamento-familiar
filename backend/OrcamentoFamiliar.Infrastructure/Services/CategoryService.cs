using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Category;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;
    public CategoryService(AppDbContext context, ICurrentFamily currentFamily)
    {
        _context = context;
        _currentFamily = currentFamily;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        return await _context.Categories.AsNoTracking()
            .Where(c => c.FamilyId == familyId)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var category = new Category { FamilyId = familyId, Name = dto.Name };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return new CategoryDto { Id = category.Id, Name = category.Name };
    }

    public async Task DeleteAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.FamilyId == familyId)
            ?? throw new KeyNotFoundException();
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}