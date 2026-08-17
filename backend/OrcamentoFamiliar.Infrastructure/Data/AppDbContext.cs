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
    public DbSet<FamilyAccess> FamilyAccess => Set<FamilyAccess>();

    public DbSet<Family> Families => Set<Family>();
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<InstallmentGroup> InstallmentGroups => Set<InstallmentGroup>();
    public DbSet<Import> Imports => Set<Import>();
    public DbSet<CategorizationRule> CategorizationRules => Set<CategorizationRule>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasIndex(x => x.FamilyId);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MonthlyBudget>(e =>
        {
            e.HasIndex(x => new { x.FamilyId, x.Year, x.Month }).IsUnique();
            e.Property(x => x.Salary1).HasPrecision(18, 2);
            e.Property(x => x.Salary2).HasPrecision(18, 2);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
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
            e.HasIndex(x => x.FamilyId);
            e.Property(x => x.Limit).HasPrecision(18, 2);
            e.Property(x => x.MonthlyGoal).HasPrecision(18, 2);
            e.Property(x => x.MonthlyCredit).HasPrecision(18, 2);
            e.Property(x => x.InitialBalance).HasPrecision(18, 2);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(e =>
        {
            e.HasIndex(x => x.FamilyId);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.HasOne(x => x.User)
             .WithMany(x => x.RefreshTokens)
             .HasForeignKey(x => x.UserId);
        });

        builder.Entity<FamilyAccess>(e =>
        {
            e.Property(x => x.InviteCode).HasMaxLength(6);
            e.Property(x => x.Pin).HasMaxLength(4);
            e.HasIndex(x => x.InviteCode).IsUnique();
            e.HasIndex(x => x.FamilyId).IsUnique();
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- New financial model ----

        builder.Entity<FinancialAccount>(e =>
        {
            e.HasIndex(x => x.FamilyId);
            e.Property(x => x.InitialBalance).HasPrecision(18, 2);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InstallmentGroup>(e =>
        {
            e.HasIndex(x => x.FamilyId);
            e.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            e.Property(x => x.InstallmentValue).HasPrecision(18, 2);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FinancialAccount)
             .WithMany()
             .HasForeignKey(x => x.FinancialAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Import>(e =>
        {
            e.HasIndex(x => new { x.FamilyId, x.ImportedAt });
            e.Property(x => x.FileHash).HasMaxLength(128);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FinancialAccount)
             .WithMany()
             .HasForeignKey(x => x.FinancialAccountId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CategorizationRule>(e =>
        {
            e.HasIndex(x => new { x.FamilyId, x.Priority });
            e.HasIndex(x => x.FinancialAccountId);
            e.Property(x => x.Pattern).HasMaxLength(500);
            e.Property(x => x.RuleMatchType).HasColumnName("MatchType");
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FinancialAccount)
             .WithMany()
             .HasForeignKey(x => x.FinancialAccountId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category)
             .WithMany()
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Transaction>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.FamilyId, x.TransactionDate });
            e.HasIndex(x => new { x.FamilyId, x.FinancialAccountId, x.TransactionDate });
            e.HasIndex(x => new { x.FamilyId, x.TransactionHash }).IsUnique();
            e.HasIndex(x => x.FinancialAccountId);
            e.HasIndex(x => x.CategoryId);
            e.HasIndex(x => x.ImportId);
            e.HasIndex(x => x.InstallmentGroupId);
            e.HasOne(x => x.Family)
             .WithMany()
             .HasForeignKey(x => x.FamilyId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.FinancialAccount)
             .WithMany()
             .HasForeignKey(x => x.FinancialAccountId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category)
             .WithMany()
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Import)
             .WithMany(x => x.Transactions)
             .HasForeignKey(x => x.ImportId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.InstallmentGroup)
             .WithMany(x => x.Transactions)
             .HasForeignKey(x => x.InstallmentGroupId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}