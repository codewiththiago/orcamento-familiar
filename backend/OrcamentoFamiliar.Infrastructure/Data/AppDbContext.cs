using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrcamentoFamiliar.Domain.Entities;

namespace OrcamentoFamiliar.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<MonthlyBudget> MonthlyBudgets => Set<MonthlyBudget>();
    public DbSet<ExtraIncome> ExtraIncomes => Set<ExtraIncome>();
    public DbSet<FixedExpense> FixedExpenses => Set<FixedExpense>();
    public DbSet<CreditCardLaunch> CreditCardLaunches => Set<CreditCardLaunch>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MonthlyBudget>(e =>
        {
            e.HasIndex(x => new { x.Year, x.Month }).IsUnique();
            e.Property(x => x.SalaryThiago).HasPrecision(18, 2);
            e.Property(x => x.SalaryJuh).HasPrecision(18, 2);
        });

        builder.Entity<ExtraIncome>(e =>
        {
            e.Property(x => x.Value).HasPrecision(18, 2);
        });

        builder.Entity<FixedExpense>(e =>
        {
            e.Property(x => x.PlannedValue).HasPrecision(18, 2);
            e.Property(x => x.ActualValue).HasPrecision(18, 2);
        });

        builder.Entity<CreditCardLaunch>(e =>
        {
            e.Property(x => x.Value).HasPrecision(18, 2);
        });

        builder.Entity<Card>(e =>
        {
            e.Property(x => x.Limit).HasPrecision(18, 2);
            e.Property(x => x.MonthlyGoal).HasPrecision(18, 2);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasOne(x => x.User)
             .WithMany(x => x.RefreshTokens)
             .HasForeignKey(x => x.UserId);
        });
    }
}
