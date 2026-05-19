using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Application.DTOs.Card;
using OrcamentoFamiliar.Application.Interfaces;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Infrastructure.Data;

namespace OrcamentoFamiliar.Infrastructure.Services;

public class CardService : ICardService
{
    private readonly AppDbContext _context;
    public CardService(AppDbContext context) => _context = context;

    public async Task<List<CardDto>> GetAllAsync(int? year = null, int? month = null)
    {
        var cards = await _context.Cards.ToListAsync();
        var usageDict = new Dictionary<int, decimal>();

        if (year.HasValue && month.HasValue)
        {
            var budget = await _context.MonthlyBudgets
                .Include(b => b.CreditCardLaunches)
                .FirstOrDefaultAsync(b => b.Year == year && b.Month == month);

            if (budget != null)
                usageDict = budget.CreditCardLaunches.GroupBy(x => x.CardId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));
        }

        return cards.Select(c => new CardDto
        {
            Id = c.Id, Name = c.Name, Limit = c.Limit,
            ClosingDay = c.ClosingDay, DueDay = c.DueDay, MonthlyGoal = c.MonthlyGoal,
            CurrentMonthUsage = usageDict.GetValueOrDefault(c.Id, 0)
        }).ToList();
    }

    public async Task<CardDto> GetByIdAsync(int id)
    {
        var card = await _context.Cards.FindAsync(id) ?? throw new KeyNotFoundException();
        return new CardDto { Id = card.Id, Name = card.Name, Limit = card.Limit, ClosingDay = card.ClosingDay, DueDay = card.DueDay, MonthlyGoal = card.MonthlyGoal };
    }

    public async Task<CardDto> CreateAsync(CreateCardDto dto)
    {
        var card = new Card { Name = dto.Name, Limit = dto.Limit, ClosingDay = dto.ClosingDay, DueDay = dto.DueDay, MonthlyGoal = dto.MonthlyGoal };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return new CardDto { Id = card.Id, Name = card.Name, Limit = card.Limit, ClosingDay = card.ClosingDay, DueDay = card.DueDay, MonthlyGoal = card.MonthlyGoal };
    }

    public async Task<CardDto> UpdateAsync(int id, UpdateCardDto dto)
    {
        var card = await _context.Cards.FindAsync(id) ?? throw new KeyNotFoundException();
        card.Name = dto.Name; card.Limit = dto.Limit;
        card.ClosingDay = dto.ClosingDay; card.DueDay = dto.DueDay; card.MonthlyGoal = dto.MonthlyGoal;
        await _context.SaveChangesAsync();
        return new CardDto { Id = card.Id, Name = card.Name, Limit = card.Limit, ClosingDay = card.ClosingDay, DueDay = card.DueDay, MonthlyGoal = card.MonthlyGoal };
    }

    public async Task DeleteAsync(int id)
    {
        var card = await _context.Cards.FindAsync(id) ?? throw new KeyNotFoundException();
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
    }
}
