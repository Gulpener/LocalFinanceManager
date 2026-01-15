using LocalFinanceManager.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalFinanceManager.E2E;

/// <summary>
/// Base fixture for E2E tests using Playwright and WebApplicationFactory.
/// Provides a test web server with an isolated SQLite database.
/// </summary>
public class PlaywrightFixture : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;
    private const string TestDatabasePath = "localfinancemanager.test.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string to use test database
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={TestDatabasePath}",
                ["RecreateDatabase"] = "true" // Always fresh database for E2E tests
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Use file-based SQLite database for E2E testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite($"Data Source={TestDatabasePath}");
            });

            // Build service provider and ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Recreate database for clean state
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
            
            // Clean up test database file
            if (File.Exists(TestDatabasePath))
            {
                try
                {
                    File.Delete(TestDatabasePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        base.Dispose(disposing);
    }
}
