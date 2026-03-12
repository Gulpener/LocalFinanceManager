using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for AppSettings persistence behaviour.
/// Uses in-memory SQLite (provider matrix: integration tests = in-memory SQLite).
/// Note: EF migration execution tests are handled by E2E tests against a real PostgreSQL backend.
/// </summary>
[TestFixture]
public class AppSettingsMigrationTests
{
    private SqliteConnection _connection = null!;

    [SetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private DbContextOptions<AppDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

    [Test]
    public async Task AppSettings_CanBeCreatedAndRetrieved()
    {
        var options = BuildOptions();

        await using (var ctx = new AppDbContext(options))
        {
            await ctx.Database.EnsureCreatedAsync();

            var settings = new AppSettings
            {
                Id = Guid.NewGuid(),
                AutoApplyEnabled = true,
                MinimumConfidence = 0.72f,
                IntervalMinutes = 25,
                AccountIdsJson = "[\"11111111-1111-1111-1111-111111111111\"]",
                ExcludedCategoryIdsJson = "[\"22222222-2222-2222-2222-222222222222\"]",
                UpdatedBy = "test-user"
            };

            await ctx.AppSettings.AddAsync(settings);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new AppDbContext(options))
        {
            var settings = await ctx.AppSettings.SingleAsync();

            Assert.That(settings.AutoApplyEnabled, Is.True);
            Assert.That(settings.MinimumConfidence, Is.EqualTo(0.72f).Within(0.001f));
            Assert.That(settings.IntervalMinutes, Is.EqualTo(25));
            Assert.That(settings.AccountIdsJson, Is.EqualTo("[\"11111111-1111-1111-1111-111111111111\"]"));
            Assert.That(settings.ExcludedCategoryIdsJson, Is.EqualTo("[\"22222222-2222-2222-2222-222222222222\"]"));
            Assert.That(settings.UpdatedBy, Is.EqualTo("test-user"));
            Assert.That(settings.IsArchived, Is.False);
            Assert.That(settings.CreatedAt, Is.Not.EqualTo(default(DateTime)));
        }
    }

    [Test]
    public async Task AppSettings_SoftDelete_WorksCorrectly()
    {
        var options = BuildOptions();

        await using var ctx = new AppDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        var settings = new AppSettings
        {
            Id = Guid.NewGuid(),
            AutoApplyEnabled = false,
            MinimumConfidence = 0.5f,
            IntervalMinutes = 10
        };

        await ctx.AppSettings.AddAsync(settings);
        await ctx.SaveChangesAsync();

        // Soft-delete
        settings.IsArchived = true;
        await ctx.SaveChangesAsync();

        var archived = await ctx.AppSettings.Where(s => s.IsArchived).ToListAsync();
        Assert.That(archived, Has.Count.EqualTo(1));

        var active = await ctx.AppSettings.Where(s => !s.IsArchived).ToListAsync();
        Assert.That(active, Is.Empty);
    }
}

