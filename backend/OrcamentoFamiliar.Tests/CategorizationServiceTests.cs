using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.CategorizationRules;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Services;

namespace OrcamentoFamiliar.Tests;

public class CategorizationServiceTests
{
    [Fact]
    public async Task ContainsRule_MatchesNormalizedDescription()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"cat_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var categoryId = await TestDbContextFactory.AddCategoryAsync(context, "Combustível");
        var service = new CategorizationService(context, family);

        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "POSTO",
            RuleMatchType = RuleMatchType.Contains,
            CategoryId = categoryId
        });

        var category = await service.CategorizeAsync(TransactionNormalizerTestCase.Normalized("POSTO IPOJUCA"), accountId);
        Assert.Equal(categoryId, category);

        context.Dispose();
    }

    [Fact]
    public async Task ExactRule_DoesNotMatchPartial()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"cat_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var categoryId = await TestDbContextFactory.AddCategoryAsync(context, "Assinaturas");
        var service = new CategorizationService(context, family);

        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "OPENROUTER",
            RuleMatchType = RuleMatchType.Exact,
            CategoryId = categoryId
        });

        var match = await service.CategorizeAsync(TransactionNormalizerTestCase.Normalized("OPENROUTER "), accountId);
        Assert.Equal(categoryId, match);

        var noMatch = await service.CategorizeAsync(TransactionNormalizerTestCase.Normalized("OPENROUTER EXTRA"), accountId);
        Assert.Null(noMatch);

        context.Dispose();
    }

    [Fact]
    public async Task AccountSpecificRule_WinsOverGlobalRule()
    {
        var dbName = $"cat_{Guid.NewGuid():N}";
        var seeded = await TestDbContextFactory.CreateSeededAsync(dbName);
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;

        var otherAccount = new FinancialAccount { FamilyId = family.FamilyId, Name = "Outra", InitialBalance = 0 };
        context.FinancialAccounts.Add(otherAccount);
        await context.SaveChangesAsync();
        var otherAccountId = otherAccount.Id;

        var globalCategory = await TestDbContextFactory.AddCategoryAsync(context, "Geral");
        var specificCategory = await TestDbContextFactory.AddCategoryAsync(context, "Específica");
        var service = new CategorizationService(context, family);

        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "ANTHROPIC",
            RuleMatchType = RuleMatchType.Contains,
            CategoryId = globalCategory,
            Priority = 1
        });
        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "ANTHROPIC",
            RuleMatchType = RuleMatchType.Contains,
            CategoryId = specificCategory,
            Priority = 100,
            FinancialAccountId = accountId
        });

        var forAccount = await service.CategorizeAsync("ANTHROPIC API", accountId);
        Assert.Equal(specificCategory, forAccount);

        var forOther = await service.CategorizeAsync("ANTHROPIC API", otherAccountId);
        Assert.Equal(globalCategory, forOther);

        context.Dispose();
    }

    [Fact]
    public async Task Priority_RespectsOrder()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"cat_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var lowCategory = await TestDbContextFactory.AddCategoryAsync(context, "Baixa");
        var highCategory = await TestDbContextFactory.AddCategoryAsync(context, "Alta");
        var service = new CategorizationService(context, family);

        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "SUPERMERCADO",
            RuleMatchType = RuleMatchType.Contains,
            CategoryId = highCategory,
            Priority = 10
        });
        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "SUPERMERCADO",
            RuleMatchType = RuleMatchType.Contains,
            CategoryId = lowCategory,
            Priority = 5
        });

        var category = await service.CategorizeAsync("SUPERMERCADO EXTRA", accountId);
        Assert.Equal(lowCategory, category);

        context.Dispose();
    }

    [Fact]
    public async Task HistoryFallback_WhenNoRuleMatches()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"cat_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var categoryId = await TestDbContextFactory.AddCategoryAsync(context, "Serviços");
        var service = new CategorizationService(context, family);

        context.Transactions.Add(new Transaction
        {
            FamilyId = family.FamilyId,
            FinancialAccountId = accountId,
            CategoryId = categoryId,
            Description = "ifood",
            NormalizedDescription = OrcamentoFamiliar.Infrastructure.Parsers.TransactionNormalizer.Normalize("ifood"),
            Amount = 10m,
            TransactionDate = DateTime.UtcNow,
            TransactionHash = $"h{Guid.NewGuid():N}"
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var category = await service.CategorizeAsync(
            OrcamentoFamiliar.Infrastructure.Parsers.TransactionNormalizer.Normalize("ifood"), accountId);
        Assert.Equal(categoryId, category);

        context.Dispose();
    }

    [Fact]
    public async Task GetRulesAsync_ReturnsAccountRulesBeforeGlobal()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"cat_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var categoryId = await TestDbContextFactory.AddCategoryAsync(context, "Outros");
        var service = new CategorizationService(context, family);

        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "GLOBAL",
            RuleMatchType = RuleMatchType.Contains,
            CategoryId = categoryId,
            Priority = 1
        });
        await service.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "ESPECIFICA",
            RuleMatchType = RuleMatchType.Contains,
            CategoryId = categoryId,
            FinancialAccountId = accountId,
            Priority = 999
        });

        var rules = await service.GetRulesAsync();
        Assert.Equal(2, rules.Count);
        Assert.True(rules[0].FinancialAccountId.HasValue);
        Assert.Null(rules[1].FinancialAccountId);

        context.Dispose();
    }
}

file static class TransactionNormalizerTestCase
{
    public static string Normalized(string s) =>
        OrcamentoFamiliar.Infrastructure.Parsers.TransactionNormalizer.Normalize(s);
}