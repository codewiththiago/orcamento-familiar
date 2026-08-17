using System.ComponentModel.DataAnnotations;

namespace OrcamentoFamiliar.Application.DTOs.Auth;

public class ResetPasswordDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string InviteCode { get; set; } = string.Empty;

    [Required]
    public string Pin { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}