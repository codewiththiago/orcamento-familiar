using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class CurrentFamilyService : ICurrentFamily
{
    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    public CurrentFamilyService(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    public async Task<Guid> GetFamilyIdAsync()
    {
        var claim = _http.HttpContext?.User?.FindFirstValue("familyId");
        if (Guid.TryParse(claim, out var claimFamilyId))
            return claimFamilyId;

        var userId = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            var userFamilyId = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FamilyId)
                .FirstOrDefaultAsync();
            if (userFamilyId.HasValue)
                return userFamilyId.Value;
        }

        var family = await _db.Families.AsNoTracking().OrderBy(f => f.CreatedAt).FirstOrDefaultAsync();
        return family?.Id ?? throw new InvalidOperationException("Nenhuma família configurada");
    }

    public async Task<string?> GetUserIdAsync()
    {
        var userId = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null) return userId;
        await Task.CompletedTask;
        return null;
    }
}