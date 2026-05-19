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

    public async Task<InviteDto> CreateInviteAsync(CreateInviteDto dto, string createdByUserId)
    {
        var existing = await _context.Invites
            .FirstOrDefaultAsync(x => x.Email == dto.Email && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow);
        if (existing != null)
        {
            var creatorExisting = await _userManager.FindByIdAsync(existing.CreatedByUserId);
            return MapInviteDto(existing, creatorExisting?.Name ?? "");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower();
        var invite = new Invite
        {
            Email = dto.Email.Trim().ToLower(),
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            CreatedByUserId = createdByUserId
        };

        _context.Invites.Add(invite);
        await _context.SaveChangesAsync();

        var creator = await _userManager.FindByIdAsync(createdByUserId);
        return MapInviteDto(invite, creator?.Name ?? "");
    }

    public async Task<InviteInfoDto> ValidateInviteTokenAsync(string token)
    {
        var invite = await _context.Invites
            .FirstOrDefaultAsync(x => x.Token == token && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow);

        return new InviteInfoDto
        {
            Email = invite?.Email ?? "",
            IsValid = invite != null
        };
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        var userCount = await _userManager.Users.CountAsync();
        var isFirstUser = userCount == 0;

        if (!isFirstUser)
        {
            if (string.IsNullOrEmpty(dto.Token))
                return null;

            var invite = await _context.Invites
                .FirstOrDefaultAsync(x => x.Token == dto.Token && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow);

            if (invite == null || !string.Equals(invite.Email, dto.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                return null;

            invite.IsUsed = true;
            await _context.SaveChangesAsync();
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

    public async Task DeleteInviteAsync(int id)
    {
        var invite = await _context.Invites.FindAsync(id);
        if (invite != null)
        {
            _context.Invites.Remove(invite);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<InviteDto>> GetPendingInvitesAsync()
    {
        var invites = await _context.Invites
            .Where(x => !x.IsUsed && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var result = new List<InviteDto>();
        foreach (var inv in invites)
        {
            var creator = await _userManager.FindByIdAsync(inv.CreatedByUserId);
            result.Add(MapInviteDto(inv, creator?.Name ?? ""));
        }
        return result;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        return await _userManager.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name, Email = u.Email! })
            .ToListAsync();
    }

    private static InviteDto MapInviteDto(Invite inv, string creatorName) => new()
    {
        Id = inv.Id,
        Email = inv.Email,
        Token = inv.Token,
        ExpiresAt = inv.ExpiresAt,
        CreatedAt = inv.CreatedAt,
        IsUsed = inv.IsUsed,
        CreatedByName = creatorName
    };

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
