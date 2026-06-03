using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrcamentoFamiliar.Infrastructure.Data;

#nullable disable

namespace OrcamentoFamiliar.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260105000000_AddPrepaidCardSupport")]
    public partial class AddPrepaidCardSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardType",
                table: "Cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyCredit",
                table: "Cards",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditSinceYear",
                table: "Cards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditSinceMonth",
                table: "Cards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InitialBalance",
                table: "Cards",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CardType", table: "Cards");
            migrationBuilder.DropColumn(name: "MonthlyCredit", table: "Cards");
            migrationBuilder.DropColumn(name: "CreditSinceYear", table: "Cards");
            migrationBuilder.DropColumn(name: "CreditSinceMonth", table: "Cards");
            migrationBuilder.DropColumn(name: "InitialBalance", table: "Cards");
        }
    }
}
