namespace OrcamentoFamiliar.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
}