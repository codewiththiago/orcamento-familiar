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
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, IWebHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result == null) return Unauthorized(new { message = "Email ou senha inválidos" });

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new { result.AccessToken, result.UserId, result.Name, result.Email, result.FamilyId, result.FamilyName });
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
        return Ok(new { result.AccessToken, result.UserId, result.Name, result.Email, result.FamilyId, result.FamilyName });
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
        if (result == null) return BadRequest(new { message = "Código ou PIN inválidos, ou e-mail já cadastrado." });

        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new
        {
            result.AccessToken, result.UserId, result.Name, result.Email,
            result.FamilyId, result.FamilyName, result.FamilyCode, result.FamilyPin
        });
    }

    [HttpGet("registration-status")]
    public async Task<IActionResult> GetRegistrationStatus()
    {
        var status = await _authService.GetRegistrationStatusAsync();
        return Ok(status);
    }

    [HttpGet("family-code")]
    [Authorize]
    public async Task<IActionResult> GetFamilyCode()
    {
        var code = await _authService.GetFamilyCodeAsync();
        return Ok(code);
    }

    [HttpPost("family-code/regenerate")]
    [Authorize]
    public async Task<IActionResult> RegenerateFamilyCode()
    {
        var result = await _authService.RegenerateFamilyCodeAsync();
        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userId == null) return Unauthorized();

        var ok = await _authService.ChangePasswordAsync(userId, dto);
        if (!ok) return BadRequest(new { message = "Senha atual incorreta." });
        return Ok(new { message = "Senha alterada com sucesso." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var ok = await _authService.ResetPasswordAsync(dto);
        if (!ok) return BadRequest(new { message = "Código ou PIN inválidos, ou e-mail não cadastrado." });
        return Ok(new { message = "Senha alterada com sucesso." });
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
        var isProd = _env.IsProduction();
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = isProd ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
