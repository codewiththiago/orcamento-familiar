using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OrcamentoFamiliar.Application.DTOs.Auth;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(UserManager<ApplicationUser> userManager, AppDbContext context, IConfiguration config)
    {
        _userManager = userManager;
        _context = context;
        _config = config;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return null;

        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            AccessToken = GenerateAccessToken(user),
            RefreshToken = refreshToken,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email!
        };
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow);

        if (token == null) return null;

        token.IsRevoked = true;
        var newRefreshToken = await CreateRefreshTokenAsync(token.UserId);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = GenerateAccessToken(token.User),
            RefreshToken = newRefreshToken,
            UserId = token.User.Id,
            Name = token.User.Name,
            Email = token.User.Email!
        };
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
        if (token == null) return;
        token.IsRevoked = true;
        await _context.SaveChangesAsync();
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        var userCount = await _userManager.Users.CountAsync();
        var isFirstUser = userCount == 0;

        if (!isFirstUser)
        {
            if (string.IsNullOrEmpty(dto.InviteCode) || string.IsNullOrEmpty(dto.Pin))
                return null;

            var access = await _context.FamilyAccess.FirstOrDefaultAsync();
            if (access == null) return null;

            var codeMatch = string.Equals(access.InviteCode, dto.InviteCode.Trim().ToUpper(), StringComparison.Ordinal);
            var pinMatch = access.Pin == dto.Pin.Trim();
            if (!codeMatch || !pinMatch) return null;
        }

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null) return null;

        var user = new ApplicationUser
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim().ToLower(),
            UserName = dto.Email.Trim().ToLower(),
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return null;

        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        return new AuthResponseDto
        {
            AccessToken = GenerateAccessToken(user),
            RefreshToken = refreshToken,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email!
        };
    }

    public async Task<RegistrationStatusDto> GetRegistrationStatusAsync()
    {
        var userCount = await _userManager.Users.CountAsync();
        return new RegistrationStatusDto { RequiresCode = userCount > 0 };
    }

    public async Task<FamilyCodeDto> GetFamilyCodeAsync()
    {
        var access = await _context.FamilyAccess.FirstOrDefaultAsync();
        return new FamilyCodeDto
        {
            InviteCode = access?.InviteCode ?? "",
            HasCode = access != null
        };
    }

    public async Task<FamilyCodeCreatedDto> RegenerateFamilyCodeAsync()
    {
        var existing = await _context.FamilyAccess.ToListAsync();
        _context.FamilyAccess.RemoveRange(existing);

        var code = GenerateCode();
        var pin = Random.Shared.Next(1000, 10000).ToString();

        _context.FamilyAccess.Add(new FamilyAccess
        {
            InviteCode = code,
            Pin = pin,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new FamilyCodeCreatedDto { InviteCode = code, Pin = pin };
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        return await _userManager.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name, Email = u.Email! })
            .ToListAsync();
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        return result.Succeeded;
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }

    private string GenerateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("name", user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(string userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await _context.SaveChangesAsync();
        return token;
    }
}
