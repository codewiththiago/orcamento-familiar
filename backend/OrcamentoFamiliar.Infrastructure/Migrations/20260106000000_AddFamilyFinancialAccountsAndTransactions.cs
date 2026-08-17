using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OrcamentoFamiliar.Infrastructure.Data;

#nullable disable

namespace OrcamentoFamiliar.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260106000000_AddFamilyFinancialAccountsAndTransactions")]
    public partial class AddFamilyFinancialAccountsAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Institution = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<string>(type: "text", nullable: true),
                    InitialBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Imports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImportedByUserId = table.Column<string>(type: "text", nullable: true),
                    TotalRecords = table.Column<int>(type: "integer", nullable: false),
                    ImportedRecords = table.Column<int>(type: "integer", nullable: false),
                    DuplicateRecords = table.Column<int>(type: "integer", nullable: false),
                    FailedRecords = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Imports_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Imports_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstallmentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallmentValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInstallments = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallmentGroups_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstallmentGroups_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategorizationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<int>(type: "integer", nullable: true),
                    Pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MatchType = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorizationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategorizationRules_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategorizationRules_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategorizationRules_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    NormalizedDescription = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    ImportId = table.Column<int>(type: "integer", nullable: true),
                    TransactionHash = table.Column<string>(type: "text", nullable: false),
                    InstallmentGroupId = table.Column<int>(type: "integer", nullable: true),
                    CurrentInstallment = table.Column<int>(type: "integer", nullable: false),
                    TotalInstallments = table.Column<int>(type: "integer", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Transactions_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Imports_ImportId",
                        column: x => x.ImportId,
                        principalTable: "Imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Transactions_InstallmentGroups_InstallmentGroupId",
                        column: x => x.InstallmentGroupId,
                        principalTable: "InstallmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex(name: "IX_AspNetUsers_FamilyId", table: "AspNetUsers", column: "FamilyId");
            migrationBuilder.CreateIndex(name: "IX_FinancialAccounts_FamilyId", table: "FinancialAccounts", column: "FamilyId");
            migrationBuilder.CreateIndex(name: "IX_Imports_FamilyId_ImportedAt", table: "Imports", columns: new[] { "FamilyId", "ImportedAt" });
            migrationBuilder.CreateIndex(name: "IX_Imports_FinancialAccountId", table: "Imports", column: "FinancialAccountId");
            migrationBuilder.CreateIndex(name: "IX_InstallmentGroups_FamilyId", table: "InstallmentGroups", column: "FamilyId");
            migrationBuilder.CreateIndex(name: "IX_InstallmentGroups_FinancialAccountId", table: "InstallmentGroups", column: "FinancialAccountId");
            migrationBuilder.CreateIndex(name: "IX_CategorizationRules_CategoryId", table: "CategorizationRules", column: "CategoryId");
            migrationBuilder.CreateIndex(name: "IX_CategorizationRules_FamilyId_Priority", table: "CategorizationRules", columns: new[] { "FamilyId", "Priority" });
            migrationBuilder.CreateIndex(name: "IX_CategorizationRules_FinancialAccountId", table: "CategorizationRules", column: "FinancialAccountId");
            migrationBuilder.CreateIndex(name: "IX_Transactions_CategoryId", table: "Transactions", column: "CategoryId");
            migrationBuilder.CreateIndex(name: "IX_Transactions_FamilyId_FinancialAccountId_TransactionDate", table: "Transactions", columns: new[] { "FamilyId", "FinancialAccountId", "TransactionDate" });
            migrationBuilder.CreateIndex(name: "IX_Transactions_FamilyId_TransactionDate", table: "Transactions", columns: new[] { "FamilyId", "TransactionDate" });
            migrationBuilder.CreateIndex(name: "IX_Transactions_FamilyId_TransactionHash", table: "Transactions", columns: new[] { "FamilyId", "TransactionHash" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Transactions_FinancialAccountId", table: "Transactions", column: "FinancialAccountId");
            migrationBuilder.CreateIndex(name: "IX_Transactions_ImportId", table: "Transactions", column: "ImportId");
            migrationBuilder.CreateIndex(name: "IX_Transactions_InstallmentGroupId", table: "Transactions", column: "InstallmentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Families_FamilyId",
                table: "AspNetUsers",
                column: "FamilyId",
                principalTable: "Families",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_AspNetUsers_Families_FamilyId", table: "AspNetUsers");
            migrationBuilder.DropTable(name: "Transactions");
            migrationBuilder.DropTable(name: "CategorizationRules");
            migrationBuilder.DropTable(name: "InstallmentGroups");
            migrationBuilder.DropTable(name: "Imports");
            migrationBuilder.DropTable(name: "FinancialAccounts");
            migrationBuilder.DropColumn(name: "FamilyId", table: "AspNetUsers");
            migrationBuilder.DropTable(name: "Families");
        }
    }
}