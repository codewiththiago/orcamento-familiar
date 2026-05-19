namespace OrcamentoFamiliar.Application.DTOs.Auth;

public class CreateInviteDto
{
    public string Email { get; set; } = string.Empty;
}

public class InviteDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsUsed { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}

public class InviteInfoDto
{
    public string Email { get; set; } = string.Empty;
    public bool IsValid { get; set; }
}

public class RegisterDto
{
    public string? Token { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
