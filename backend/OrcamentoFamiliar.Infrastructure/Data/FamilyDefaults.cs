using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Infrastructure.Data;

public static class FamilyDefaults
{
    public static async Task EnsureFamilyDefaultsAsync(AppDbContext context, Guid familyId)
    {
        await EnsureCategoriesAsync(context, familyId);
        await EnsureCardsAsync(context, familyId);
        await EnsureFinancialAccountsAsync(context, familyId);
    }

    public static async Task EnsureCategoriesAsync(AppDbContext context, Guid familyId)
    {
        if (await context.Categories.AnyAsync(c => c.FamilyId == familyId)) return;

        var categories = new[]
        {
            "Alimentação", "Casa", "Comunicação", "Educação", "Impostos",
            "Saúde", "Pets", "Energia", "Água", "Transporte", "Lazer",
            "Assinaturas", "Serviços", "Seguros", "Dívidas/Parcelamentos",
            "Investimentos", "Cartões (pagamento)", "Outros"
        };

        foreach (var name in categories)
            context.Categories.Add(new Category { FamilyId = familyId, Name = name });

        await context.SaveChangesAsync();
    }

    public static async Task EnsureCardsAsync(AppDbContext context, Guid familyId)
    {
        if (await context.Cards.AnyAsync(c => c.FamilyId == familyId)) return;

        context.Cards.AddRange(
            new Card { FamilyId = familyId, Name = "C6", Limit = 1400, ClosingDay = 10, DueDay = 15, MonthlyGoal = 1400 },
            new Card { FamilyId = familyId, Name = "PicPay", Limit = null, ClosingDay = 8, DueDay = 15, MonthlyGoal = null },
            new Card { FamilyId = familyId, Name = "Nubank", Limit = null, ClosingDay = 8, DueDay = 15, MonthlyGoal = null },
            new Card { FamilyId = familyId, Name = "VR", Limit = 500, ClosingDay = 25, DueDay = 15, MonthlyGoal = 500 }
        );

        await context.SaveChangesAsync();
    }

    public static async Task EnsureFinancialAccountsAsync(AppDbContext context, Guid familyId)
    {
        if (await context.FinancialAccounts.AnyAsync(a => a.FamilyId == familyId)) return;

        context.FinancialAccounts.AddRange(
            new FinancialAccount { FamilyId = familyId, Name = "C6", Institution = "C6 Bank", Type = FinancialAccountType.CheckingAccount },
            new FinancialAccount { FamilyId = familyId, Name = "PicPay", Institution = "PicPay", Type = FinancialAccountType.DigitalWallet },
            new FinancialAccount { FamilyId = familyId, Name = "Nubank", Institution = "Nubank", Type = FinancialAccountType.DigitalWallet }
        );

        await context.SaveChangesAsync();
    }
}