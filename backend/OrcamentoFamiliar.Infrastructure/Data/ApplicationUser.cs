using Microsoft.AspNetCore.Identity;
using OrcamentoFamiliar.Domain.Entities;

namespace OrcamentoFamiliar.Infrastructure.Data;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public Guid? FamilyId { get; set; }
    public Family? Family { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}