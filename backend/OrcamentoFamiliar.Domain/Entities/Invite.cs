namespace OrcamentoFamiliar.Domain.Entities;

public class Invite
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUsed { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
}
