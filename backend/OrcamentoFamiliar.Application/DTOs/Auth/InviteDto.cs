namespace OrcamentoFamiliar.Application.DTOs.Auth;

public class RegisterDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// "create" cria uma nova família; "join" entra numa existente via código+PIN.
    public string? FamilyMode { get; set; }
    public string? FamilyName { get; set; }
    public string? InviteCode { get; set; }
    public string? Pin { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class FamilyCodeDto
{
    public string InviteCode { get; set; } = string.Empty;
    public bool HasCode { get; set; }
}

public class FamilyCodeCreatedDto
{
    public string InviteCode { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}

public class RegistrationStatusDto
{
    public bool HasFamilies { get; set; }
}