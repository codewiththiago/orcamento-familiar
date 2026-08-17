using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Budget;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;
    private static readonly string[] MonthNames =
        ["Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
         "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];

    public BudgetService(AppDbContext context, ICurrentFamily currentFamily)
    {
        _context = context;
        _currentFamily = currentFamily;
    }

    public async Task<DashboardDto> GetDashboardAsync(int year)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();

        var budgets = await _context.MonthlyBudgets
            .Include(b => b.ExtraIncomes)
            .Include(b => b.FixedExpenses)
            .Include(b => b.CreditCardLaunches)
            .Where(b => b.FamilyId == familyId && b.Year == year)
            .ToListAsync();

        var months = Enumerable.Range(1, 12).Select(m =>
        {
            var budget = budgets.FirstOrDefault(b => b.Month == m);
            if (budget == null)
                return new MonthSummaryDto { Month = m, MonthName = MonthNames[m - 1] };

            var totalIncome = budget.Salary1 + budget.Salary2
                + budget.ExtraIncomes.Sum(e => e.Value);
            var plannedExpense = budget.FixedExpenses.Sum(e => e.PlannedValue)
                + budget.CreditCardLaunches.Sum(e => e.Value);
            var actualExpense = budget.FixedExpenses.Sum(e => e.ActualValue)
                + budget.CreditCardLaunches.Sum(e => e.Value);

            return new MonthSummaryDto
            {
                Month = m,
                MonthName = MonthNames[m - 1],
                TotalIncome = totalIncome,
                PlannedExpense = plannedExpense,
                ActualExpense = actualExpense,
                PlannedBalance = totalIncome - plannedExpense,
                ActualBalance = totalIncome - actualExpense,
                HasData = true
            };
        }).ToList();

        return new DashboardDto { Year = year, Months = months };
    }

    public async Task<MonthlyBudgetDto> GetOrCreateMonthlyBudgetAsync(int year, int month)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var budget = await GetFamilyBudgetAsync(familyId, year, month);

        if (budget == null)
        {
            budget = new MonthlyBudget { FamilyId = familyId, Year = year, Month = month };

            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;
            var prevBudget = await GetFamilyBudgetAsync(familyId, prevYear, prevMonth);

            if (prevBudget != null)
            {
                foreach (var fe in prevBudget.FixedExpenses)
                {
                    budget.FixedExpenses.Add(new FixedExpense
                    {
                        Name = fe.Name,
                        InstallmentType = fe.InstallmentType,
                        PlannedValue = fe.PlannedValue,
                        ActualValue = 0,
                        CategoryId = fe.CategoryId
                    });
                }
            }

            _context.MonthlyBudgets.Add(budget);
            await _context.SaveChangesAsync();
            budget = await GetFamilyBudgetAsync(familyId, year, month);
        }

        return MapToDto(budget!);
    }

    public async Task<MonthlyBudgetDto> UpdateSalaryAsync(int year, int month, UpdateSalaryDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var budget = await GetFamilyBudgetAsync(familyId, year, month)
            ?? throw new KeyNotFoundException("Budget not found");
        budget.Salary1 = dto.Salary1;
        budget.Salary2 = dto.Salary2;
        await _context.SaveChangesAsync();
        return MapToDto(budget);
    }

    public async Task<ExtraIncomeDto> AddExtraIncomeAsync(CreateExtraIncomeDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        await EnsureFamilyBudgetAsync(familyId, dto.MonthlyBudgetId);

        var entity = new ExtraIncome { MonthlyBudgetId = dto.MonthlyBudgetId, Description = dto.Description, Value = dto.Value };
        _context.ExtraIncomes.Add(entity);
        await _context.SaveChangesAsync();
        return new ExtraIncomeDto { Id = entity.Id, MonthlyBudgetId = entity.MonthlyBudgetId, Description = entity.Description, Value = entity.Value };
    }

    public async Task<ExtraIncomeDto> UpdateExtraIncomeAsync(int id, UpdateExtraIncomeDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var entity = await _context.ExtraIncomes.FindAsync(id) ?? throw new KeyNotFoundException();
        await EnsureFamilyBudgetAsync(familyId, entity.MonthlyBudgetId);

        entity.Description = dto.Description;
        entity.Value = dto.Value;
        await _context.SaveChangesAsync();
        return new ExtraIncomeDto { Id = entity.Id, MonthlyBudgetId = entity.MonthlyBudgetId, Description = entity.Description, Value = entity.Value };
    }

    public async Task DeleteExtraIncomeAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var entity = await _context.ExtraIncomes.FindAsync(id) ?? throw new KeyNotFoundException();
        await EnsureFamilyBudgetAsync(familyId, entity.MonthlyBudgetId);
        _context.ExtraIncomes.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<FixedExpenseDto> AddFixedExpenseAsync(CreateFixedExpenseDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        await EnsureFamilyBudgetAsync(familyId, dto.MonthlyBudgetId);
        await EnsureFamilyCategoryAsync(familyId, dto.CategoryId);

        var entity = new FixedExpense
        {
            MonthlyBudgetId = dto.MonthlyBudgetId, Name = dto.Name,
            InstallmentType = dto.InstallmentType, PlannedValue = dto.PlannedValue,
            ActualValue = dto.ActualValue, CategoryId = dto.CategoryId
        };
        _context.FixedExpenses.Add(entity);
        await _context.SaveChangesAsync();
        return await MapFixedExpenseDtoAsync(entity);
    }

    public async Task<FixedExpenseDto> UpdateFixedExpenseAsync(int id, UpdateFixedExpenseDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var entity = await _context.FixedExpenses.FindAsync(id) ?? throw new KeyNotFoundException();
        await EnsureFamilyBudgetAsync(familyId, entity.MonthlyBudgetId);
        await EnsureFamilyCategoryAsync(familyId, dto.CategoryId);

        entity.Name = dto.Name;
        entity.InstallmentType = dto.InstallmentType;
        entity.PlannedValue = dto.PlannedValue;
        entity.ActualValue = dto.ActualValue;
        entity.CategoryId = dto.CategoryId;
        await _context.SaveChangesAsync();
        return await MapFixedExpenseDtoAsync(entity);
    }

    public async Task DeleteFixedExpenseAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var entity = await _context.FixedExpenses.FindAsync(id) ?? throw new KeyNotFoundException();
        await EnsureFamilyBudgetAsync(familyId, entity.MonthlyBudgetId);
        _context.FixedExpenses.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<CreditCardLaunchDto>> AddCreditCardLaunchAsync(CreateCreditCardLaunchDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var groupId = dto.TotalInstallments > 1 ? Guid.NewGuid() : (Guid?)null;

        var budget = await GetFamilyBudgetAsync(familyId, dto.MonthlyBudgetId)
            ?? throw new KeyNotFoundException("Budget not found");
        await EnsureFamilyCategoryAsync(familyId, dto.CategoryId);

        for (int i = 1; i <= dto.TotalInstallments; i++)
        {
            var targetYear = budget.Year;
            var targetMonth = budget.Month + (i - 1);
            while (targetMonth > 12) { targetMonth -= 12; targetYear++; }

            var targetBudget = i == 1 ? budget
                : await GetFamilyBudgetAsync(familyId, targetYear, targetMonth);

            if (targetBudget == null)
            {
                targetBudget = new MonthlyBudget { FamilyId = familyId, Year = targetYear, Month = targetMonth };
                _context.MonthlyBudgets.Add(targetBudget);
                await _context.SaveChangesAsync();
            }

            _context.CreditCardLaunches.Add(new CreditCardLaunch
            {
                MonthlyBudgetId = targetBudget.Id,
                Description = dto.Description,
                CardId = dto.CardId,
                Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc),
                CategoryId = dto.CategoryId,
                CurrentInstallment = i,
                TotalInstallments = dto.TotalInstallments,
                Value = dto.Value,
                Observation = dto.Observation,
                GroupId = groupId
            });
        }

        await _context.SaveChangesAsync();

        var created = await _context.CreditCardLaunches
            .Include(x => x.Card).Include(x => x.Category)
            .Where(x => x.MonthlyBudgetId == dto.MonthlyBudgetId && (groupId == null || x.GroupId == groupId))
            .OrderBy(x => x.CurrentInstallment)
            .ToListAsync();

        return created.Select(MapLaunchDto).ToList();
    }

    public async Task<CreditCardLaunchDto> UpdateCreditCardLaunchAsync(int id, UpdateCreditCardLaunchDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var entity = await _context.CreditCardLaunches
            .Include(x => x.Card).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        await EnsureFamilyBudgetAsync(familyId, entity.MonthlyBudgetId);
        await EnsureFamilyCategoryAsync(familyId, dto.CategoryId);

        entity.Description = dto.Description;
        entity.CardId = dto.CardId;
        entity.Date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        entity.CategoryId = dto.CategoryId;
        entity.Value = dto.Value;
        entity.Observation = dto.Observation;
        await _context.SaveChangesAsync();

        await _context.Entry(entity).Reference(x => x.Card).LoadAsync();
        await _context.Entry(entity).Reference(x => x.Category).LoadAsync();
        return MapLaunchDto(entity);
    }

    public async Task DeleteCreditCardLaunchAsync(int id, bool deleteAll = false)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var entity = await _context.CreditCardLaunches.FindAsync(id) ?? throw new KeyNotFoundException();
        await EnsureFamilyBudgetAsync(familyId, entity.MonthlyBudgetId);

        if (deleteAll && entity.GroupId.HasValue)
        {
            var siblings = await _context.CreditCardLaunches
                .Where(x => x.GroupId == entity.GroupId && x.CurrentInstallment >= entity.CurrentInstallment)
                .ToListAsync();
            _context.CreditCardLaunches.RemoveRange(siblings);
        }
        else
        {
            _context.CreditCardLaunches.Remove(entity);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<CategorySummaryDto>> GetCategorySummaryAsync(int budgetId)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        await EnsureFamilyBudgetAsync(familyId, budgetId);

        var fixedByCategory = await _context.FixedExpenses
            .Include(x => x.Category)
            .Where(x => x.MonthlyBudgetId == budgetId)
            .GroupBy(x => new { x.CategoryId, x.Category.Name })
            .Select(g => new { g.Key.CategoryId, g.Key.Name, Total = g.Sum(x => x.ActualValue) })
            .ToListAsync();

        var cardsByCategory = await _context.CreditCardLaunches
            .Include(x => x.Category)
            .Where(x => x.MonthlyBudgetId == budgetId)
            .GroupBy(x => new { x.CategoryId, x.Category.Name })
            .Select(g => new { g.Key.CategoryId, g.Key.Name, Total = g.Sum(x => x.Value) })
            .ToListAsync();

        return fixedByCategory
            .Concat(cardsByCategory)
            .GroupBy(x => new { x.CategoryId, x.Name })
            .Select(g => new CategorySummaryDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Total = g.Sum(x => x.Total)
            })
            .Where(x => x.Total > 0)
            .OrderByDescending(x => x.Total)
            .ToList();
    }

    private async Task<MonthlyBudget?> GetFamilyBudgetAsync(Guid familyId, int budgetId) =>
        await _context.MonthlyBudgets
            .Include(b => b.ExtraIncomes)
            .Include(b => b.FixedExpenses).ThenInclude(f => f.Category)
            .Include(b => b.CreditCardLaunches).ThenInclude(c => c.Card)
            .Include(b => b.CreditCardLaunches).ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(b => b.Id == budgetId && b.FamilyId == familyId);

    private async Task<MonthlyBudget?> GetFamilyBudgetAsync(Guid familyId, int year, int month) =>
        await _context.MonthlyBudgets
            .Include(b => b.ExtraIncomes)
            .Include(b => b.FixedExpenses).ThenInclude(f => f.Category)
            .Include(b => b.CreditCardLaunches).ThenInclude(c => c.Card)
            .Include(b => b.CreditCardLaunches).ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(b => b.FamilyId == familyId && b.Year == year && b.Month == month);

    private async Task EnsureFamilyBudgetAsync(Guid familyId, int budgetId)
    {
        var exists = await _context.MonthlyBudgets.AnyAsync(b => b.Id == budgetId && b.FamilyId == familyId);
        if (!exists) throw new KeyNotFoundException("Budget not found");
    }

    private async Task EnsureFamilyCategoryAsync(Guid familyId, int categoryId)
    {
        var exists = await _context.Categories.AnyAsync(c => c.Id == categoryId && c.FamilyId == familyId);
        if (!exists) throw new KeyNotFoundException("Categoria não encontrada");
    }

    private static MonthlyBudgetDto MapToDto(MonthlyBudget b) => new()
    {
        Id = b.Id, Year = b.Year, Month = b.Month,
        Salary1 = b.Salary1, Salary2 = b.Salary2,
        ExtraIncomes = b.ExtraIncomes.Select(e => new ExtraIncomeDto { Id = e.Id, MonthlyBudgetId = e.MonthlyBudgetId, Description = e.Description, Value = e.Value }).ToList(),
        FixedExpenses = b.FixedExpenses.Select(e => new FixedExpenseDto
        {
            Id = e.Id, MonthlyBudgetId = e.MonthlyBudgetId, Name = e.Name,
            InstallmentType = e.InstallmentType, PlannedValue = e.PlannedValue,
            ActualValue = e.ActualValue, CategoryId = e.CategoryId, CategoryName = e.Category.Name
        }).ToList(),
        CreditCardLaunches = b.CreditCardLaunches.Select(MapLaunchDto).ToList(),
    };

    private static CreditCardLaunchDto MapLaunchDto(CreditCardLaunch c) => new()
    {
        Id = c.Id, MonthlyBudgetId = c.MonthlyBudgetId, Description = c.Description,
        CardId = c.CardId, CardName = c.Card?.Name ?? "",
        Date = c.Date, CategoryId = c.CategoryId, CategoryName = c.Category?.Name ?? "",
        CurrentInstallment = c.CurrentInstallment, TotalInstallments = c.TotalInstallments,
        Value = c.Value, Observation = c.Observation, GroupId = c.GroupId
    };

    private async Task<FixedExpenseDto> MapFixedExpenseDtoAsync(FixedExpense e)
    {
        var category = await _context.Categories.FindAsync(e.CategoryId);
        return new FixedExpenseDto
        {
            Id = e.Id, MonthlyBudgetId = e.MonthlyBudgetId, Name = e.Name,
            InstallmentType = e.InstallmentType, PlannedValue = e.PlannedValue,
            ActualValue = e.ActualValue, CategoryId = e.CategoryId, CategoryName = category?.Name ?? ""
        };
    }
}