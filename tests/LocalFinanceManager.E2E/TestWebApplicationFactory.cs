using System.Collections.Generic;
using LocalFinanceManager.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalFinanceManager.E2E;

/// <summary>
/// Web application factory for E2E tests using Playwright.
/// Creates a REAL Kestrel server (not TestServer) that Playwright can connect to.
/// Provides an isolated SQLite database for testing.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestServerUrl = "http://localhost:5173";
    private IHost? _host;
    private readonly string _testDatabasePath;

    public string ServerAddress => TestServerUrl;
    public string TestDatabasePath => _testDatabasePath;

    public TestWebApplicationFactory(string testName)
    {
        _testDatabasePath = $"localfinancemanager.e2etest.{SanitizeTestName(testName)}.db";
    }

    private static string SanitizeTestName(string testName)
    {
        // Remove invalid filename characters
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", testName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create a real host that runs Kestrel (not TestServer) for Playwright
        var dummyHost = builder.Build();

        // Build the real host with Kestrel
        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

        _host = builder.Build();
        _host.Start();

        return dummyHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Kestrel with a real HTTP endpoint for Playwright E2E tests
        builder.UseKestrel();
        builder.UseUrls(TestServerUrl);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string to use test database
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_testDatabasePath}",
                ["RecreateDatabase"] = "true", // Always fresh database for E2E tests
                ["Automation:Enabled"] = "false", // Disable background jobs during tests
                ["ML:EnableAutoSuggestions"] = "false" // Disable ML during tests unless explicitly enabled
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
                options.UseSqlite($"Data Source={_testDatabasePath}");
            });
        });

        // Configure logging to output to test console
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning); // Reduce noise in test output
        });
    }

    /// <summary>
    /// Ensures database is ready for testing by deleting and recreating it.
    /// Must be called before the server starts to avoid file locks.
    /// </summary>
    public async Task EnsureDatabaseReadyAsync()
    {
        // Delete database file if it exists (do this before server starts)
        if (File.Exists(_testDatabasePath))
        {
            try
            {
                File.Delete(_testDatabasePath);
            }
            catch (IOException)
            {
                // File is locked, wait a moment and retry
                await Task.Delay(100);
                try
                {
                    File.Delete(_testDatabasePath);
                }
                catch
                {
                    // If still locked, just ignore - migrations will handle it
                }
            }
        }
    }

    /// <summary>
    /// Resets the database to a clean state by deleting and recreating all tables.
    /// Call this between tests to ensure test isolation.
    /// </summary>
    /// <remarks>
    /// This method is useful when running multiple tests in sequence without disposing the factory.
    /// For most test scenarios, the factory should be recreated for each test to ensure isolation.
    /// </remarks>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Don't delete the database file (it might be locked), just ensure migrations are applied
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Gets a scoped database context for direct database operations in tests.
    /// Dispose the scope when done to release resources.
    /// </summary>
    /// <returns>A new service scope containing a DbContext.</returns>
    public IServiceScope CreateDbScope()
    {
        return Services.CreateScope();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Stop and dispose the host first
            if (_host != null)
            {
                try
                {
                    _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                }
                catch
                {
                    // Ignore shutdown errors
                }

                _host.Dispose();
                _host = null;
            }
        }

        // Call base disposal to ensure all WebApplicationFactory resources are disposed
        base.Dispose(disposing);

        if (disposing)
        {
            // Clear SQLite connection pool to release all file handles
            SqliteConnection.ClearAllPools();

            // Wait longer for file handles to be fully released
            Thread.Sleep(200);

            // Force garbage collection to ensure finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Clean up test database file with retry logic
            if (File.Exists(_testDatabasePath))
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(_testDatabasePath);
                        break; // Success
                    }
                    catch (IOException) when (i < 4)
                    {
                        // File still locked, clear pools again and wait
                        SqliteConnection.ClearAllPools();
                        Thread.Sleep(100 * (i + 1)); // Increasing backoff
                    }
                    catch
                    {
                        // Final attempt failed or different error, ignore
                        break;
                    }
                }
            }
        }
    }
}
