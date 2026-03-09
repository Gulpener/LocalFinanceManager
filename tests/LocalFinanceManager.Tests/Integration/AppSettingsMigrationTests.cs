using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LocalFinanceManager.Tests.Integration;

[TestFixture]
public class AppSettingsMigrationTests
{
    private string _dbPath = null!;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"lfm-appsettings-migration-{Guid.NewGuid():N}.db");
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task Migrate_FromLegacyAppSettings_PreservesSettingsInSingletonRecord()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        await using (var setupContext = new AppDbContext(options))
        {
            var migrator = setupContext.Database.GetService<IMigrator>();

            await migrator.MigrateAsync("20260205211852_AddAppSettings");

            await setupContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"AppSettings\" (\"AutoApplyEnabled\", \"MinimumConfidence\", \"IntervalMinutes\", \"AccountIdsJson\", \"ExcludedCategoryIdsJson\", \"UpdatedAt\", \"UpdatedBy\") " +
                "VALUES (1, 0.72, 25, '[\"11111111-1111-1111-1111-111111111111\"]', '[\"22222222-2222-2222-2222-222222222222\"]', datetime('now'), 'legacy-user');");

            await migrator.MigrateAsync();
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var settings = await assertContext.AppSettings.SingleAsync();

            Assert.That(settings.AutoApplyEnabled, Is.True);
            Assert.That(settings.MinimumConfidence, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(settings.IntervalMinutes, Is.EqualTo(25));
            Assert.That(settings.AccountIdsJson, Is.EqualTo("[\"11111111-1111-1111-1111-111111111111\"]"));
            Assert.That(settings.ExcludedCategoryIdsJson, Is.EqualTo("[\"22222222-2222-2222-2222-222222222222\"]"));
            Assert.That(settings.UpdatedBy, Is.EqualTo("legacy-user"));
            Assert.That(settings.IsArchived, Is.False);
            Assert.That(settings.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        }
    }
}
