using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionAssignmentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BeforeState = table.Column<string>(type: "TEXT", nullable: true),
                    AfterState = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionAudits_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionSplits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BudgetLineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionSplits_BudgetLines_BudgetLineId",
                        column: x => x.BudgetLineId,
                        principalTable: "BudgetLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionSplits_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionSplits_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAudits_ActionType",
                table: "TransactionAudits",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAudits_ChangedAt",
                table: "TransactionAudits",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAudits_IsArchived",
                table: "TransactionAudits",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionAudits_TransactionId",
                table: "TransactionAudits",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplits_BudgetLineId",
                table: "TransactionSplits",
                column: "BudgetLineId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplits_CategoryId",
                table: "TransactionSplits",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplits_IsArchived",
                table: "TransactionSplits",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplits_TransactionId",
                table: "TransactionSplits",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionAudits");

            migrationBuilder.DropTable(
                name: "TransactionSplits");
        }
    }
}
