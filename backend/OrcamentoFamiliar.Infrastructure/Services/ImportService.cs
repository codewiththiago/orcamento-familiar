using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Imports;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Data;
using OrcamentoFamiliar.Infrastructure.Parsers;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class ImportService : IImportService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;
    private readonly IImportParserFactory _parserFactory;
    private readonly ICategorizationService _categorization;

    public ImportService(
        AppDbContext context,
        ICurrentFamily currentFamily,
        IImportParserFactory parserFactory,
        ICategorizationService categorization)
    {
        _context = context;
        _currentFamily = currentFamily;
        _parserFactory = parserFactory;
        _categorization = categorization;
    }

    public async Task<ImportPreviewDto> PreviewAsync(Stream file, string fileName, ImportFormat format, string? institution, int financialAccountId)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        await EnsureAccountAsync(familyId, financialAccountId);

        var parser = _parserFactory.GetParser(format, institution)
            ?? throw new InvalidOperationException("Formato de importação não suportado");

        var parsed = await parser.ParseAsync(file, institution);

        var existingHashes = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.FamilyId == familyId)
            .Select(t => t.TransactionHash)
            .ToListAsync();
        var hashSet = existingHashes.ToHashSet(StringComparer.Ordinal);

        var categoryNames = await _context.Categories
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var dtoItems = parsed.Select(p =>
        {
            var normalized = TransactionNormalizer.Normalize(p.Description);
            var hash = TransactionNormalizer.BuildHash(financialAccountId, p.ExternalId, p.TransactionDate, p.Amount, normalized);
            return new ParsedTransactionDto
            {
                Description = p.Description,
                NormalizedDescription = normalized,
                Amount = p.Amount,
                TransactionDate = p.TransactionDate,
                Type = p.Type,
                ExternalId = p.ExternalId,
                TransactionHash = hash
            };
        }).ToList();

        var categorizationKeys = dtoItems
            .Where(i => !hashSet.Contains(i.TransactionHash))
            .Select(i => (i.NormalizedDescription, financialAccountId))
            .ToList();

        var categories = await _categorization.CategorizeBulkAsync(categorizationKeys);

        foreach (var item in dtoItems)
        {
            var isDuplicate = hashSet.Contains(item.TransactionHash);
            item.IsDuplicate = isDuplicate;
            item.CategoryId = isDuplicate ? null : categories.GetValueOrDefault($"{financialAccountId}|{item.NormalizedDescription}");
            item.IsCategorized = item.CategoryId.HasValue;
            if (item.CategoryId.HasValue)
                item.CategoryName = categoryNames.GetValueOrDefault(item.CategoryId.Value, "");
        }

        return new ImportPreviewDto
        {
            TotalFound = dtoItems.Count,
            NewCount = dtoItems.Count(i => !i.IsDuplicate),
            DuplicateCount = dtoItems.Count(i => i.IsDuplicate),
            CategorizedCount = dtoItems.Count(i => i.IsCategorized),
            NeedsReviewCount = dtoItems.Count(i => !i.IsDuplicate && !i.IsCategorized),
            Items = dtoItems
        };
    }

    public async Task<ImportResultDto> ConfirmAsync(ConfirmImportRequestDto request)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var userId = await _currentFamily.GetUserIdAsync();
        await EnsureAccountAsync(familyId, request.FinancialAccountId);

        var validItems = request.Items
            .Where(i => i.Amount > 0 && !string.IsNullOrWhiteSpace(i.Description))
            .ToList();

        var items = validItems.Select(i =>
        {
            var normalized = TransactionNormalizer.Normalize(i.Description);
            return new
            {
                Normalized = normalized,
                Hash = TransactionNormalizer.BuildHash(request.FinancialAccountId, i.ExternalId, i.TransactionDate, i.Amount, normalized),
                Item = i
            };
        }).ToList();

        var existingHashes = await _context.Transactions
            .Where(t => t.FamilyId == familyId)
            .Select(t => t.TransactionHash)
            .ToListAsync();
        var hashSet = existingHashes.ToHashSet(StringComparer.Ordinal);

        var categories = await _context.Categories.AsNoTracking().Select(c => c.Id).ToListAsync();
        var validCategoryIds = categories.ToHashSet();

        var toCreate = items.Where(x => !hashSet.Contains(x.Hash)).ToList();
        var duplicates = items.Count - toCreate.Count;

        var import = new Import
        {
            FamilyId = familyId,
            FinancialAccountId = request.FinancialAccountId,
            FileName = string.IsNullOrWhiteSpace(request.FileName) ? "importacao" : request.FileName.Trim(),
            FileHash = TransactionNormalizer.BuildFileHash(items.Select(x => $"{x.Item.TransactionDate:yyyy-MM-dd}|{x.Normalized}|{x.Item.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}").OrderBy(s => s)),
            Format = request.Format,
            ImportedAt = DateTime.UtcNow,
            ImportedByUserId = userId,
            TotalRecords = request.Items.Count,
            DuplicateRecords = duplicates
        };

        _context.Imports.Add(import);

        var imported = 0;
        var failed = 0;

        foreach (var x in toCreate)
        {
            import.Transactions.Add(new Transaction
            {
                FamilyId = familyId,
                FinancialAccountId = request.FinancialAccountId,
                CategoryId = validCategoryIds.Contains(x.Item.CategoryId ?? 0) ? x.Item.CategoryId : null,
                Type = x.Item.Type,
                Description = x.Item.Description.Trim(),
                NormalizedDescription = x.Normalized,
                Amount = x.Item.Amount,
                TransactionDate = DateTime.SpecifyKind(x.Item.TransactionDate.Date, DateTimeKind.Utc),
                Status = TransactionStatus.Confirmed,
                ExternalId = string.IsNullOrWhiteSpace(x.Item.ExternalId) ? null : x.Item.ExternalId.Trim(),
                TransactionHash = x.Hash,
                CurrentInstallment = 1,
                TotalInstallments = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            imported++;
        }

        import.ImportedRecords = imported;
        import.FailedRecords = failed;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Concurrency guard: unique index (FamilyId, TransactionHash) may reject
            // rows that were inserted concurrently. Fall back to per-item inserts.
            _context.ChangeTracker.Clear();

            var recheck = new HashSet<string>(await _context.Transactions
                .Where(t => t.FamilyId == familyId)
                .Select(t => t.TransactionHash)
                .ToListAsync(), StringComparer.Ordinal);

            var importEntity = new Import
            {
                FamilyId = familyId,
                FinancialAccountId = request.FinancialAccountId,
                FileName = import.FileName,
                FileHash = import.FileHash,
                Format = request.Format,
                ImportedAt = DateTime.UtcNow,
                ImportedByUserId = userId,
                TotalRecords = request.Items.Count
            };

            _context.Imports.Add(importEntity);
            await _context.SaveChangesAsync();

            foreach (var x in toCreate)
            {
                if (recheck.Contains(x.Hash))
                {
                    importEntity.DuplicateRecords++;
                    continue;
                }

                try
                {
                    _context.Transactions.Add(new Transaction
                    {
                        FamilyId = familyId,
                        FinancialAccountId = request.FinancialAccountId,
                        CategoryId = validCategoryIds.Contains(x.Item.CategoryId ?? 0) ? x.Item.CategoryId : null,
                        Type = x.Item.Type,
                        Description = x.Item.Description.Trim(),
                        NormalizedDescription = x.Normalized,
                        Amount = x.Item.Amount,
                        TransactionDate = DateTime.SpecifyKind(x.Item.TransactionDate.Date, DateTimeKind.Utc),
                        Status = TransactionStatus.Confirmed,
                        ExternalId = string.IsNullOrWhiteSpace(x.Item.ExternalId) ? null : x.Item.ExternalId.Trim(),
                        TransactionHash = x.Hash,
                        CurrentInstallment = 1,
                        TotalInstallments = 1,
                        ImportId = importEntity.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    importEntity.ImportedRecords++;
                    recheck.Add(x.Hash);
                }
                catch (DbUpdateException)
                {
                    _context.ChangeTracker.Clear();
                    importEntity.DuplicateRecords++;
                }
            }

            return new ImportResultDto
            {
                ImportId = importEntity.Id,
                Imported = importEntity.ImportedRecords,
                Duplicates = importEntity.DuplicateRecords,
                Failed = 0,
                Total = request.Items.Count
            };
        }

        return new ImportResultDto
        {
            ImportId = import.Id,
            Imported = imported,
            Duplicates = duplicates,
            Failed = failed,
            Total = request.Items.Count
        };
    }

    public async Task<List<ImportDto>> GetHistoryAsync()
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        return await _context.Imports
            .AsNoTracking()
            .Include(i => i.FinancialAccount)
            .Where(i => i.FamilyId == familyId)
            .OrderByDescending(i => i.ImportedAt)
            .Select(i => new ImportDto
            {
                Id = i.Id,
                FamilyId = i.FamilyId,
                FinancialAccountId = i.FinancialAccountId,
                FinancialAccountName = i.FinancialAccount!.Name,
                FileName = i.FileName,
                FileHash = i.FileHash,
                Format = i.Format,
                ImportedAt = i.ImportedAt,
                ImportedByUserName = i.ImportedByUserId,
                TotalRecords = i.TotalRecords,
                ImportedRecords = i.ImportedRecords,
                DuplicateRecords = i.DuplicateRecords,
                FailedRecords = i.FailedRecords
            })
            .ToListAsync();
    }

    public async Task<ImportDto> GetByIdAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var result = await _context.Imports
            .AsNoTracking()
            .Include(i => i.FinancialAccount)
            .Where(i => i.Id == id && i.FamilyId == familyId)
            .Select(i => new ImportDto
            {
                Id = i.Id,
                FamilyId = i.FamilyId,
                FinancialAccountId = i.FinancialAccountId,
                FinancialAccountName = i.FinancialAccount!.Name,
                FileName = i.FileName,
                FileHash = i.FileHash,
                Format = i.Format,
                ImportedAt = i.ImportedAt,
                TotalRecords = i.TotalRecords,
                ImportedRecords = i.ImportedRecords,
                DuplicateRecords = i.DuplicateRecords,
                FailedRecords = i.FailedRecords
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Importação não encontrada");

        return result;
    }

    private async Task EnsureAccountAsync(Guid familyId, int financialAccountId)
    {
        var accountExists = await _context.FinancialAccounts
            .AnyAsync(a => a.Id == financialAccountId && a.FamilyId == familyId);
        if (!accountExists)
            throw new KeyNotFoundException("Conta financeira não encontrada");
    }
}