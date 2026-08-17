using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Transactions;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Services;

namespace OrcamentoFamiliar.Tests;

public class TransactionServiceTests
{
    [Fact]
    public async Task CreateAsync_WithInstallments_PropagatesToFutureMonths()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"txn_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var service = new TransactionService(context, family);

        var result = await service.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountId,
            Description = "Celular parcelado",
            Amount = 150m,
            TransactionDate = new DateTime(2026, 1, 10),
            Type = TransactionType.Expense,
            TotalInstallments = 3
        });

        Assert.Equal(3, result.Count);
        Assert.All(result, t => Assert.Equal(150m, t.Amount));
        Assert.All(result, t => Assert.True(t.InstallmentGroupId.HasValue));
        Assert.Equal(1, result[0].CurrentInstallment);
        Assert.Equal(3, result[0].TotalInstallments);
        Assert.Equal(new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc), result[0].TransactionDate);
        Assert.Equal(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), result[1].TransactionDate);
        Assert.Equal(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), result[2].TransactionDate);

        var group = await context.InstallmentGroups.SingleAsync();
        Assert.Equal(3, group.TotalInstallments);
        Assert.Equal(450m, group.OriginalAmount);

        var all = await context.Transactions.CountAsync();
        Assert.Equal(3, all);

        context.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_OneInstallment_RemovesOnlyThatOne()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"txn_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var service = new TransactionService(context, family);

        var created = await service.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountId,
            Description = "Compra parcelada",
            Amount = 100m,
            TransactionDate = new DateTime(2026, 1, 5),
            TotalInstallments = 3
        });

        var second = created[1];
        await service.DeleteAsync(second.Id, deleteFuture: false);

        var remaining = await context.Transactions.OrderBy(t => t.CurrentInstallment).ToListAsync();
        Assert.Equal(2, remaining.Count);
        Assert.Equal(1, remaining[0].CurrentInstallment);
        Assert.Equal(3, remaining[1].CurrentInstallment);
        Assert.Contains(remaining, t => t.Id != second.Id);

        context.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_CurrentAndFuture_RemovesSiblingsAndKeepsPast()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"txn_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var service = new TransactionService(context, family);

        var created = await service.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountId,
            Description = "Móveis",
            Amount = 200m,
            TransactionDate = new DateTime(2026, 1, 15),
            TotalInstallments = 4
        });

        var second = created[1];
        var groupId = second.InstallmentGroupId!.Value;

        await service.DeleteAsync(second.Id, deleteFuture: true);

        var remaining = await context.Transactions.Where(t => t.InstallmentGroupId == groupId).OrderBy(t => t.CurrentInstallment).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(1, remaining[0].CurrentInstallment);

        // group still exists because the first installment remains
        Assert.NotNull(await context.InstallmentGroups.FindAsync(groupId));

        context.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_LastInstallment_RemovesGroup()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"txn_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var service = new TransactionService(context, family);

        var created = await service.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountId,
            Description = "Curso",
            Amount = 80m,
            TransactionDate = new DateTime(2026, 1, 20),
            TotalInstallments = 2
        });

        var groupId = created[0].InstallmentGroupId!.Value;

        await service.DeleteAsync(created[0].Id, deleteFuture: true);
        Assert.Empty(await context.Transactions.ToListAsync());
        Assert.Null(await context.InstallmentGroups.FindAsync(groupId));

        context.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_WithoutDeleteFuture_RemovesSingleFromGroup()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"txn_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var service = new TransactionService(context, family);

        var created = await service.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountId,
            Description = "Eletrônico",
            Amount = 300m,
            TransactionDate = new DateTime(2026, 1, 1),
            TotalInstallments = 2
        });

        await service.DeleteAsync(created[0].Id, deleteFuture: false);

        var remaining = await context.Transactions.SingleAsync();
        Assert.Equal(2, remaining.CurrentInstallment);

        // group kept because a transaction still references it
        Assert.NotNull(await context.InstallmentGroups.FindAsync(created[0].InstallmentGroupId!.Value));

        context.Dispose();
    }

    [Fact]
    public async Task QueryAsync_IsolatedByInstallmentAndDateRange()
    {
        var seeded = await TestDbContextFactory.CreateSeededAsync($"txn_{Guid.NewGuid():N}");
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var service = new TransactionService(context, family);

        await service.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountId,
            Description = "Compra jan",
            Amount = 50m,
            TransactionDate = new DateTime(2026, 1, 3)
        });
        await service.CreateAsync(new CreateTransactionDto
        {
            FinancialAccountId = accountId,
            Description = "Compra fev",
            Amount = 60m,
            TransactionDate = new DateTime(2026, 2, 3)
        });

        var february = await service.QueryAsync(new TransactionQueryDto
        {
            From = new DateTime(2026, 2, 1),
            To = new DateTime(2026, 2, 28)
        });

        Assert.Single(february);
        Assert.Equal("Compra fev", february[0].Description);

        context.Dispose();
    }
}