using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace LocalFinanceManager.Infrastructure.Migrations
{
    [DbContext(typeof(LocalFinanceManager.Infrastructure.ApplicationDbContext))]
    [Migration("20251128_AddTransactionSplit")]
    public partial class AddTransactionSplit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ParentTransactionId column to Transactions (nullable self-reference)
            migrationBuilder.AddColumn<int>(
                name: "ParentTransactionId",
                table: "Transactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Create TransactionSplits table
            migrationBuilder.CreateTable(
                name: "TransactionSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentTransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    EnvelopeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionSplits_Transactions_ParentTransactionId",
                        column: x => x.ParentTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionSplits_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransactionSplits_Envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "Envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplits_ParentTransactionId",
                table: "TransactionSplits",
                column: "ParentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplits_CategoryId",
                table: "TransactionSplits",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplits_EnvelopeId",
                table: "TransactionSplits",
                column: "EnvelopeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionSplits");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Transactions_ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ParentTransactionId",
                table: "Transactions");
        }
    }
}
