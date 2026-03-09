using System.Collections.Generic;
using System.Security.Claims;
using LocalFinanceManager.Data;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// <summary>
    /// Cookie name used by E2EAuthBypassStartupFilter to authenticate test requests at the
    /// HTTP level. Must be added to the Playwright browser context (via Context.AddCookiesAsync)
    /// in E2ETestBase.BaseSetUp for every test that navigates to protected pages.
    /// Tests that verify unauthenticated behaviour should create a fresh browser context
    /// (Browser.NewContextAsync) to avoid carrying this cookie.
    /// </summary>
    internal const string E2EAuthCookieName = "e2e-auth-token";
    private IHost? _host;
    private readonly string _testDatabasePath;
    private readonly string _fixtureId;

    public string ServerAddress { get; private set; }
    public string TestDatabasePath => _testDatabasePath;

    /// <summary>
    /// The real Kestrel host's service provider.
    /// Use this instead of <see cref="WebApplicationFactory{TEntryPoint}.Services"/> when you need
    /// to access singletons that live in the actual web server (e.g. IAutoApplySettingsProvider,
    /// IMLModelCache), because WebApplicationFactory.Services resolves from a separate dummy host.
    /// </summary>
    public IServiceProvider HostServices => _host?.Services
        ?? throw new InvalidOperationException("Real host has not been started. Call EnsureServerStarted() first.");

    public TestWebApplicationFactory(string fixtureId)
    {
        // Use fixture ID to create one database per test fixture (enables parallel execution)
        _fixtureId = fixtureId;
        _testDatabasePath = $"localfinancemanager.e2etest.fixture_{_fixtureId}.db";
        // Let Kestrel bind to an OS-assigned port to avoid race conditions in parallel test runs.
        ServerAddress = "http://127.0.0.1:0";
    }
    /// <summary>
    /// Gets the connection string for the test database.
    /// Pooling=False prevents SQLite shared-cache stale read issues: with connection pooling
    /// enabled, a pooled connection that was opened before a write may still see the old page-cache
    /// state even after the writer commits, causing the Kestrel host and the test's dummy host
    /// to disagree on the database contents.
    /// </summary>
    private string GetConnectionString() => $"Data Source={_testDatabasePath};Pooling=False";

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

        var addressFeature = _host.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>();

        var assignedAddress = addressFeature?.Addresses
            .FirstOrDefault(address => address.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            ?? addressFeature?.Addresses.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(assignedAddress))
        {
            throw new InvalidOperationException("Failed to resolve Kestrel assigned test server address.");
        }

        ServerAddress = assignedAddress;

        return dummyHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment and paths via configuration - this works with WebApplicationFactory
        var projectDir = GetMainApplicationPath();
        builder.UseEnvironment("Development");
        builder.UseContentRoot(projectDir);
        builder.UseWebRoot(Path.Combine(projectDir, "wwwroot"));

        // Use Kestrel with a real HTTP endpoint for Playwright E2E tests.
        // Keep ':0' so the OS picks a free ephemeral port atomically.
        builder.UseKestrel();

        // CRITICAL: Must use 127.0.0.1 instead of localhost for dynamic port binding.
        // Kestrel does not support "localhost:0" - only "127.0.0.1:0" or "[::1]:0".
        builder.UseUrls(ServerAddress);

        // Also set via configuration to ensure it takes precedence
        builder.UseSetting(WebHostDefaults.ServerUrlsKey, ServerAddress);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection string to use test database
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = GetConnectionString(),
                ["EnableDevSmokeAuth"] = "true",
                ["RecreateDatabase"] = "false", // Database cleanup is handled by InitializeDatabaseAsync()
                ["SkipDatabaseMigrations"] = "true", // Migrations are applied in InitializeDatabaseAsync() before host startup
                ["Automation:Enabled"] = "false", // Disable background jobs during tests
                ["AutomationOptions:MonitoringRefreshIntervalSeconds"] = "2", // Speed up monitoring auto-refresh tests
                // ML training overrides: smaller trees for fast test training
                ["MLOptions:NumberOfTrees"] = "5",
                ["MLOptions:NumberOfLeaves"] = "5",
                ["MLOptions:MinimumExampleCountPerLeaf"] = "2"
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

            services.RemoveAll<IDeviceDetectionService>();
            services.AddScoped<IDeviceDetectionService, DesktopDeviceDetectionService>();

            // Replace IUserContext with a test implementation that always returns the seed user ID
            services.RemoveAll<IUserContext>();
            services.AddScoped<IUserContext, E2ETestUserContext>();

            services.AddTransient<E2EDevSmokeHeaderHandler>();
            services.AddHttpClient("AuthorizedApiClient")
                .AddHttpMessageHandler<E2EDevSmokeHeaderHandler>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "E2ECookie";
                options.DefaultAuthenticateScheme = "E2ECookie";
                options.DefaultChallengeScheme = "E2ECookie";
            })
            .AddScheme<AuthenticationSchemeOptions, E2ECookieAuthenticationHandler>("E2ECookie", _ => { });

            // Replace AuthenticationStateProvider so AuthorizeRouteView treats every Playwright
            // request as authenticated (bypasses the /login redirect for the interactive circuit).
            // Also replace the concrete CustomAuthenticationStateProvider so Login/Logout pages
            // that inject it directly don't blow up during test warm-up.
            services.RemoveAll<AuthenticationStateProvider>();
            services.RemoveAll<CustomAuthenticationStateProvider>();
            services.AddScoped<CookieAwareAuthenticationStateProvider>();
            services.AddScoped<CustomAuthenticationStateProvider>(
                sp => sp.GetRequiredService<CookieAwareAuthenticationStateProvider>());
            services.AddScoped<AuthenticationStateProvider>(
                sp => sp.GetRequiredService<CookieAwareAuthenticationStateProvider>());

            // In Blazor 8 static SSR, AuthorizeRouteView reads HttpContext.User (set by ASP.NET Core
            // auth middleware) rather than calling AuthenticationStateProvider. Register a startup
            // filter so that requests carrying the "e2e-auth-token" cookie have HttpContext.User set
            // to an authenticated principal before any other middleware runs.
            // Tests that verify unauthenticated behaviour create a fresh browser context without
            // this cookie, so those requests still see an anonymous HttpContext.User.
            services.AddSingleton<IStartupFilter>(new E2EAuthBypassStartupFilter());
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

        // Apply migrations before the web host starts.
        // This avoids duplicate/concurrent migration execution paths during WebApplicationFactory host setup.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(GetConnectionString(), sqliteOptions => sqliteOptions.CommandTimeout(60))
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        var seedService = new DevelopmentUserSeedService(context);
        await seedService.SeedAsync();
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

        // Reset SQLite sequence counters (wrap in try-catch as table may not exist)
        // Not strictly needed for GUID PKs but ensures clean state
        try
        {
            await context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence;");
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // Table doesn't exist, ignore
        }
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
        if (_host == null)
        {
            return;
        }

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

    private sealed class DesktopDeviceDetectionService : IDeviceDetectionService
    {
        public Task<bool> IsTouchDeviceAsync() => Task.FromResult(false);

        public Task<ClientOperatingSystem> GetOperatingSystemAsync() => Task.FromResult(ClientOperatingSystem.Windows);
    }

    /// <summary>
    /// Returns an authenticated Blazor state only when the E2E auth cookie is present.
    /// This keeps authenticated and unauthenticated browser-context tests both valid.
    /// </summary>
    private sealed class CookieAwareAuthenticationStateProvider : CustomAuthenticationStateProvider
    {
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public CookieAwareAuthenticationStateProvider(
            Microsoft.JSInterop.IJSRuntime jsRuntime,
            Microsoft.Extensions.Logging.ILogger<CustomAuthenticationStateProvider> logger,
            AuthTokenStore tokenStore,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
            : base(jsRuntime, logger, tokenStore)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var hasCookie = _httpContextAccessor.HttpContext?.Request.Cookies.ContainsKey(E2EAuthCookieName) == true;
            if (!hasCookie)
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            var claims = new[]
            {
                new Claim("sub", "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.NameIdentifier, AppDbContext.SeedUserId.ToString()),
                new Claim(ClaimTypes.Name, AppDbContext.SeedUserEmail),
                new Claim(ClaimTypes.Email, AppDbContext.SeedUserEmail)
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "E2ECookie"));
            return Task.FromResult(new AuthenticationState(principal));
        }
    }

    private sealed class E2ECookieAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public E2ECookieAuthenticationHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var hasBypassCookie = Request.Cookies.ContainsKey(E2EAuthCookieName);
            var isInternalE2ERequest = Request.Headers.TryGetValue("X-E2E-Internal", out var marker)
                && string.Equals(marker.ToString(), "1", StringComparison.Ordinal);

            if (!hasBypassCookie && !isInternalE2ERequest)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim("sub", "00000000-0000-0000-0000-000000000001"),
                new Claim(ClaimTypes.NameIdentifier, AppDbContext.SeedUserId.ToString()),
                new Claim(ClaimTypes.Name, AppDbContext.SeedUserEmail),
                new Claim(ClaimTypes.Email, AppDbContext.SeedUserEmail)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// Inserts HTTP middleware at the start of the request pipeline that sets HttpContext.User
    /// to an authenticated principal when the "e2e-auth-token" cookie is present.
    /// This is required for Blazor 8 static-SSR pages: AuthorizeRouteView reads HttpContext.User
    /// (set by ASP.NET Core auth middleware) rather than calling AuthenticationStateProvider,
    /// so AlwaysAuthenticatedStateProvider alone is insufficient to bypass the /login redirect.
    /// Tests that verify unauthenticated behaviour create fresh browser contexts without this
    /// cookie, keeping those requests anonymous so AuthorizeRouteView still redirects them.
    /// </summary>
    private sealed class E2EAuthBypassStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (httpContext, nextMiddleware) =>
                {
                    if (httpContext.Request.Cookies.ContainsKey(E2EAuthCookieName))
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, AppDbContext.SeedUserEmail),
                            new Claim(ClaimTypes.NameIdentifier, AppDbContext.SeedUserId.ToString()),
                        };
                        var identity = new ClaimsIdentity(claims, "E2ETest");
                        httpContext.User = new ClaimsPrincipal(identity);
                    }
                    await nextMiddleware(httpContext);
                });
                next(app);
            };
        }
    }

    /// <summary>
    /// E2E test user context that always returns the seed user ID.
    /// This allows existing E2E tests to run without requiring real Supabase authentication.
    /// </summary>
    private sealed class E2ETestUserContext : IUserContext
    {
        public Guid GetCurrentUserId() => AppDbContext.SeedUserId;
        public string GetCurrentUserEmail() => AppDbContext.SeedUserEmail;
        public bool IsAuthenticated() => true;
    }

    private sealed class E2EDevSmokeHeaderHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Remove("X-E2E-Internal");
            request.Headers.Remove("X-Dev-Smoke-UserId");
            request.Headers.Remove("X-Dev-Smoke-Sub");
            request.Headers.Remove("X-Dev-Smoke-Email");

            request.Headers.Add("X-E2E-Internal", "1");
            request.Headers.Add("X-Dev-Smoke-UserId", AppDbContext.SeedUserId.ToString());
            request.Headers.Add("X-Dev-Smoke-Sub", "00000000-0000-0000-0000-000000000001");
            request.Headers.Add("X-Dev-Smoke-Email", AppDbContext.SeedUserEmail);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
