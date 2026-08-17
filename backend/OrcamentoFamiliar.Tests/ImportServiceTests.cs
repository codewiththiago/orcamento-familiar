using System.Text;
using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Imports;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Parsers;
using OrcamentoFamiliar.Infrastructure.Services;

namespace OrcamentoFamiliar.Tests;

public class ImportServiceTests
{
    [Fact]
    public async Task Confirm_ThenPreview_SameFile_DetectsAllDuplicates()
    {
        var dbName = $"imp_{Guid.NewGuid():N}";
        var seeded = await TestDbContextFactory.CreateSeededAsync(dbName);
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var categorization = new CategorizationService(context, family);
        var service = new ImportService(context, family,
            new ImportParserFactory([new CsvTransactionParser()]),
            categorization);

        var confirm = new ConfirmImportRequestDto
        {
            FinancialAccountId = accountId,
            FileName = "extrato.csv",
            Format = ImportFormat.Csv,
            Items =
            [
                new ConfirmImportItemDto { Description = "MERCADO EXTRA", Amount = 150.90m, TransactionDate = new DateTime(2026, 8, 1), Type = TransactionType.Expense, ExternalId = "x1" },
                new ConfirmImportItemDto { Description = "PIX RECEBIDO", Amount = 500m, TransactionDate = new DateTime(2026, 8, 2), Type = TransactionType.Income, ExternalId = "x2" }
            ]
        };

        var result = await service.ConfirmAsync(confirm);
        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Duplicates);

        Assert.Equal(2, await context.Transactions.CountAsync());

        // Re-preview the same content via CSV parsing
        var csv = "data;valor;identificador;descricao\n01/08/2026;-150,90;x1;MERCADO EXTRA\n02/08/2026;500,00;x2;PIX RECEBIDO\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var preview = await service.PreviewAsync(stream, "extrato.csv", ImportFormat.Csv, null, accountId);

        Assert.Equal(2, preview.TotalFound);
        Assert.Equal(2, preview.DuplicateCount);
        Assert.Equal(0, preview.NewCount);

