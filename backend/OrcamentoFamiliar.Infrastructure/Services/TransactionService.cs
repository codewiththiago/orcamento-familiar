using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Transactions;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Data;
using OrcamentoFamiliar.Infrastructure.Parsers;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;

    public TransactionService(AppDbContext context, ICurrentFamily currentFamily)
    {
        _context = context;
        _currentFamily = currentFamily;
    }

    public async Task<List<TransactionDto>> QueryAsync(TransactionQueryDto query)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var q = _context.Transactions
            .AsNoTracking()
            .Include(t => t.FinancialAccount)
            .Include(t => t.Category)
            .Where(t => t.FamilyId == familyId && t.Status == TransactionStatus.Confirmed);

        if (query.From.HasValue)
        {
            var from = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
            q = q.Where(t => t.TransactionDate >= from);
        }
        if (query.To.HasValue)
        {
            var to = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1), DateTimeKind.Utc);
            q = q.Where(t => t.TransactionDate < to);
        }
        if (query.AccountId.HasValue)
            q = q.Where(t => t.FinancialAccountId == query.AccountId);
        if (query.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == query.CategoryId);
        if (query.Type.HasValue)
            q = q.Where(t => t.Type == query.Type);

        var items = await q
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Take(Math.Clamp(query.Limit, 1, 1000))
            .ToListAsync();

        return items.Select(MapDto).ToList();
    }

    public async Task<TransactionDto> GetByIdAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var entity = await _context.Transactions
            .AsNoTracking()
            .Include(t => t.FinancialAccount)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id && t.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Transação não encontrada");

        return MapDto(entity);
    }

    public async Task<List<TransactionDto>> CreateAsync(CreateTransactionDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var account = await _context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == dto.FinancialAccountId && a.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Conta financeira não encontrada");

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value && c.FamilyId == familyId);
            if (!categoryExists)
                throw new KeyNotFoundException("Categoria não encontrada");
        }

        var description = dto.Description.Trim();
        var normalized = TransactionNormalizer.Normalize(description);
        var totalInstallments = Math.Max(1, dto.TotalInstallments);
        var baseDate = DateTime.SpecifyKind(dto.TransactionDate.Date, DateTimeKind.Utc);

        InstallmentGroup? group = null;
        if (totalInstallments > 1)
        {
            group = new InstallmentGroup
            {
                FamilyId = familyId,
                FinancialAccountId = account.Id,
                Description = description,
                OriginalAmount = dto.Amount * totalInstallments,
                InstallmentValue = dto.Amount,
                TotalInstallments = totalInstallments
            };
            _context.InstallmentGroups.Add(group);
            await _context.SaveChangesAsync();
        }

        var transactions = new List<Transaction>();
        for (var i = 1; i <= totalInstallments; i++)
        {
            transactions.Add(new Transaction
            {
                FamilyId = familyId,
                FinancialAccountId = account.Id,
                CategoryId = dto.CategoryId,
                Type = dto.Type,
                Description = description,
                NormalizedDescription = normalized,
                Amount = dto.Amount,
                TransactionDate = baseDate.AddMonths(i - 1),
                Status = TransactionStatus.Confirmed,
                TransactionHash = $"manual:{Guid.NewGuid():N}",
                InstallmentGroupId = group?.Id,
                CurrentInstallment = i,
                TotalInstallments = totalInstallments,
                Observation = dto.Observation,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        return transactions.Select(MapDto).ToList();
    }

    public async Task<TransactionDto> UpdateAsync(int id, UpdateTransactionDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var entity = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Transação não encontrada");

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value && c.FamilyId == familyId);
            if (!categoryExists)
                throw new KeyNotFoundException("Categoria não encontrada");
        }

        entity.Description = dto.Description.Trim();
        entity.NormalizedDescription = TransactionNormalizer.Normalize(dto.Description);
        entity.Amount = dto.Amount;
        entity.TransactionDate = DateTime.SpecifyKind(dto.TransactionDate.Date, DateTimeKind.Utc);
        entity.Type = dto.Type;
        entity.CategoryId = dto.CategoryId;
        entity.Observation = dto.Observation;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(t => t.FinancialAccount).LoadAsync();
        await _context.Entry(entity).Reference(t => t.Category).LoadAsync();
        return MapDto(entity);
    }

    public async Task DeleteAsync(int id, bool deleteFuture = false)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var entity = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Transação não encontrada");

        if (deleteFuture && entity.InstallmentGroupId.HasValue)
        {
            var siblings = await _context.Transactions
                .Where(t => t.InstallmentGroupId == entity.InstallmentGroupId && t.CurrentInstallment >= entity.CurrentInstallment)
                .ToListAsync();

            _context.Transactions.RemoveRange(siblings);
            await _context.SaveChangesAsync();

            var remaining = await _context.Transactions.CountAsync(t => t.InstallmentGroupId == entity.InstallmentGroupId);
            if (remaining == 0)
            {
                var group = await _context.InstallmentGroups.FindAsync(entity.InstallmentGroupId.Value);
                if (group != null)
                    _context.InstallmentGroups.Remove(group);
                await _context.SaveChangesAsync();
            }
            return;
        }

        _context.Transactions.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private static TransactionDto MapDto(Transaction t) => new()
    {
        Id = t.Id,
        FamilyId = t.FamilyId,
        FinancialAccountId = t.FinancialAccountId,
        FinancialAccountName = t.FinancialAccount?.Name ?? "",
        CategoryId = t.CategoryId,
        CategoryName = t.Category?.Name ?? "",
        Type = t.Type,
        Description = t.Description,
        NormalizedDescription = t.NormalizedDescription,
        Amount = t.Amount,
        TransactionDate = t.TransactionDate,
        Status = t.Status,
        ExternalId = t.ExternalId,
        ImportId = t.ImportId,
        TransactionHash = t.TransactionHash,
        InstallmentGroupId = t.InstallmentGroupId,
        CurrentInstallment = t.CurrentInstallment,
        TotalInstallments = t.TotalInstallments,
        Observation = t.Observation,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}