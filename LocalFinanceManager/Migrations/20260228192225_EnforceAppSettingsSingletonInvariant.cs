using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class EnforceAppSettingsSingletonInvariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "AppSettings"
                WHERE "Id" <> '6fba7d31-3d45-4e1f-bcba-6eb433be34df'
                  AND EXISTS (
                      SELECT 1 FROM "AppSettings"
                      WHERE "Id" = '6fba7d31-3d45-4e1f-bcba-6eb433be34df'
                  );

                UPDATE "AppSettings"
                SET "Id" = '6fba7d31-3d45-4e1f-bcba-6eb433be34df'
                WHERE "Id" <> '6fba7d31-3d45-4e1f-bcba-6eb433be34df'
                  AND "Id" = (
                      SELECT "Id"
                      FROM "AppSettings"
                      WHERE "Id" <> '6fba7d31-3d45-4e1f-bcba-6eb433be34df'
                      ORDER BY "CreatedAt", "UpdatedAt"
                      LIMIT 1
                  );

                DELETE FROM "AppSettings"
                WHERE "Id" <> '6fba7d31-3d45-4e1f-bcba-6eb433be34df';

                INSERT INTO "AppSettings" (
                    "Id", "AutoApplyEnabled", "MinimumConfidence", "IntervalMinutes",
                    "AccountIdsJson", "ExcludedCategoryIdsJson", "UpdatedAt", "UpdatedBy",
                    "RowVersion", "CreatedAt", "IsArchived"
                )
                SELECT
                    '6fba7d31-3d45-4e1f-bcba-6eb433be34df', 0, 0.85, 15,
                    NULL, NULL, datetime('now'), NULL,
                    NULL, datetime('now'), 0
                WHERE NOT EXISTS (SELECT 1 FROM "AppSettings");
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppSettings_SingletonId",
                table: "AppSettings",
                sql: "lower(Id) = '6fba7d31-3d45-4e1f-bcba-6eb433be34df'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AppSettings_SingletonId",
                table: "AppSettings");
        }
    }
}
