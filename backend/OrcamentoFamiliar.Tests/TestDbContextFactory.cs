using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Tests;

public sealed class TestFamily : ICurrentFamily
{
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public Task<Guid> GetFamilyIdAsync() => Task.FromResult(FamilyId);
    public Task<string?> GetUserIdAsync() => Task.FromResult<string?>(null);
}

public sealed class SeededContext
{
    public required AppDbContext Context { get; init; }
    public required TestFamily Family { get; init; }
    public required int AccountId { get; init; }
}

public static class TestDbContextFactory
{
    public static AppDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    public static async Task<SeededContext> CreateSeededAsync(string dbName)
    {
        var context = Create(dbName);
        var family = new TestFamily();
        context.Families.Add(new Domain.Entities.Family { Id = family.FamilyId, Name = "Família Teste" });
        var account = new Domain.Entities.FinancialAccount
        {
            FamilyId = family.FamilyId,
            Name = "Conta Teste",
            InitialBalance = 0
        };
        context.FinancialAccounts.Add(account);
        await context.SaveChangesAsync();
        return new SeededContext { Context = context, Family = family, AccountId = account.Id };
    }

    public static async Task<int> AddCategoryAsync(AppDbContext context, string name)
    {
        var category = new Domain.Entities.Category { Name = name };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return category.Id;
    }
}