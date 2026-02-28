using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class AlignAppSettingsWithBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"AppSettings\" RENAME TO \"AppSettings_Old\";");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AutoApplyEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumConfidence = table.Column<float>(type: "REAL", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountIdsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExcludedCategoryIdsJson = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_IsArchived",
                table: "AppSettings",
                column: "IsArchived");

            migrationBuilder.Sql("""
                INSERT INTO "AppSettings" (
                    "Id", "AutoApplyEnabled", "MinimumConfidence", "IntervalMinutes",
                    "AccountIdsJson", "ExcludedCategoryIdsJson", "UpdatedAt", "UpdatedBy",
                    "RowVersion", "CreatedAt", "IsArchived")
                SELECT
                    '6fba7d31-3d45-4e1f-bcba-6eb433be34df', "AutoApplyEnabled", "MinimumConfidence", "IntervalMinutes",
                    "AccountIdsJson", "ExcludedCategoryIdsJson", "UpdatedAt", "UpdatedBy",
                    NULL, COALESCE("UpdatedAt", datetime('now')), 0
                FROM "AppSettings_Old"
                ORDER BY "Id"
                LIMIT 1;
                """);

            migrationBuilder.DropTable(name: "AppSettings_Old");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"AppSettings\" RENAME TO \"AppSettings_New\";");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AutoApplyEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumConfidence = table.Column<float>(type: "REAL", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountIdsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExcludedCategoryIdsJson = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.Sql("""
                INSERT INTO "AppSettings" (
                    "Id", "AutoApplyEnabled", "MinimumConfidence", "IntervalMinutes",
                    "AccountIdsJson", "ExcludedCategoryIdsJson", "UpdatedAt", "UpdatedBy")
                SELECT
                    1, "AutoApplyEnabled", "MinimumConfidence", "IntervalMinutes",
                    "AccountIdsJson", "ExcludedCategoryIdsJson", "UpdatedAt", "UpdatedBy"
                FROM "AppSettings_New"
                ORDER BY "CreatedAt"
                LIMIT 1;
                """);

            migrationBuilder.DropTable(name: "AppSettings_New");
        }
    }
}
