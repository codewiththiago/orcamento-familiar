using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Domain.Entities;
using OrcamentoFamiliar.Domain.Enums;

namespace OrcamentoFamiliar.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        await SeedFamilyAsync(context);
        await SeedCategoriesAsync(context);
        await SeedCardsAsync(context);
        await SeedFinancialAccountsAsync(context);
    }

    private static async Task SeedFamilyAsync(AppDbContext context)
    {
        var family = await context.Families.OrderBy(f => f.CreatedAt).FirstOrDefaultAsync();
        if (family == null)
        {
            family = new Family { Id = Guid.NewGuid(), Name = "Minha Família", CreatedAt = DateTime.UtcNow };
            context.Families.Add(family);
            await context.SaveChangesAsync();
        }

        var usersWithoutFamily = await context.Users.Where(u => u.FamilyId == null).ToListAsync();
        foreach (var user in usersWithoutFamily)
            user.FamilyId = family.Id;

        if (usersWithoutFamily.Count > 0)
            await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        var categories = new[]
        {
            "Alimentação", "Casa", "Comunicação", "Educação", "Impostos",
            "Saúde", "Pets", "Energia", "Água", "Transporte", "Lazer",
            "Assinaturas", "Serviços", "Seguros", "Dívidas/Parcelamentos",
            "Investimentos", "Cartões (pagamento)", "Outros"
        };

        foreach (var name in categories)
            context.Categories.Add(new Category { Name = name });

        await context.SaveChangesAsync();
    }

    private static async Task SeedCardsAsync(AppDbContext context)
    {
        if (await context.Cards.AnyAsync()) return;

        context.Cards.AddRange(
            new Card { Name = "C6", Limit = 1400, ClosingDay = 10, DueDay = 15, MonthlyGoal = 1400 },
            new Card { Name = "PicPay", Limit = null, ClosingDay = 8, DueDay = 15, MonthlyGoal = null },
            new Card { Name = "Nubank", Limit = null, ClosingDay = 8, DueDay = 15, MonthlyGoal = null },
            new Card { Name = "VR", Limit = 500, ClosingDay = 25, DueDay = 15, MonthlyGoal = 500 }
        );

        await context.SaveChangesAsync();
    }

    private static async Task SeedFinancialAccountsAsync(AppDbContext context)
    {
        if (await context.FinancialAccounts.AnyAsync()) return;

        var family = await context.Families.OrderBy(f => f.CreatedAt).FirstOrDefaultAsync();
        if (family == null) return;

        context.FinancialAccounts.AddRange(
            new FinancialAccount { FamilyId = family.Id, Name = "C6", Institution = "C6 Bank", Type = FinancialAccountType.CheckingAccount },
            new FinancialAccount { FamilyId = family.Id, Name = "PicPay", Institution = "PicPay", Type = FinancialAccountType.DigitalWallet },
            new FinancialAccount { FamilyId = family.Id, Name = "Nubank", Institution = "Nubank", Type = FinancialAccountType.DigitalWallet }
        );

        await context.SaveChangesAsync();
    }
}