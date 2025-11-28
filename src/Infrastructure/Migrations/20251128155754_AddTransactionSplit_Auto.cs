using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSplit_Auto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSplit",
                table: "Transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentTransactionId",
                table: "Transactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransactionSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentTransactionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    EnvelopeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionSplits", x => x.Id);
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
                    table.ForeignKey(
                        name: "FK_TransactionSplits_Transactions_ParentTransactionId",
                        column: x => x.ParentTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionSplit_ParentTransactionId",
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

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_ParentTransactionId",
                table: "Transactions",
                column: "ParentTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Transactions_ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "TransactionSplits");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ParentTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsSplit",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ParentTransactionId",
                table: "Transactions");
        }
    }
}
