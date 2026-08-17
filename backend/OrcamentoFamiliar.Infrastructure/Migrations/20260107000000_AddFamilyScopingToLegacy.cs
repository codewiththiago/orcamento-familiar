using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrcamentoFamiliar.Infrastructure.Data;

#nullable disable

namespace OrcamentoFamiliar.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260107000000_AddFamilyScopingToLegacy")]
    public partial class AddFamilyScopingToLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure a default family exists so legacy rows can be backfilled
            // (on fresh installs the seed runs after migrations).
            migrationBuilder.Sql(
                "INSERT INTO \"Families\" (\"Id\", \"Name\", \"CreatedAt\") " +
                "SELECT gen_random_uuid(), 'Minha Família', now() " +
                "WHERE NOT EXISTS (SELECT 1 FROM \"Families\");");

            // ---- Families: dono ----
            migrationBuilder.AddColumn<string>(name: "OwnerUserId", table: "Families", type: "text", nullable: true);

            // ---- MonthlyBudgets ----
            migrationBuilder.AddColumn<Guid>(name: "FamilyId", table: "MonthlyBudgets", type: "uuid", nullable: true);
            migrationBuilder.Sql(
                "UPDATE \"MonthlyBudgets\" SET \"FamilyId\" = (SELECT \"Id\" FROM \"Families\" ORDER BY \"CreatedAt\" LIMIT 1);");
            migrationBuilder.AlterColumn<Guid>(name: "FamilyId", table: "MonthlyBudgets", type: "uuid", nullable: false, defaultValue: Guid.Empty);
            migrationBuilder.DropIndex(name: "IX_MonthlyBudgets_Year_Month", table: "MonthlyBudgets");
            migrationBuilder.CreateIndex(
                name: "IX_MonthlyBudgets_FamilyId_Year_Month",
                table: "MonthlyBudgets",
                columns: new[] { "FamilyId", "Year", "Month" },
                unique: true);
            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyBudgets_Families_FamilyId",
                table: "MonthlyBudgets",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ---- Cards ----
            migrationBuilder.AddColumn<Guid>(name: "FamilyId", table: "Cards", type: "uuid", nullable: true);
            migrationBuilder.Sql(
                "UPDATE \"Cards\" SET \"FamilyId\" = (SELECT \"Id\" FROM \"Families\" ORDER BY \"CreatedAt\" LIMIT 1);");
            migrationBuilder.AlterColumn<Guid>(name: "FamilyId", table: "Cards", type: "uuid", nullable: false, defaultValue: Guid.Empty);
            migrationBuilder.CreateIndex(name: "IX_Cards_FamilyId", table: "Cards", column: "FamilyId");
            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Families_FamilyId",
                table: "Cards",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ---- Categories ----
            migrationBuilder.AddColumn<Guid>(name: "FamilyId", table: "Categories", type: "uuid", nullable: true);
            migrationBuilder.Sql(
                "UPDATE \"Categories\" SET \"FamilyId\" = (SELECT \"Id\" FROM \"Families\" ORDER BY \"CreatedAt\" LIMIT 1);");
            migrationBuilder.AlterColumn<Guid>(name: "FamilyId", table: "Categories", type: "uuid", nullable: false, defaultValue: Guid.Empty);
            migrationBuilder.CreateIndex(name: "IX_Categories_FamilyId", table: "Categories", column: "FamilyId");
            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Families_FamilyId",
                table: "Categories",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ---- FamilyAccess ----
            migrationBuilder.AddColumn<Guid>(name: "FamilyId", table: "FamilyAccess", type: "uuid", nullable: true);
            migrationBuilder.Sql(
                "UPDATE \"FamilyAccess\" SET \"FamilyId\" = (SELECT \"Id\" FROM \"Families\" ORDER BY \"CreatedAt\" LIMIT 1);");
            migrationBuilder.AlterColumn<Guid>(name: "FamilyId", table: "FamilyAccess", type: "uuid", nullable: false, defaultValue: Guid.Empty);
            migrationBuilder.CreateIndex(name: "IX_FamilyAccess_FamilyId", table: "FamilyAccess", column: "FamilyId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_FamilyAccess_InviteCode", table: "FamilyAccess", column: "InviteCode", unique: true);
            migrationBuilder.AddForeignKey(
                name: "FK_FamilyAccess_Families_FamilyId",
                table: "FamilyAccess",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_MonthlyBudgets_Families_FamilyId", table: "MonthlyBudgets");
            migrationBuilder.DropForeignKey(name: "FK_Cards_Families_FamilyId", table: "Cards");
            migrationBuilder.DropForeignKey(name: "FK_Categories_Families_FamilyId", table: "Categories");
            migrationBuilder.DropForeignKey(name: "FK_FamilyAccess_Families_FamilyId", table: "FamilyAccess");
            migrationBuilder.DropIndex(name: "IX_Cards_FamilyId", table: "Cards");
            migrationBuilder.DropIndex(name: "IX_Categories_FamilyId", table: "Categories");
            migrationBuilder.DropIndex(name: "IX_FamilyAccess_FamilyId", table: "FamilyAccess");
            migrationBuilder.DropIndex(name: "IX_FamilyAccess_InviteCode", table: "FamilyAccess");
            migrationBuilder.DropColumn(name: "FamilyId", table: "FamilyAccess");
            migrationBuilder.DropColumn(name: "FamilyId", table: "Categories");
            migrationBuilder.DropColumn(name: "FamilyId", table: "Cards");
            migrationBuilder.DropColumn(name: "FamilyId", table: "MonthlyBudgets");
            migrationBuilder.DropColumn(name: "OwnerUserId", table: "Families");
            migrationBuilder.DropIndex(name: "IX_MonthlyBudgets_FamilyId_Year_Month", table: "MonthlyBudgets");
            migrationBuilder.CreateIndex(name: "IX_MonthlyBudgets_Year_Month", table: "MonthlyBudgets", columns: new[] { "Year", "Month" }, unique: true);
        }
    }
}