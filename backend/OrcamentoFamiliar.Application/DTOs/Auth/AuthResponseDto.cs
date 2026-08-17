namespace OrcamentoFamiliar.Application.DTOs.Auth;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? FamilyId { get; set; }
    public string FamilyName { get; set; } = string.Empty;

    /// Preenchidos apenas ao criar uma nova família durante o cadastro.
    public string? FamilyCode { get; set; }
    public string? FamilyPin { get; set; }
}