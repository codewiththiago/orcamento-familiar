using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Domain.Entities;

namespace OrcamentoFamiliar.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        await SeedFamilyAsync(context);

        var familyIds = await context.Families.Select(f => f.Id).ToListAsync();
        foreach (var familyId in familyIds)
            await FamilyDefaults.EnsureFamilyDefaultsAsync(context, familyId);
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
}