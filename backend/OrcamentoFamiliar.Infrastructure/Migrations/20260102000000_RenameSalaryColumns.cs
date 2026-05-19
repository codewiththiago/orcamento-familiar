using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrcamentoFamiliar.Infrastructure.Data;

#nullable disable

namespace OrcamentoFamiliar.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260102000000_RenameSalaryColumns")]
    public partial class RenameSalaryColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SalaryThiago",
                table: "MonthlyBudgets",
                newName: "Salary1");

            migrationBuilder.RenameColumn(
                name: "SalaryJuh",
                table: "MonthlyBudgets",
                newName: "Salary2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Salary1",
                table: "MonthlyBudgets",
                newName: "SalaryThiago");

            migrationBuilder.RenameColumn(
                name: "Salary2",
                table: "MonthlyBudgets",
                newName: "SalaryJuh");
        }
    }
}
