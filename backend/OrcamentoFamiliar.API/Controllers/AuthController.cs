using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrcamentoFamiliar.Application.DTOs.Auth;
using OrcamentoFamiliar.Application.Interfaces;

namespace OrcamentoFamiliar.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result == null) return Unauthorized(new { message = "Email ou senha inválidos" });

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new
        {
            result.AccessToken,
            result.UserId,
            result.Name,
            result.Email
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token não encontrado" });

        var result = await _authService.RefreshTokenAsync(refreshToken);
        if (result == null) return Unauthorized(new { message = "Refresh token inválido ou expirado" });

        SetRefreshTokenCookie(result.RefreshToken);

        return Ok(new
        {
            result.AccessToken,
            result.UserId,
            result.Name,
            result.Email
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
            await _authService.RevokeRefreshTokenAsync(refreshToken);

        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Logout realizado com sucesso" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (result == null) return BadRequest(new { message = "Dados inválidos, convite expirado ou e-mail já cadastrado." });

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new { result.AccessToken, result.UserId, result.Name, result.Email });
    }

    [HttpPost("invite")]
    [Authorize]
    public async Task<IActionResult> CreateInvite([FromBody] CreateInviteDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")!;
        var invite = await _authService.CreateInviteAsync(dto, userId);
        return Ok(invite);
    }

    [HttpGet("invite/{token}")]
    public async Task<IActionResult> ValidateInvite(string token)
    {
        var info = await _authService.ValidateInviteTokenAsync(token);
        return Ok(info);
    }

    [HttpDelete("invite/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteInvite(int id)
    {
        await _authService.DeleteInviteAsync(id);
        return NoContent();
    }

    [HttpGet("invites")]
    [Authorize]
    public async Task<IActionResult> GetInvites()
    {
        var invites = await _authService.GetPendingInvitesAsync();
        return Ok(invites);
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _authService.GetUsersAsync();
        return Ok(users);
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // set to true in production with HTTPS
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
