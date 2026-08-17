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
    private readonly ICurrentFamily _currentFamily;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext context,
        IConfiguration config,
        ICurrentFamily currentFamily)
    {
        _userManager = userManager;
        _context = context;
        _config = config;
        _currentFamily = currentFamily;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return null;

        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        return await BuildResponseAsync(user, refreshToken);
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

        return await BuildResponseAsync(token.User, newRefreshToken);
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
        var existing = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
        if (existing != null) return null;

        var hasFamilies = await _context.Families.AnyAsync();
        var mode = dto.FamilyMode?.Trim().ToLowerInvariant();

        Guid familyId;
        string? newFamilyCode = null;
        string? newFamilyPin = null;

        if (mode == "join" && hasFamilies)
        {
            if (string.IsNullOrEmpty(dto.InviteCode) || string.IsNullOrEmpty(dto.Pin))
                return null;

            var access = await _context.FamilyAccess
                .FirstOrDefaultAsync(a => a.InviteCode == dto.InviteCode.Trim().ToUpper());
            if (access == null || access.Pin != dto.Pin.Trim())
                return null;

            familyId = access.FamilyId;
        }
        else
        {
            // Cria uma nova família (fluxo padrão; forçado quando não há nenhuma)
            var family = new Family
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrWhiteSpace(dto.FamilyName) ? "Minha Família" : dto.FamilyName.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Families.Add(family);

            newFamilyCode = GenerateCode();
            newFamilyPin = Random.Shared.Next(1000, 10000).ToString();
            _context.FamilyAccess.Add(new FamilyAccess
            {
                FamilyId = family.Id,
                InviteCode = newFamilyCode,
                Pin = newFamilyPin,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            familyId = family.Id;
        }

        var user = new ApplicationUser
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim().ToLower(),
            UserName = dto.Email.Trim().ToLower(),
            EmailConfirmed = true,
            FamilyId = familyId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return null;

        if (newFamilyCode is not null)
        {
            var family = await _context.Families.FindAsync(familyId);
            if (family != null)
            {
                family.OwnerUserId = user.Id;
                await _context.SaveChangesAsync();
            }

            await FamilyDefaults.EnsureFamilyDefaultsAsync(_context, familyId);
        }

        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        var response = await BuildResponseAsync(user, refreshToken);
        response.FamilyCode = newFamilyCode;
        response.FamilyPin = newFamilyPin;
        return response;
    }

    public async Task<RegistrationStatusDto> GetRegistrationStatusAsync()
    {
        var hasFamilies = await _context.Families.AnyAsync();
        return new RegistrationStatusDto { HasFamilies = hasFamilies };
    }

    public async Task<FamilyCodeDto> GetFamilyCodeAsync()
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var access = await _context.FamilyAccess.FirstOrDefaultAsync(a => a.FamilyId == familyId);
        return new FamilyCodeDto
        {
            InviteCode = access?.InviteCode ?? "",
            HasCode = access != null
        };
    }

    public async Task<FamilyCodeCreatedDto> RegenerateFamilyCodeAsync()
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var existing = await _context.FamilyAccess.Where(a => a.FamilyId == familyId).ToListAsync();
        _context.FamilyAccess.RemoveRange(existing);

        var code = GenerateCode();
        var pin = Random.Shared.Next(1000, 10000).ToString();

        _context.FamilyAccess.Add(new FamilyAccess
        {
            FamilyId = familyId,
            InviteCode = code,
            Pin = pin,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new FamilyCodeCreatedDto { InviteCode = code, Pin = pin };
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        return await _userManager.Users
            .Where(u => u.FamilyId == familyId)
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

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
        if (user == null) return false;

        var access = await _context.FamilyAccess
            .FirstOrDefaultAsync(a => a.InviteCode == dto.InviteCode.Trim().ToUpper());
        if (access == null || access.Pin != dto.Pin.Trim())
            return false;

        // O código deve pertencer à família do usuário
        if (user.FamilyId.HasValue && access.FamilyId != user.FamilyId.Value)
            return false;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        return result.Succeeded;
    }

    private async Task<AuthResponseDto> BuildResponseAsync(ApplicationUser user, string refreshToken)
    {
        var familyName = "";
        if (user.FamilyId.HasValue)
        {
            var family = await _context.Families.AsNoTracking()
                .Where(f => f.Id == user.FamilyId.Value)
                .Select(f => f.Name)
                .FirstOrDefaultAsync();
            familyName = family ?? "";
        }

        return new AuthResponseDto
        {
            AccessToken = GenerateAccessToken(user),
            RefreshToken = refreshToken,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email!,
            FamilyId = user.FamilyId,
            FamilyName = familyName
        };
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

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("name", user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.FamilyId.HasValue)
            claims.Add(new Claim("familyId", user.FamilyId.Value.ToString()));

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