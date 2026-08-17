using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.CategorizationRules;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Data;
using OrcamentoFamiliar.Infrastructure.Parsers;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class CategorizationService : ICategorizationService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;

    public CategorizationService(AppDbContext context, ICurrentFamily currentFamily)
    {
        _context = context;
        _currentFamily = currentFamily;
    }

    public async Task<List<CategorizationRuleDto>> GetRulesAsync()
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var rules = await _context.CategorizationRules
            .AsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.FinancialAccount)
            .Where(r => r.FamilyId == familyId)
            .OrderBy(r => r.FinancialAccountId == null)
            .ThenBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync();

        return rules.Select(MapDto).ToList();
    }

    public async Task<CategorizationRuleDto> CreateRuleAsync(CreateCategorizationRuleDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        await ValidateAsync(familyId, dto.FinancialAccountId, dto.CategoryId);

        var entity = new CategorizationRule
        {
            FamilyId = familyId,
            FinancialAccountId = dto.FinancialAccountId,
            Pattern = dto.Pattern.Trim(),
            RuleMatchType = dto.RuleMatchType,
            CategoryId = dto.CategoryId,
            Priority = dto.Priority,
            Active = dto.Active
        };

        _context.CategorizationRules.Add(entity);
        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(r => r.Category).LoadAsync();
        await _context.Entry(entity).Reference(r => r.FinancialAccount).LoadAsync();
        return MapDto(entity);
    }

    public async Task<CategorizationRuleDto> UpdateRuleAsync(int id, UpdateCategorizationRuleDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        await ValidateAsync(familyId, dto.FinancialAccountId, dto.CategoryId);

        var entity = await _context.CategorizationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Regra não encontrada");

        entity.FinancialAccountId = dto.FinancialAccountId;
        entity.Pattern = dto.Pattern.Trim();
        entity.RuleMatchType = dto.RuleMatchType;
        entity.CategoryId = dto.CategoryId;
        entity.Priority = dto.Priority;
        entity.Active = dto.Active;

        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(r => r.Category).LoadAsync();
        await _context.Entry(entity).Reference(r => r.FinancialAccount).LoadAsync();
        return MapDto(entity);
    }

    public async Task DeleteRuleAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var entity = await _context.CategorizationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Regra não encontrada");

        _context.CategorizationRules.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<int?> CategorizeAsync(string normalizedDescription, int financialAccountId)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var rules = await LoadRulesAsync(familyId);

        var match = FindMatch(rules, normalizedDescription, financialAccountId);
        if (match != null) return match;

        return await HistoryCategoryAsync(familyId, normalizedDescription);
    }

    public async Task<Dictionary<string, int?>> CategorizeBulkAsync(IReadOnlyCollection<(string NormalizedDescription, int FinancialAccountId)> items)
    {
        if (items.Count == 0) return new Dictionary<string, int?>();

        var familyId = await _currentFamily.GetFamilyIdAsync();
        var rules = await LoadRulesAsync(familyId);

        var result = new Dictionary<string, int?>();
        var unmatched = new List<(string NormalizedDescription, int FinancialAccountId)>();

        foreach (var item in items)
        {
            var key = Key(item.NormalizedDescription, item.FinancialAccountId);
            var match = FindMatch(rules, item.NormalizedDescription, item.FinancialAccountId);
            if (match != null)
            {
                result[key] = match;
            }
            else
            {
                result[key] = null;
                unmatched.Add(item);
            }
        }

        var unmatchedDescriptions = unmatched
            .Select(u => u.NormalizedDescription)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .ToList();

        if (unmatchedDescriptions.Count > 0)
        {
            var history = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.FamilyId == familyId
                    && t.CategoryId != null
                    && unmatchedDescriptions.Contains(t.NormalizedDescription))
                .GroupBy(t => t.NormalizedDescription)
                .Select(g => g.OrderByDescending(t => t.TransactionDate).First())
                .ToListAsync();

            foreach (var group in history)
            {
                foreach (var unmatchedItem in unmatched.Where(u => u.NormalizedDescription == group.NormalizedDescription))
                {
                    result[Key(unmatchedItem.NormalizedDescription, unmatchedItem.FinancialAccountId)] = group.CategoryId;
                }
            }
        }

        return result;
    }

    private async Task<List<CategorizationRule>> LoadRulesAsync(Guid familyId) =>
        await _context.CategorizationRules
            .AsNoTracking()
            .Where(r => r.FamilyId == familyId && r.Active)
            .OrderBy(r => r.FinancialAccountId == null)
            .ThenBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync();

    private static int? FindMatch(List<CategorizationRule> rules, string normalizedDescription, int financialAccountId)
    {
        foreach (var rule in rules)
        {
            if (rule.FinancialAccountId.HasValue && rule.FinancialAccountId.Value != financialAccountId)
                continue;

            if (Matches(rule, normalizedDescription))
                return rule.CategoryId;
        }

        return null;
    }

    private static bool Matches(CategorizationRule rule, string normalizedDescription)
    {
        return rule.RuleMatchType switch
        {
            RuleMatchType.Exact => normalizedDescription == TransactionNormalizer.Normalize(rule.Pattern),
            RuleMatchType.StartsWith => normalizedDescription.StartsWith(TransactionNormalizer.Normalize(rule.Pattern), StringComparison.Ordinal),
            RuleMatchType.Regex => Regex.IsMatch(normalizedDescription, rule.Pattern, RegexOptions.IgnoreCase),
            _ => normalizedDescription.Contains(TransactionNormalizer.Normalize(rule.Pattern), StringComparison.Ordinal)
        };
    }

    private async Task<int?> HistoryCategoryAsync(Guid familyId, string normalizedDescription) =>
        await _context.Transactions
            .AsNoTracking()
            .Where(t => t.FamilyId == familyId
                && t.NormalizedDescription == normalizedDescription
                && t.CategoryId != null)
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => t.CategoryId)
            .FirstOrDefaultAsync();

    private async Task ValidateAsync(Guid familyId, int? financialAccountId, int categoryId)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId && c.FamilyId == familyId);
        if (!categoryExists)
            throw new KeyNotFoundException("Categoria não encontrada");

        if (financialAccountId.HasValue)
        {
            var accountExists = await _context.FinancialAccounts
                .AnyAsync(a => a.Id == financialAccountId.Value && a.FamilyId == familyId);
            if (!accountExists)
                throw new KeyNotFoundException("Conta financeira não encontrada");
        }
    }

    private static string Key(string normalizedDescription, int financialAccountId) =>
        $"{financialAccountId}|{normalizedDescription}";

    private static CategorizationRuleDto MapDto(CategorizationRule r) => new()
    {
        Id = r.Id,
        FamilyId = r.FamilyId,
        FinancialAccountId = r.FinancialAccountId,
        FinancialAccountName = r.FinancialAccount?.Name ?? "",
        Pattern = r.Pattern,
        RuleMatchType = r.RuleMatchType,
        CategoryId = r.CategoryId,
        CategoryName = r.Category?.Name ?? "",
        Priority = r.Priority,
        Active = r.Active,
        CreatedAt = r.CreatedAt
    };
}