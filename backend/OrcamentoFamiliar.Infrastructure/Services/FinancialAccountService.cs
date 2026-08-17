using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Accounts;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class FinancialAccountService : IFinancialAccountService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;

    public FinancialAccountService(AppDbContext context, ICurrentFamily currentFamily)
    {
        _context = context;
        _currentFamily = currentFamily;
    }

    public async Task<List<FinancialAccountDto>> GetAllAsync()
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var accounts = await _context.FinancialAccounts
            .AsNoTracking()
            .Where(a => a.FamilyId == familyId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        var balances = await ComputeBalancesAsync(accounts.Select(a => a.Id).ToList());

        return accounts.Select(a => Map(a, balances.GetValueOrDefault(a.Id))).ToList();
    }

    public async Task<FinancialAccountDto> CreateAsync(CreateFinancialAccountDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var entity = new FinancialAccount
        {
            FamilyId = familyId,
            Name = dto.Name.Trim(),
            Institution = dto.Institution?.Trim(),
            Type = dto.Type,
            OwnerUserId = dto.OwnerUserId,
            InitialBalance = dto.InitialBalance,
            Active = dto.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FinancialAccounts.Add(entity);
        await _context.SaveChangesAsync();

        return Map(entity, entity.InitialBalance);
    }

    public async Task<FinancialAccountDto> UpdateAsync(int id, UpdateFinancialAccountDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var entity = await _context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Conta não encontrada");

        entity.Name = dto.Name.Trim();
        entity.Institution = dto.Institution?.Trim();
        entity.Type = dto.Type;
        entity.OwnerUserId = dto.OwnerUserId;
        entity.InitialBalance = dto.InitialBalance;
        entity.Active = dto.Active;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var balance = await ComputeBalanceAsync(id);
        return Map(entity, balance);
    }

    public async Task DeleteAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var entity = await _context.FinancialAccounts
            .FirstOrDefaultAsync(a => a.Id == id && a.FamilyId == familyId)
            ?? throw new KeyNotFoundException("Conta não encontrada");

        var hasTransactions = await _context.Transactions.AnyAsync(t => t.FinancialAccountId == id);
        if (hasTransactions)
            throw new InvalidOperationException("Não é possível excluir uma conta que possui transações.");

        var hasImports = await _context.Imports.AnyAsync(i => i.FinancialAccountId == id);
        if (hasImports)
            throw new InvalidOperationException("Não é possível excluir uma conta com histórico de importações.");

        _context.FinancialAccounts.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private async Task<Dictionary<int, decimal>> ComputeBalancesAsync(List<int> accountIds)
    {
        if (accountIds.Count == 0) return new Dictionary<int, decimal>();

        var groups = await _context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.FinancialAccountId) && t.Status == TransactionStatus.Confirmed)
            .GroupBy(t => t.FinancialAccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Income = g.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount),
                Expense = g.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount)
            })
            .ToListAsync();

        return groups.ToDictionary(g => g.AccountId, g => g.Income - g.Expense);
    }

    private async Task<decimal> ComputeBalanceAsync(int accountId)
    {
        var income = await _context.Transactions
            .Where(t => t.FinancialAccountId == accountId && t.Status == TransactionStatus.Confirmed && t.Type == TransactionType.Income)
            .SumAsync(t => t.Amount);
        var expense = await _context.Transactions
            .Where(t => t.FinancialAccountId == accountId && t.Status == TransactionStatus.Confirmed && t.Type == TransactionType.Expense)
            .SumAsync(t => t.Amount);
        return income - expense;
    }

    private static FinancialAccountDto Map(FinancialAccount a, decimal movementBalance) => new()
    {
        Id = a.Id,
        FamilyId = a.FamilyId,
        Name = a.Name,
        Institution = a.Institution,
        Type = a.Type,
        OwnerUserId = a.OwnerUserId,
        InitialBalance = a.InitialBalance,
        Active = a.Active,
        Balance = a.InitialBalance + movementBalance,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}