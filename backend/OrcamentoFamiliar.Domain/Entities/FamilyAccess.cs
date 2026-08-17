namespace OrcamentoFamiliar.Domain.Entities;

public class FamilyAccess
{
    public int Id { get; set; }
    public Guid FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public string InviteCode { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}