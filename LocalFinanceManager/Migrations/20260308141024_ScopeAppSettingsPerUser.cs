using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalFinanceManager.Migrations
{
    /// <inheritdoc />
    public partial class ScopeAppSettingsPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AppSettings_SingletonId",
                table: "AppSettings");

            migrationBuilder.CreateIndex(
                name: "IX_AppSettings_UserId",
                table: "AppSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppSettings_UserId",
                table: "AppSettings");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppSettings_SingletonId",
                table: "AppSettings",
                sql: "lower(Id) = '6fba7d31-3d45-4e1f-bcba-6eb433be34df'");
        }
    }
}
