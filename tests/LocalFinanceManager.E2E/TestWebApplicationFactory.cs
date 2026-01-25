using System.Collections.Generic;
using LocalFinanceManager.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    private const string TestDatabasePath = "localfinancemanager.test.db";
    private const string TestServerUrl = "http://localhost:5173";
    private IHost? _realHost;

    public string ServerAddress => TestServerUrl;

    /// <summary>
    /// Starts a REAL Kestrel server for Playwright. WebApplicationFactory's TestServer won't work with Playwright.
    /// </summary>
    public async Task StartServerAsync()
    {
        TestContext.Out.WriteLine("Starting real Kestrel server for Playwright...");

        // Build a real WebApplication with Kestrel (not TestServer)
        var builder = WebApplication.CreateBuilder();

        // Configure connection string for test database
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = $"Data Source={TestDatabasePath}",
            ["Automation:Enabled"] = "false"
        });

        // Configure Kestrel to listen on our test port
        builder.WebHost.UseUrls(TestServerUrl);

        // Reduce logging noise in tests
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Register all services - we need to duplicate Program.cs setup
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        // Apply migrations
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        // Configure middleware pipeline (simplified for testing)
        app.UseStaticFiles(); // Simplified static file serving for tests
        app.UseRouting();
        app.UseAntiforgery();
        app.MapRazorComponents<LocalFinanceManager.Components.App>()
            .AddInteractiveServerRenderMode();
        app.MapControllers();

        // Start the server
        _ = app.RunAsync(); // Fire and forget - keep running in background
        _realHost = app; // Store for cleanup

        TestContext.Out.WriteLine("Real Kestrel server started and listening");
        // Give it a moment to fully bind
        await Task.Delay(2000);
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Copy service registration from Program.cs
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=localfinancemanager.db";
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.Configure<LocalFinanceManager.Configuration.MLOptions>(configuration.GetSection("MLOptions"));
        services.Configure<LocalFinanceManager.Configuration.AutomationOptions>(configuration.GetSection("AutomationOptions"));
        services.Configure<LocalFinanceManager.Configuration.CacheOptions>(configuration.GetSection("Caching"));

        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000;
            options.CompactionPercentage = 0.25;
        });

        // Register repositories (simplified - add more as needed for tests)
        services.AddScoped(typeof(Data.Repositories.IRepository<>), typeof(Data.Repositories.Repository<>));
        services.AddScoped<Data.Repositories.IAccountRepository, Data.Repositories.AccountRepository>();

        // Add Blazor services
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddControllers();
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
                ["ConnectionStrings:Default"] = $"Data Source={TestDatabasePath}",
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
                options.UseSqlite($"Data Source={TestDatabasePath}");
            });

            // Build service provider and ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Recreate database for clean state
            context.Database.EnsureDeleted();
            context.Database.Migrate(); // Apply migrations instead of EnsureCreated
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

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync(); // Apply migrations instead of EnsureCreated
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
            _realHost?.StopAsync().GetAwaiter().GetResult();
            _realHost?.Dispose();

            // Clean up test database file
            if (File.Exists(TestDatabasePath))
            {
                try
                {
                    File.Delete(TestDatabasePath);
                }
                catch
                {
                    // Ignore cleanup errors - database might be in use
                }
            }
        }
        base.Dispose(disposing);
    }
}
