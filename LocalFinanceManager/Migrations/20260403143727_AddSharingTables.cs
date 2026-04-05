using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class AddSharingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountShares_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountShares_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BudgetPlanShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPlanShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetPlanShares_BudgetPlans_BudgetPlanId",
                        column: x => x.BudgetPlanId,
                        principalTable: "BudgetPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetPlanShares_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountShares_AccountId_SharedWithUserId",
                table: "AccountShares",
                columns: new[] { "AccountId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountShares_IsArchived",
                table: "AccountShares",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_AccountShares_SharedWithUserId",
                table: "AccountShares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountShares_Status",
                table: "AccountShares",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlanShares_BudgetPlanId_SharedWithUserId",
                table: "BudgetPlanShares",
                columns: new[] { "BudgetPlanId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlanShares_IsArchived",
                table: "BudgetPlanShares",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlanShares_SharedWithUserId",
                table: "BudgetPlanShares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPlanShares_Status",
                table: "BudgetPlanShares",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountShares");

            migrationBuilder.DropTable(
                name: "BudgetPlanShares");
        }
    }
}
