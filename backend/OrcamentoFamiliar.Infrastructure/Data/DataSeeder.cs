using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Domain.Entities;

namespace OrcamentoFamiliar.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        await SeedCategoriesAsync(context);
        await SeedCardsAsync(context);
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

}
