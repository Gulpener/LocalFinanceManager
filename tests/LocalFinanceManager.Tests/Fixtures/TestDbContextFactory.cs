using LocalFinanceManager.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Tests.Fixtures;

/// <summary>
/// Factory for creating fresh in-memory SQLite database contexts for isolated tests.
/// Uses :memory: mode to ensure each test has a clean database state.
/// A system user (Id = Guid.Empty) is seeded via EF Core model data so that FK constraints on UserId are satisfied.
/// </summary>
public class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        // Use in-memory SQLite database with shared cache disabled for isolation
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Creates a new AppDbContext instance connected to the in-memory database.
    /// Automatically applies model and seeds the system user via EnsureCreated.
    /// </summary>
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
