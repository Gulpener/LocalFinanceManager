using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentBudgetPlanToAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentBudgetPlanId",
                table: "Accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CurrentBudgetPlanId",
                table: "Accounts",
                column: "CurrentBudgetPlanId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_BudgetPlans_CurrentBudgetPlanId",
                table: "Accounts",
                column: "CurrentBudgetPlanId",
                principalTable: "BudgetPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_BudgetPlans_CurrentBudgetPlanId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_CurrentBudgetPlanId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "CurrentBudgetPlanId",
                table: "Accounts");
        }
    }
}
