using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Card;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class CardService : ICardService
{
    private readonly AppDbContext _context;
    private readonly ICurrentFamily _currentFamily;
    public CardService(AppDbContext context, ICurrentFamily currentFamily)
    {
        _context = context;
        _currentFamily = currentFamily;
    }

    public async Task<List<CardDto>> GetAllAsync(int? year = null, int? month = null)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var cards = await _context.Cards
            .Where(c => c.FamilyId == familyId)
            .ToListAsync();
        var usageDict = new Dictionary<int, decimal>();

        if (year.HasValue && month.HasValue)
        {
            var budget = await _context.MonthlyBudgets
                .Include(b => b.CreditCardLaunches)
                .FirstOrDefaultAsync(b => b.FamilyId == familyId && b.Year == year && b.Month == month);

            if (budget != null)
                usageDict = budget.CreditCardLaunches.GroupBy(x => x.CardId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));
        }

        var prepaidIds = cards.Where(c => c.CardType == CardType.Prepaid).Select(c => c.Id).ToList();
        var allTimeSpending = prepaidIds.Count > 0
            ? (await _context.CreditCardLaunches
                .Where(l => prepaidIds.Contains(l.CardId))
                .GroupBy(l => l.CardId)
                .Select(g => new { CardId = g.Key, Total = g.Sum(l => l.Value) })
                .ToListAsync())
                .ToDictionary(x => x.CardId, x => x.Total)
            : new Dictionary<int, decimal>();

        var now = DateTime.UtcNow;

        return cards.Select(c =>
        {
            decimal? currentBalance = null;
            if (c.CardType == CardType.Prepaid && c.MonthlyCredit.HasValue
                && c.CreditSinceYear.HasValue && c.CreditSinceMonth.HasValue)
            {
                var months = (now.Year - c.CreditSinceYear.Value) * 12
                    + now.Month - c.CreditSinceMonth.Value + 1;
                months = Math.Max(0, months);
                var totalCredits = (c.InitialBalance ?? 0) + months * c.MonthlyCredit.Value;
                var totalSpent = allTimeSpending.GetValueOrDefault(c.Id, 0);
                currentBalance = totalCredits - totalSpent;
            }

            return new CardDto
            {
                Id = c.Id, Name = c.Name, CardType = c.CardType,
                Limit = c.Limit, ClosingDay = c.ClosingDay, DueDay = c.DueDay,
                MonthlyGoal = c.MonthlyGoal,
                CurrentMonthUsage = usageDict.GetValueOrDefault(c.Id, 0),
                MonthlyCredit = c.MonthlyCredit,
                CreditSinceYear = c.CreditSinceYear,
                CreditSinceMonth = c.CreditSinceMonth,
                InitialBalance = c.InitialBalance,
                CurrentBalance = currentBalance,
            };
        }).ToList();
    }

    public async Task<CardDto> GetByIdAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var c = await _context.Cards.FirstOrDefaultAsync(x => x.Id == id && x.FamilyId == familyId)
            ?? throw new KeyNotFoundException();
        return new CardDto
        {
            Id = c.Id, Name = c.Name, CardType = c.CardType,
            Limit = c.Limit, ClosingDay = c.ClosingDay, DueDay = c.DueDay,
            MonthlyGoal = c.MonthlyGoal, MonthlyCredit = c.MonthlyCredit,
            CreditSinceYear = c.CreditSinceYear, CreditSinceMonth = c.CreditSinceMonth,
            InitialBalance = c.InitialBalance,
        };
    }

    public async Task<CardDto> CreateAsync(CreateCardDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var card = new Card
        {
            FamilyId = familyId,
            Name = dto.Name, CardType = dto.CardType,
            Limit = dto.Limit, ClosingDay = dto.ClosingDay, DueDay = dto.DueDay,
            MonthlyGoal = dto.MonthlyGoal, MonthlyCredit = dto.MonthlyCredit,
            CreditSinceYear = dto.CreditSinceYear, CreditSinceMonth = dto.CreditSinceMonth,
            InitialBalance = dto.InitialBalance,
        };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return new CardDto
        {
            Id = card.Id, Name = card.Name, CardType = card.CardType,
            Limit = card.Limit, ClosingDay = card.ClosingDay, DueDay = card.DueDay,
            MonthlyGoal = card.MonthlyGoal, MonthlyCredit = card.MonthlyCredit,
            CreditSinceYear = card.CreditSinceYear, CreditSinceMonth = card.CreditSinceMonth,
            InitialBalance = card.InitialBalance, CurrentBalance = dto.InitialBalance,
        };
    }

    public async Task<CardDto> UpdateAsync(int id, UpdateCardDto dto)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var card = await _context.Cards.FirstOrDefaultAsync(x => x.Id == id && x.FamilyId == familyId)
            ?? throw new KeyNotFoundException();
        card.Name = dto.Name; card.CardType = dto.CardType;
        card.Limit = dto.Limit; card.ClosingDay = dto.ClosingDay; card.DueDay = dto.DueDay;
        card.MonthlyGoal = dto.MonthlyGoal; card.MonthlyCredit = dto.MonthlyCredit;
        card.CreditSinceYear = dto.CreditSinceYear; card.CreditSinceMonth = dto.CreditSinceMonth;
        card.InitialBalance = dto.InitialBalance;
        await _context.SaveChangesAsync();
        return new CardDto
        {
            Id = card.Id, Name = card.Name, CardType = card.CardType,
            Limit = card.Limit, ClosingDay = card.ClosingDay, DueDay = card.DueDay,
            MonthlyGoal = card.MonthlyGoal, MonthlyCredit = card.MonthlyCredit,
            CreditSinceYear = card.CreditSinceYear, CreditSinceMonth = card.CreditSinceMonth,
            InitialBalance = card.InitialBalance,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var familyId = await _currentFamily.GetFamilyIdAsync();
        var card = await _context.Cards.FirstOrDefaultAsync(x => x.Id == id && x.FamilyId == familyId)
            ?? throw new KeyNotFoundException();
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
    }
}