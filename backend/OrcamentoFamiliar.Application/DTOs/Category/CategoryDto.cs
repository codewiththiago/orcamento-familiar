using System.ComponentModel.DataAnnotations;

namespace OrcamentoFamiliar.Application.DTOs.Category;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateCategoryDto
{
    [Required, MinLength(1)]
    public string Name { get; set; } = string.Empty;
}
