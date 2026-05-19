using OrcamentoFamiliar.Application.DTOs.Auth;

namespace OrcamentoFamiliar.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);

    Task<InviteDto> CreateInviteAsync(CreateInviteDto dto, string createdByUserId);
    Task<InviteInfoDto> ValidateInviteTokenAsync(string token);
    Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
    Task DeleteInviteAsync(int id);
    Task<List<InviteDto>> GetPendingInvitesAsync();
    Task<List<UserDto>> GetUsersAsync();
}
