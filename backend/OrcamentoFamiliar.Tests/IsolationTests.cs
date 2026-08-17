using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Transactions;
using OrcamentoFamiliar.Application.DTOs.Accounts;
using OrcamentoFamiliar.Application.DTOs.CategorizationRules;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Services;

namespace OrcamentoFamiliar.Tests;

public class IsolationTests
{
    [Fact]
    public async Task FamilyB_CannotReadFamilyA_Transaction()
    {
        var seededA = await TestDbContextFactory.CreateSeededAsync($"iso_a_{Guid.NewGuid():N}");
var contextA = seededA.Context;
var familyA = seededA.Family;
var accountA = seededA.AccountId;
        var serviceA = new TransactionService(contextA, familyA);

        var created = await serviceA.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountA,
            Description = "Segredo da família A",
            Amount = 10m,
            TransactionDate = new DateTime(2026, 1, 1)
        });

        contextA.Dispose();

        // Family B (fresh db, same data shape) must not see A's transaction
        var seededB = await TestDbContextFactory.CreateSeededAsync($"iso_b_{Guid.NewGuid():N}");
var contextB = seededB.Context;
var familyB = seededB.Family;
var accountB = seededB.AccountId;
        var serviceB = new TransactionService(contextB, familyB);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceB.GetByIdAsync(created[0].Id));

        contextB.Dispose();
    }

    [Fact]
    public async Task FamilyB_CannotUpdateFamilyA_Account()
    {
        // Two families share the same database; family B must not touch A's account
        var dbName = $"iso_c_{Guid.NewGuid():N}";
        var context = TestDbContextFactory.Create(dbName);

        var familyA = new TestFamily { FamilyId = Guid.NewGuid() };
        var familyB = new TestFamily { FamilyId = Guid.NewGuid() };

        context.Families.AddRange(
            new Family { Id = familyA.FamilyId, Name = "A" },
            new Family { Id = familyB.FamilyId, Name = "B" });
        var accountA = new FinancialAccount { FamilyId = familyA.FamilyId, Name = "Conta A", InitialBalance = 0 };
        var accountB = new FinancialAccount { FamilyId = familyB.FamilyId, Name = "Conta B", InitialBalance = 0 };
        context.FinancialAccounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        var serviceB = new FinancialAccountService(context, familyB);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceB.UpdateAsync(accountA.Id, new UpdateFinancialAccountDto
        {
            Name = "Hackeado",
            Type = FinancialAccountType.CheckingAccount
        }));

        context.Dispose();
    }

    [Fact]
    public async Task FamilyB_CannotCreateRuleReferencingFamilyA_Account()
    {
        var seededA = await TestDbContextFactory.CreateSeededAsync($"iso_e_{Guid.NewGuid():N}");
var contextA = seededA.Context;
var familyA = seededA.Family;
var accountA = seededA.AccountId;
        var categoryA = await TestDbContextFactory.AddCategoryAsync(contextA, familyA.FamilyId, "Cat A");
        contextA.Dispose();

        var seededB = await TestDbContextFactory.CreateSeededAsync($"iso_f_{Guid.NewGuid():N}");
var contextB = seededB.Context;
var familyB = seededB.Family;
var accountB = seededB.AccountId;
        var serviceB = new CategorizationService(contextB, familyB);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceB.CreateRuleAsync(new CreateCategorizationRuleDto
        {
            Pattern = "X",
            CategoryId = categoryA,
            FinancialAccountId = accountA
        }));

        contextB.Dispose();
    }

    [Fact]
    public async Task SameContext_DifferentFamilies_BudgetsAreIsolated()
    {
        var dbName = $"iso_budget_{Guid.NewGuid():N}";
        var context = TestDbContextFactory.Create(dbName);

        var familyA = new TestFamily { FamilyId = Guid.NewGuid() };
        var familyB = new TestFamily { FamilyId = Guid.NewGuid() };
        context.Families.AddRange(
            new Family { Id = familyA.FamilyId, Name = "A" },
            new Family { Id = familyB.FamilyId, Name = "B" });
        await context.SaveChangesAsync();

        var serviceA = new BudgetService(context, familyA);
        var serviceB = new BudgetService(context, familyB);

        var budgetA = await serviceA.GetOrCreateMonthlyBudgetAsync(2026, 8);
        var budgetB = await serviceB.GetOrCreateMonthlyBudgetAsync(2026, 8);

        Assert.NotEqual(budgetA.Id, budgetB.Id);

        await serviceA.UpdateSalaryAsync(2026, 8, new OrcamentoFamiliar.Application.DTOs.Budget.UpdateSalaryDto { Salary1 = 5000 });

        var budgetAAfter = await serviceA.GetOrCreateMonthlyBudgetAsync(2026, 8);
        var budgetBAfter = await serviceB.GetOrCreateMonthlyBudgetAsync(2026, 8);
        Assert.Equal(5000m, budgetAAfter.Salary1);
        Assert.Equal(0m, budgetBAfter.Salary1);

        // Family B cannot add an income to family A's budget
        await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceB.AddExtraIncomeAsync(
            new OrcamentoFamiliar.Application.DTOs.Budget.CreateExtraIncomeDto
            {
                MonthlyBudgetId = budgetA.Id,
                Description = "X",
                Value = 1
            }));

        context.Dispose();
    }

    [Fact]
    public async Task SameContext_DifferentFamilies_AreIsolatedByFilter()
    {
        // Two families coexist in the same database (single context), like production single-DB
        var dbName = $"iso_multi_{Guid.NewGuid():N}";
        var context = TestDbContextFactory.Create(dbName);

        var familyA = new TestFamily { FamilyId = Guid.NewGuid() };
        var familyB = new TestFamily { FamilyId = Guid.NewGuid() };

        context.Families.AddRange(
            new Family { Id = familyA.FamilyId, Name = "A" },
            new Family { Id = familyB.FamilyId, Name = "B" });
        var accountA = new FinancialAccount { FamilyId = familyA.FamilyId, Name = "Conta A", InitialBalance = 0 };
        var accountB = new FinancialAccount { FamilyId = familyB.FamilyId, Name = "Conta B", InitialBalance = 0 };
        context.FinancialAccounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        context.Transactions.AddRange(
            new Transaction
            {
                FamilyId = familyA.FamilyId,
                FinancialAccountId = accountA.Id,
                Description = "A",
                NormalizedDescription = "A",
                Amount = 100m,
                TransactionDate = DateTime.UtcNow,
                TransactionHash = $"hash_{Guid.NewGuid():N}"
            },
            new Transaction
            {
                FamilyId = familyB.FamilyId,
                FinancialAccountId = accountB.Id,
                Description = "B",
                NormalizedDescription = "B",
                Amount = 100m,
                TransactionDate = DateTime.UtcNow,
                TransactionHash = $"hash_{Guid.NewGuid():N}"
            });
        await context.SaveChangesAsync();

        var serviceA = new TransactionService(context, familyA);
        var serviceB = new TransactionService(context, familyB);

        var resultB = await serviceB.QueryAsync(new TransactionQueryDto { Limit = 100 });
        Assert.Single(resultB);
        Assert.Equal("B", resultB[0].Description);

        var resultA = await serviceA.QueryAsync(new TransactionQueryDto { Limit = 100 });
        Assert.Single(resultA);
        Assert.Equal("A", resultA[0].Description);

        context.Dispose();
    }
}