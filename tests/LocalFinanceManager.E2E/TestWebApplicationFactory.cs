using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
/// Uses dynamic port allocation to avoid port conflicts when tests run sequentially.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _host;
    private readonly string _testDatabasePath;
    private readonly int _port;

    public string ServerAddress { get; private set; }
    public string TestDatabasePath => _testDatabasePath;

    public TestWebApplicationFactory(string testName)
    {
        _testDatabasePath = $"localfinancemanager.e2etest.{SanitizeTestName(testName)}.db";
        _port = GetAvailablePort();
        ServerAddress = $"http://localhost:{_port}";
    }

    /// <summary>
    /// Gets an available TCP port by binding to port 0 (letting the OS choose)
    /// and then immediately releasing it.
    /// </summary>
    private static int GetAvailablePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)socket.LocalEndPoint!).Port;
        return port;
    }

    /// <summary>
    /// Gets the connection string for the test database with pooling disabled.
    /// Disabling pooling ensures file handles are released immediately after connections close.
    /// </summary>
    private string GetConnectionString() => $"Data Source={_testDatabasePath};Pooling=False;Cache=Shared";

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
        // Use dynamically allocated port to avoid conflicts
        builder.UseKestrel();
        builder.UseUrls(ServerAddress);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string to use test database
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = GetConnectionString(),
                ["RecreateDatabase"] = "false", // DON'T recreate database - we manually delete before startup
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
                options.UseSqlite(GetConnectionString());
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
        // Clear all SQLite connection pools first to release file handles
        SqliteConnection.ClearAllPools();

        // Give OS time to release handles
        await Task.Delay(100);

        // Delete database file and related SQLite files if they exist (do this before server starts)
        var filesToDelete = new[]
        {
            _testDatabasePath,
            _testDatabasePath + "-shm", // Shared memory file
            _testDatabasePath + "-wal"  // Write-Ahead Log file
        };

        foreach (var file in filesToDelete)
        {
            if (!File.Exists(file)) continue;

            // Try up to 3 times to delete the file
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    // Remove read-only attribute if set
                    var attributes = File.GetAttributes(file);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                    }

                    File.Delete(file);
                    break; // Success, exit retry loop
                }
                catch (IOException) when (attempt < 2)
                {
                    // File is locked, clear pools and wait before retry
                    SqliteConnection.ClearAllPools();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(200 * (attempt + 1)); // 200ms, then 400ms
                }
                catch
                {
                    // Different error or final attempt failed - just break
                    break;
                }
            }
        }

        // Final cleanup
        SqliteConnection.ClearAllPools();
    }

    /// <summary>
    /// Resets the database to a clean state by truncating all tables.
    /// WARNING: Do NOT call this during initialization - migrations are already applied by Program.cs during startup.
    /// Only call this method if you need to reset database state mid-test.
    /// </summary>
    /// <remarks>
    /// For most test scenarios, the factory should be recreated for each test to ensure isolation.
    /// This method is provided for advanced scenarios where you need to reset data without recreating the factory.
    /// </remarks>
    public async Task ResetDatabaseAsync()
    {
        // Force close all connections and clear pools
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await Task.Delay(50);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Truncate all tables (migrations already applied by Program.cs)
        // Using raw SQL for efficiency - EF Core doesn't have a built-in truncate method for SQLite
        await context.Database.ExecuteSqlRawAsync("DELETE FROM TransactionSplits;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM TransactionAudits;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM LabeledExamples;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Transactions;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM BudgetLines;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Categories;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM BudgetPlans;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Accounts;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM MLModels;");
        
        // Reset SQLite sequence counters (not needed for GUID primary keys, but good practice)
        await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence;");
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

    /// <summary>
    /// Closes all database connections to ensure file handles are released.
    /// Call this before disposing the factory to prevent file lock issues.
    /// </summary>
    public async Task CloseAllConnectionsAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Close the connection if it's open
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Closed)
        {
            await connection.CloseAsync();
        }

        // Clear connection pools to release all file handles
        SqliteConnection.ClearAllPools();

        // Give the OS time to release file handles
        await Task.Delay(50);
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

            // Force garbage collection to ensure finalizers run
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Wait longer for file handles to be fully released
            Thread.Sleep(300);

            // Clean up test database file with aggressive retry logic
            if (File.Exists(_testDatabasePath))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        File.Delete(_testDatabasePath);
                        break; // Success
                    }
                    catch (IOException) when (i < 9)
                    {
                        // File still locked, clear pools again and wait
                        SqliteConnection.ClearAllPools();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(100 * (i + 1)); // Increasing backoff: 100ms, 200ms, 300ms...
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
