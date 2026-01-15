using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class MigrationAfterFinishingMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoAppliedAt",
                table: "TransactionAudits",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutoAppliedBy",
                table: "TransactionAudits",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Confidence",
                table: "TransactionAudits",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoApplied",
                table: "TransactionAudits",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ModelVersion",
                table: "TransactionAudits",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabeledExamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    WasAutoApplied = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcceptedSuggestion = table.Column<bool>(type: "INTEGER", nullable: true),
                    SuggestionConfidence = table.Column<float>(type: "REAL", nullable: true),
                    ModelVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabeledExamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabeledExamples_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabeledExamples_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabeledExamples_CategoryId",
                table: "LabeledExamples",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LabeledExamples_CategoryId_CreatedAt",
                table: "LabeledExamples",
                columns: new[] { "CategoryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabeledExamples_CreatedAt",
                table: "LabeledExamples",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabeledExamples_IsArchived",
                table: "LabeledExamples",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_LabeledExamples_TransactionId",
                table: "LabeledExamples",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabeledExamples");

            migrationBuilder.DropColumn(
                name: "AutoAppliedAt",
                table: "TransactionAudits");

            migrationBuilder.DropColumn(
                name: "AutoAppliedBy",
                table: "TransactionAudits");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "TransactionAudits");

            migrationBuilder.DropColumn(
                name: "IsAutoApplied",
                table: "TransactionAudits");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "TransactionAudits");
        }
    }
}
