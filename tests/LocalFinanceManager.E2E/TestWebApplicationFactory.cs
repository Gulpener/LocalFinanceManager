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
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace LocalFinanceManager.E2E;

/// <summary>
/// Web application factory for E2E tests using Playwright.
/// Creates a REAL Kestrel server (not TestServer) that Playwright can connect to.
/// Provides an isolated SQLite database per test fixture for parallel testing.
/// Uses dynamic port allocation to avoid port conflicts.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _host;
    private readonly string _testDatabasePath;
    private readonly int _port;
    private readonly string _fixtureId;

    public string ServerAddress { get; private set; }
    public string TestDatabasePath => _testDatabasePath;

    public TestWebApplicationFactory(string fixtureId)
    {
        // Use fixture ID to create one database per test fixture (enables parallel execution)
        _fixtureId = fixtureId;
        _testDatabasePath = $"localfinancemanager.e2etest.fixture_{_fixtureId}.db";
        _port = GetAvailablePort();
        // Use 127.0.0.1 instead of localhost for dynamic port binding (Kestrel requirement)
        ServerAddress = $"http://127.0.0.1:{_port}";
    }

    /// <summary>
    /// Gets an available port by temporarily binding to one, then releasing it.
    /// Uses a small delay after release to ensure OS fully releases the port before Kestrel binds.
    /// </summary>
    private static int GetAvailablePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        
        // Small delay to ensure OS releases the port
        System.Threading.Thread.Sleep(50);
        
        return port;
    }
    /// <summary>
    /// Gets the connection string for the test database with WAL mode enabled.
    /// WAL (Write-Ahead Logging) mode enables better concurrent access performance.
    /// </summary>
    private string GetConnectionString() => $"Data Source={_testDatabasePath};Cache=Shared";

    private static string SanitizeTestName(string testName)
    {
        // Remove invalid filename characters
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", testName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Gets the path to the main application project (LocalFinanceManager).
    /// Walks up the directory tree from the test project to find the solution root,
    /// then navigates to the main application folder.
    /// </summary>
    private static string GetMainApplicationPath()
    {
        // Start from the current assembly location (test bin directory)
        var assemblyLocation = Path.GetDirectoryName(typeof(TestWebApplicationFactory).Assembly.Location)!;
        var directory = new DirectoryInfo(assemblyLocation);
        
        // Walk up until we find the solution file
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }
        
        if (directory == null)
        {
            throw new InvalidOperationException("Could not find solution directory");
        }
        
        // Navigate to the main application project
        return Path.Combine(directory.FullName, "LocalFinanceManager");
    }

    /// <summary>
    /// Ensures the server is started by accessing the Services property which triggers host creation.
    /// This ensures Kestrel is listening on the configured port.
    /// </summary>
    public void EnsureServerStarted()
    {
        // Accessing Services property triggers CreateHost if not already called
        // This ensures Kestrel is listening on the configured port
        var services = Services;
        
        // Verify services are available
        if (services == null)
        {
            throw new InvalidOperationException("Failed to start server - Services property is null");
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create a dummy host that is returned to WebApplicationFactory
        var dummyHost = builder.Build();

        // Build the real host with Kestrel for Playwright
        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

        // Build and start the real Kestrel host
        // No semaphore needed - each test fixture has its own database file and port
        _host = builder.Build();
        _host.Start();

        return dummyHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment and paths via configuration - this works with WebApplicationFactory
        var projectDir = GetMainApplicationPath();
        builder.UseEnvironment("Development");
        builder.UseContentRoot(projectDir);
        builder.UseWebRoot(Path.Combine(projectDir, "wwwroot"));
        
        // Use Kestrel with a real HTTP endpoint for Playwright E2E tests
        // Use dynamically allocated port to avoid conflicts
        builder.UseKestrel();
        
        // CRITICAL: Must use 127.0.0.1 instead of localhost for dynamic port binding
        // Kestrel does not support "localhost:0" - only "127.0.0.1:0" or "[::1]:0"
        builder.UseUrls(ServerAddress);
        
        // Also set via configuration to ensure it takes precedence
        builder.UseSetting(WebHostDefaults.ServerUrlsKey, ServerAddress);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string to use test database
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = GetConnectionString(),
                ["RecreateDatabase"] = "true", // Let the app recreate the database on startup for E2E tests
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

            // Use file-based SQLite database for E2E testing with WAL mode
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(GetConnectionString(), sqliteOptions =>
                {
                    sqliteOptions.CommandTimeout(60);
                });
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
    /// Initializes database for the test fixture by deleting old files.
    /// Should be called once per fixture (OneTimeSetUp), BEFORE server starts.
    /// WAL mode should be enabled AFTER migrations complete.
    /// </summary>
    public async Task InitializeDatabaseAsync()
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
    /// Enables WAL mode for better concurrency.
    /// Call this AFTER migrations complete (after server starts).
    /// </summary>
    public async Task EnableWALModeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }

    /// <summary>
    /// Truncates all tables to provide clean state between tests within the same fixture.
    /// Call this in [SetUp] if your tests need isolation.
    /// For tests that don't modify data or can tolerate shared state, this is optional.
    /// </summary>
    public async Task TruncateTablesAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Truncate all tables in reverse dependency order
        await context.Database.ExecuteSqlRawAsync("DELETE FROM TransactionSplits;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM TransactionAudits;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM LabeledExamples;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Transactions;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM BudgetLines;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Categories;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM BudgetPlans;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Accounts;");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM MLModels;");

        // Reset SQLite sequence counters (not strictly needed for GUID PKs)
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
            // WebApplicationFactory base class handles disposal of the host
            // No need to manually dispose anything here
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
            Task.Delay(300);

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
                        Task.Delay(100 * (i + 1)); // Increasing backoff: 100ms, 200ms, 300ms...
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
