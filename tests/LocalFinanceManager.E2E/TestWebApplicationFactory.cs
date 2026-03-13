using System.Collections.Generic;
using System.Security.Claims;
using LocalFinanceManager.Data;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Npgsql;

namespace LocalFinanceManager.E2E;

/// <summary>
/// Web application factory for E2E tests using Playwright.
/// Creates a REAL Kestrel server (not TestServer) that Playwright can connect to.
/// Provides an isolated PostgreSQL database per test fixture for parallel testing.
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
    private readonly string _fixtureId;

    public string ServerAddress { get; private set; }

    /// <summary>PostgreSQL database name used for this test fixture.</summary>
    public string TestDatabaseName { get; }

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
        _fixtureId = fixtureId;
        TestDatabaseName = $"lfm_e2e_{fixtureId}".ToLowerInvariant();
        // Let Kestrel bind to an OS-assigned port to avoid race conditions in parallel test runs.
        ServerAddress = "http://127.0.0.1:0";
    }

    /// <summary>
    /// Returns the base connection string (points to the admin 'postgres' database).
    /// Resolution order:
    ///   1. E2E_PG_CONNECTION environment variable (used in CI)
    ///   2. ConnectionStrings:Local from the main app's user-secrets (local dev)
    ///   3. Hardcoded localhost fallback
    /// GUARD: fails if the connection string targets a *.supabase.co host to prevent
    /// accidental test execution against the production Supabase database.
    /// </summary>
    private static string GetBaseConnectionString()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddUserSecrets("3deee73f-9f3f-4e27-ae81-4b0a5b9dff24")
            .Build();

        var baseConn = Environment.GetEnvironmentVariable("E2E_PG_CONNECTION");

        if (baseConn == null && config.GetConnectionString("Local") is { } localConn)
        {
            // Derive admin connection from the app's Local secret, pointing at the postgres admin DB
            var b = new NpgsqlConnectionStringBuilder(localConn) { Database = "postgres" };
            baseConn = b.ConnectionString;
        }

        baseConn ??= "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

        if (baseConn.Contains(".supabase.co", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "E2E_PG_CONNECTION targets a Supabase production host (*.supabase.co). " +
                "E2E tests must NEVER run against the production database. " +
                "Use a local PostgreSQL instance or a dedicated CI service container.");
        }

        return baseConn;
    }

    /// <summary>
    /// Returns a connection string for the PostgreSQL admin database (used to CREATE/DROP the test DB).
    /// </summary>
    private static string GetAdminConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(GetBaseConnectionString());
        builder.Database = "postgres";
        builder.Pooling = false;
        return builder.ToString();
    }

    /// <summary>
    /// Returns a connection string targeting the fixture-specific test database.
    /// </summary>
    public string GetConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(GetBaseConnectionString());
        builder.Database = TestDatabaseName;
        builder.Pooling = false;
        return builder.ToString();
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
            // Override connection string to use the fixture-specific PostgreSQL test database
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Local"] = GetConnectionString(),
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

            // Use the fixture-specific PostgreSQL database for E2E testing
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(GetConnectionString(), npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(60);
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

            // Override the scoped HttpClient (used by Blazor components such as MLSuggestionBadge)
            // to add the X-E2E-Internal:1 header on every outgoing request.  The production
            // registration uses AuthTokenHandler which forwards a Supabase JWT — unavailable in
            // E2E tests — so requests from server-side Blazor components to the local API would
            // otherwise arrive without credentials and receive 401 Unauthorized.
            services.RemoveAll<HttpClient>();
            services.AddScoped<HttpClient>(sp =>
            {
                var navigationManager = sp.GetRequiredService<NavigationManager>();
                var handler = new E2EDevSmokeHeaderHandler();
                handler.InnerHandler = new HttpClientHandler();
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri(navigationManager.BaseUri)
                };
            });

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "E2ECookie";
                options.DefaultAuthenticateScheme = "E2ECookie";
                options.DefaultChallengeScheme = "E2ECookie";
            })
            .AddScheme<AuthenticationSchemeOptions, E2ECookieAuthenticationHandler>("E2ECookie", _ => { });

            // Replace AuthenticationStateProvider so AuthorizeRouteView treats every Playwright
            // request as authenticated (bypasses the /login redirect for the interactive circuit).
            // Also keep the concrete CustomAuthenticationStateProvider alias and add IAuthStateNotifier
            // so the Login/Logout pages (which inject IAuthStateNotifier) resolve correctly.
            services.RemoveAll<AuthenticationStateProvider>();
            services.RemoveAll<CustomAuthenticationStateProvider>();
            services.RemoveAll<IAuthStateNotifier>();
            services.AddScoped<CookieAwareAuthenticationStateProvider>();
            services.AddScoped<CustomAuthenticationStateProvider>(
                sp => sp.GetRequiredService<CookieAwareAuthenticationStateProvider>());
            services.AddScoped<AuthenticationStateProvider>(
                sp => sp.GetRequiredService<CookieAwareAuthenticationStateProvider>());
            services.AddScoped<IAuthStateNotifier>(
                sp => sp.GetRequiredService<CookieAwareAuthenticationStateProvider>());

            // In Blazor 8 static SSR, AuthorizeRouteView reads HttpContext.User (set by ASP.NET Core
            // auth middleware) rather than calling AuthenticationStateProvider. Register a startup
            // filter so that requests carrying the "e2e-auth-token" cookie have HttpContext.User set
            // to an authenticated principal before any other middleware runs.
            // Tests that verify unauthenticated behaviour create a fresh browser context without
            // this cookie, so those requests still see an anonymous HttpContext.User.
            services.AddSingleton<IStartupFilter>(new E2EAuthBypassStartupFilter());

            // Reduce Blazor Server circuit retention from 3 minutes (default) to 5 seconds.
            // Each test navigation (SetUp GotoAsync + NavigateAsync) creates a new Blazor circuit.
            // With 9+ tests per fixture, 30+ zombie circuits accumulate, each holding a scoped
            // DbContext and DI services. The server's thread pool gets starved as it services
            // reconnection attempts for circuits that will never reconnect.
            // 5 seconds is enough for any in-flight render batch to settle; circuits that have
            // genuinely disconnected (navigated away) are released almost immediately.
            services.PostConfigure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
            {
                options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(5);
            });
        });

        // Configure logging to output to test console
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }

    /// <summary>
    /// Initializes the PostgreSQL test database for this fixture.
    /// Drops the existing fixture database (if any), recreates it, applies migrations, and seeds data.
    /// Should be called once per fixture (OneTimeSetUp), BEFORE the server starts.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        var adminConnString = GetAdminConnectionString();
        var fixtureDb = TestDatabaseName;

        // Drop and recreate the fixture database for a clean slate
        await using (var adminConn = new NpgsqlConnection(adminConnString))
        {
            await adminConn.OpenAsync();

            // Terminate all existing connections to the test database before dropping
            await using (var terminateCmd = adminConn.CreateCommand())
            {
                terminateCmd.CommandText = $"""
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = '{fixtureDb}' AND pid <> pg_backend_pid();
                    """;
                await terminateCmd.ExecuteNonQueryAsync();
            }

            await using (var dropCmd = adminConn.CreateCommand())
            {
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{fixtureDb}\"";
                await dropCmd.ExecuteNonQueryAsync();
            }

            await using (var createCmd = adminConn.CreateCommand())
            {
                createCmd.CommandText = $"CREATE DATABASE \"{fixtureDb}\"";
                await createCmd.ExecuteNonQueryAsync();
            }
        }

        // Apply migrations before the web host starts to avoid concurrent migration during startup
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(GetConnectionString(), npgsqlOptions => npgsqlOptions.CommandTimeout(60))
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
        var seedService = new DevelopmentUserSeedService(context);
        await seedService.SeedAsync();
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

        // Truncate all tables in reverse dependency order (PostgreSQL-compatible)
        await context.Database.ExecuteSqlRawAsync(@"
            TRUNCATE TABLE ""TransactionSplits"", ""TransactionAudits"", ""LabeledExamples"",
                           ""Transactions"", ""BudgetLines"", ""Categories"",
                           ""BudgetPlans"", ""Accounts"", ""MLModels"", ""AppSettings"" CASCADE;
        ");
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
    /// Closes database connections and drops the fixture test database.
    /// Call this before disposing the factory.
    /// </summary>
    public async Task CloseAllConnectionsAsync()
    {
        if (_host == null)
        {
            return;
        }

        // Clear Npgsql connection pool for the fixture database
        NpgsqlConnection.ClearPool(new NpgsqlConnection(GetConnectionString()));

        // Give the pool time to drain
        await Task.Delay(50);

        // Drop the fixture database to clean up
        try
        {
            var adminConnString = GetAdminConnectionString();
            var fixtureDb = TestDatabaseName;

            await using var adminConn = new NpgsqlConnection(adminConnString);
            await adminConn.OpenAsync();

            await using (var terminateCmd = adminConn.CreateCommand())
            {
                terminateCmd.CommandText = $"""
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = '{fixtureDb}' AND pid <> pg_backend_pid();
                    """;
                await terminateCmd.ExecuteNonQueryAsync();
            }

            await using var dropCmd = adminConn.CreateCommand();
            dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{fixtureDb}\"";
            await dropCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort: ignore errors during cleanup
        }
    }

    protected override void Dispose(bool disposing)
    {
        // Call base disposal to ensure all WebApplicationFactory resources are disposed
        base.Dispose(disposing);

        if (disposing)
        {
            // Clear Npgsql connection pools for the fixture database
            try
            {
                NpgsqlConnection.ClearPool(new NpgsqlConnection(GetConnectionString()));
            }
            catch
            {
                // Best-effort
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