        context.Dispose();
    }

    [Fact]
    public async Task OverlappingPeriods_ImportsOnlyNewTransactions()
    {
        var dbName = $"imp_{Guid.NewGuid():N}";
        var seeded = await TestDbContextFactory.CreateSeededAsync(dbName);
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var categorization = new CategorizationService(context, family);
        var service = new ImportService(context, family,
            new ImportParserFactory([new CsvTransactionParser()]),
            categorization);

        // First import: 01/08 -> 15/08
        var first = await service.ConfirmAsync(new ConfirmImportRequestDto
        {
            FinancialAccountId = accountId,
            FileName = "agosto-1a15.csv",
            Format = ImportFormat.Csv,
            Items =
            [
                new ConfirmImportItemDto { Description = "MERCADO", Amount = 10m, TransactionDate = new DateTime(2026, 8, 1), Type = TransactionType.Expense },
                new ConfirmImportItemDto { Description = "FARMACIA", Amount = 20m, TransactionDate = new DateTime(2026, 8, 10), Type = TransactionType.Expense }
            ]
        });
        Assert.Equal(2, first.Imported);

        // Second import: 01/08 -> 31/08 (overlapping the first period)
        var second = await service.ConfirmAsync(new ConfirmImportRequestDto
        {
            FinancialAccountId = accountId,
            FileName = "agosto-completo.csv",
            Format = ImportFormat.Csv,
            Items =
            [
                new ConfirmImportItemDto { Description = "MERCADO", Amount = 10m, TransactionDate = new DateTime(2026, 8, 1), Type = TransactionType.Expense },
                new ConfirmImportItemDto { Description = "FARMACIA", Amount = 20m, TransactionDate = new DateTime(2026, 8, 10), Type = TransactionType.Expense },
                new ConfirmImportItemDto { Description = "POSTO", Amount = 30m, TransactionDate = new DateTime(2026, 8, 25), Type = TransactionType.Expense }
            ]
        });

        Assert.Equal(1, second.Imported);
        Assert.Equal(2, second.Duplicates);

        Assert.Equal(3, await context.Transactions.CountAsync());

        context.Dispose();
    }

    [Fact]
    public async Task Preview_AppliesAutomaticCategorization()
    {
        var dbName = $"imp_{Guid.NewGuid():N}";
        var seeded = await TestDbContextFactory.CreateSeededAsync(dbName);
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var comboCategory = await TestDbContextFactory.AddCategoryAsync(context, family.FamilyId, "Combustível");
        var categorization = new CategorizationService(context, family);
        await categorization.CreateRuleAsync(new OrcamentoFamiliar.Application.DTOs.CategorizationRules.CreateCategorizationRuleDto
        {
            Pattern = "POSTO",
            RuleMatchType = Domain.Enums.RuleMatchType.Contains,
            CategoryId = comboCategory
        });

        var service = new ImportService(context, family,
            new ImportParserFactory([new CsvTransactionParser()]),
            categorization);

        var csv = "data;valor;descricao\n01/08/2026;-100,00;POSTO IPIRANGA\n01/08/2026;-50,00;COISA DESCONHECIDA\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var preview = await service.PreviewAsync(stream, "x.csv", ImportFormat.Csv, null, accountId);

        Assert.Equal(2, preview.TotalFound);
        var posto = preview.Items.Single(i => i.Description.Contains("POSTO"));
        var desconhecida = preview.Items.Single(i => i.Description.Contains("COISA"));

        Assert.True(posto.IsCategorized);
        Assert.Equal(comboCategory, posto.CategoryId);
        Assert.False(desconhecida.IsCategorized);
        Assert.Null(desconhecida.CategoryId);
        Assert.Equal(1, preview.CategorizedCount);
        Assert.Equal(1, preview.NeedsReviewCount);

        context.Dispose();
    }

    [Fact]
    public async Task History_RecordsImportSummary()
    {
        var dbName = $"imp_{Guid.NewGuid():N}";
        var seeded = await TestDbContextFactory.CreateSeededAsync(dbName);
var context = seeded.Context;
var family = seeded.Family;
var accountId = seeded.AccountId;
        var categorization = new CategorizationService(context, family);
        var service = new ImportService(context, family,
            new ImportParserFactory([new CsvTransactionParser()]),
            categorization);

        await service.ConfirmAsync(new ConfirmImportRequestDto
        {
            FinancialAccountId = accountId,
            FileName = "outubro.csv",
            Format = ImportFormat.Csv,
            Items =
            [
                new ConfirmImportItemDto { Description = "LUZ", Amount = 200m, TransactionDate = new DateTime(2026, 10, 5), Type = TransactionType.Expense }
            ]
        });

        // Second import re-sends the same transaction (duplicate) plus a new one
        await service.ConfirmAsync(new ConfirmImportRequestDto
        {
            FinancialAccountId = accountId,
            FileName = "outubro-v2.csv",
            Format = ImportFormat.Csv,
            Items =
            [
                new ConfirmImportItemDto { Description = "LUZ", Amount = 200m, TransactionDate = new DateTime(2026, 10, 5), Type = TransactionType.Expense },
                new ConfirmImportItemDto { Description = "AGUA", Amount = 150m, TransactionDate = new DateTime(2026, 10, 8), Type = TransactionType.Expense }
            ]
        });

        var history = await service.GetHistoryAsync();
        Assert.Equal(2, history.Count);

        var first = history.Single(i => i.FileName == "outubro.csv");
        Assert.Equal(1, first.TotalRecords);
        Assert.Equal(1, first.ImportedRecords);
        Assert.Equal(0, first.DuplicateRecords);

        var second = history.Single(i => i.FileName == "outubro-v2.csv");
        Assert.Equal(2, second.TotalRecords);
        Assert.Equal(1, second.ImportedRecords);
        Assert.Equal(1, second.DuplicateRecords);

        var byId = await service.GetByIdAsync(first.Id);
        Assert.Equal(first.Id, byId.Id);
        Assert.Equal("outubro.csv", byId.FileName);

        context.Dispose();
    }

    [Fact]
    public async Task Normalizer_HandlesDescriptions()
    {
        Assert.Equal("MERCADO EXTRA", TransactionNormalizer.Normalize("  mercado  extra "));
        Assert.Equal("IFOOD ENTREGA", TransactionNormalizer.Normalize("iFood★Entrega"));
        Assert.Equal("", TransactionNormalizer.Normalize(null));
    }
}