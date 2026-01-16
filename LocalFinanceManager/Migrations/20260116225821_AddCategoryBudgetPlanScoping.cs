using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryBudgetPlanScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLines_Categories_CategoryId",
                table: "BudgetLines");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            // Add nullable BudgetPlanId column first
            migrationBuilder.AddColumn<Guid>(
                name: "BudgetPlanId",
                table: "Categories",
                type: "TEXT",
                nullable: true);

            // Data migration: Duplicate existing global categories into each budget plan
            migrationBuilder.Sql(@"
                -- Create a temporary mapping table for category IDs
                CREATE TEMP TABLE CategoryMapping (
                    OriginalCategoryId TEXT,
                    NewCategoryId TEXT,
                    BudgetPlanId TEXT
                );

                -- For each budget plan, duplicate all existing categories
                INSERT INTO Categories (Id, Name, Type, BudgetPlanId, IsArchived, CreatedAt, UpdatedAt, RowVersion)
                SELECT
                    lower(hex(randomblob(16))) as Id,
                    c.Name,
                    c.Type,
                    bp.Id as BudgetPlanId,
                    c.IsArchived,
                    datetime('now') as CreatedAt,
                    datetime('now') as UpdatedAt,
                    NULL as RowVersion
                FROM Categories c
                CROSS JOIN BudgetPlans bp
                WHERE c.BudgetPlanId IS NULL;

                -- Store mapping of original category IDs to new category IDs per budget plan
                INSERT INTO CategoryMapping (OriginalCategoryId, NewCategoryId, BudgetPlanId)
                SELECT
                    c_old.Id as OriginalCategoryId,
                    c_new.Id as NewCategoryId,
                    c_new.BudgetPlanId
                FROM Categories c_old
                CROSS JOIN BudgetPlans bp
                JOIN Categories c_new ON c_new.Name = c_old.Name 
                    AND c_new.Type = c_old.Type 
                    AND c_new.BudgetPlanId = bp.Id
                WHERE c_old.BudgetPlanId IS NULL;

                -- Update BudgetLines to reference the new scoped categories
                UPDATE BudgetLines
                SET CategoryId = (
                    SELECT cm.NewCategoryId
                    FROM CategoryMapping cm
                    WHERE cm.OriginalCategoryId = BudgetLines.CategoryId
                        AND cm.BudgetPlanId = BudgetLines.BudgetPlanId
                );

                -- Update TransactionSplits to reference the new scoped categories
                -- Match TransactionSplit.CategoryId to the budget plan of the transaction's account
                UPDATE TransactionSplits
                SET CategoryId = (
                    SELECT cm.NewCategoryId
                    FROM CategoryMapping cm
                    JOIN Transactions t ON t.Id = TransactionSplits.TransactionId
                    JOIN BudgetPlans bp ON bp.AccountId = t.AccountId
                    WHERE cm.OriginalCategoryId = TransactionSplits.CategoryId
                        AND cm.BudgetPlanId = bp.Id
                    ORDER BY bp.CreatedAt DESC
                    LIMIT 1
                )
                WHERE CategoryId IN (SELECT OriginalCategoryId FROM CategoryMapping);

                -- Update LabeledExamples to reference the newest scoped category per account
                -- (Use the most recent budget plan for each account)
                UPDATE LabeledExamples
                SET CategoryId = (
                    SELECT cm.NewCategoryId
                    FROM CategoryMapping cm
                    JOIN Transactions t ON t.Id = LabeledExamples.TransactionId
                    JOIN BudgetPlans bp ON bp.AccountId = t.AccountId
                    WHERE cm.OriginalCategoryId = LabeledExamples.CategoryId
                        AND cm.BudgetPlanId = bp.Id
                    ORDER BY bp.CreatedAt DESC
                    LIMIT 1
                )
                WHERE CategoryId IN (SELECT OriginalCategoryId FROM CategoryMapping);

                -- Delete original global categories
                DELETE FROM Categories WHERE BudgetPlanId IS NULL;

                -- Clean up temporary table
                DROP TABLE CategoryMapping;
            ");

            // Make BudgetPlanId non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "BudgetPlanId",
                table: "Categories",
                type: "TEXT",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_BudgetPlanId_Name",
                table: "Categories",
                columns: new[] { "BudgetPlanId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLines_Categories_CategoryId",
                table: "BudgetLines",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_BudgetPlans_BudgetPlanId",
                table: "Categories",
                column: "BudgetPlanId",
                principalTable: "BudgetPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetLines_Categories_CategoryId",
                table: "BudgetLines");

            migrationBuilder.DropForeignKey(
                name: "FK_Categories_BudgetPlans_BudgetPlanId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_BudgetPlanId_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "BudgetPlanId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetLines_Categories_CategoryId",
                table: "BudgetLines",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
