using LocalFinanceManager.Components;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Extensions;
using LocalFinanceManager.Services;
using LocalFinanceManager.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Text;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("Local")
    ?? "Host=localhost;Port=5432;Database=localfinancemanager;Username=postgres;Password=postgres";

// Guard: refuse to start in Production with a localhost connection string
if (!builder.Environment.IsDevelopment() && connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Local is still set to a localhost value. " +
        "Set the real PostgreSQL connection string via the ConnectionStrings__Local environment variable before deploying.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add database health check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// Register configuration options
builder.Services.Configure<ImportOptions>(builder.Configuration.GetSection("ImportOptions"));
builder.Services.Configure<MLOptions>(builder.Configuration.GetSection("MLOptions"));
builder.Services.Configure<AutomationOptions>(builder.Configuration.GetSection("AutomationOptions"));
builder.Services.Configure<BulkAssignUiOptions>(builder.Configuration.GetSection("BulkAssignUiOptions"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Caching"));
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));

// Register memory cache with size limits
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Maximum 1000 cached entries
    options.CompactionPercentage = 0.25; // Remove 25% when limit reached
});

// Register application services using extension methods (in dependency order)
builder.Services.AddDataAccess();           // Repositories (base layer)
builder.Services.AddValidation();           // Validators (no dependencies)
builder.Services.AddCachingServices();      // Cache infrastructure
builder.Services.AddDomainServices();       // Core domain services
builder.Services.AddImportServices();       // CSV/JSON import
builder.Services.AddMLServices();           // ML feature extraction & prediction
builder.Services.AddAutomationServices();   // Automation & background workers
builder.Services.AddAuthServices();         // Authentication & user context

// Configure JWT Bearer authentication
var supabaseOptions = builder.Configuration.GetSection("Supabase").Get<SupabaseOptions>() ?? new SupabaseOptions();
var enableDevSmokeAuth = builder.Configuration.GetValue<bool>("EnableDevSmokeAuth");
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SmartAuth";
    options.DefaultAuthenticateScheme = "SmartAuth";
    options.DefaultChallengeScheme = "SmartAuth";
})
    .AddPolicyScheme("SmartAuth", "Smart auth selector", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment()
                && enableDevSmokeAuth
                && context.Request.Headers.ContainsKey("X-Dev-Smoke-UserId"))
            {
                return "DevSmoke";
            }

            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddScheme<AuthenticationSchemeOptions, DevSmokeAuthenticationHandler>("DevSmoke", _ => { })
    .AddJwtBearer(options =>
    {
        var jwtSecret = supabaseOptions.JwtSecret;
        if (!string.IsNullOrEmpty(jwtSecret) && jwtSecret != "placeholder-jwt-secret")
        {
            // Supabase projects use RS256 with rotating key pairs (kid = UUID).
            // Use OIDC discovery so .NET auto-fetches the JWKS and matches by kid.
            // The HS256 JWT secret is kept as a fallback IssuerSigningKeyResolver for
            // projects that have not yet migrated to RS256.
            options.Authority = $"{supabaseOptions.Url}/auth/v1";
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = $"{supabaseOptions.Url}/auth/v1",
                ValidAudience = "authenticated",
                // HS256 fallback: if JWKS lookup fails, try the shared secret
                IssuerSigningKeyResolver = (_, _, kid, _) =>
                {
                    var fallback = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret.Trim()));
                    return [fallback];
                }
            };
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    log.LogWarning("JWT authentication failed: {Reason}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        }
        else
        {
            // Placeholder mode: accept any token (development without Supabase configured)
            // WARNING: This disables all JWT validation. Ensure proper Supabase credentials are set in production.

            // Guard: refuse to start in non-Development environments with placeholder credentials.
            // This prevents accidentally deploying with unset Supabase secrets.
            var currentEnv = builder.Environment;
            if (!currentEnv.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Supabase:JwtSecret is not configured (or still set to the placeholder value). " +
                    "This application refuses to start in non-Development environments without a valid JWT secret. " +
                    "Set the ASPNETCORE_Supabase__JwtSecret environment variable to the real Supabase JWT secret.");
            }

            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    log.LogWarning("JWT validation is in PLACEHOLDER MODE (no Supabase credentials configured). This is only safe in Development.");
                    return Task.CompletedTask;
                }
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, _) =>
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    return handler.ReadJwtToken(token);
                }
            };
        }
    });

builder.Services.AddAuthorization();

// Register HttpClientFactory (used by AuthService)
builder.Services.AddHttpClient();

// Register HttpClient for Blazor components with automatic JWT Bearer forwarding.
// NOTE: AuthTokenHandler must NOT be used via AddHttpMessageHandler<T>() because
// IHttpClientFactory resolves handlers from its own internal scope, not the Blazor
// circuit scope. This means the circuit-scoped AuthTokenStore (which holds the JWT
// token retrieved from sessionStorage) would not be resolved from the correct circuit
// scope, causing all API calls to go out without a Bearer token.
// Instead we build the HttpClient manually from the circuit's DI scope so that
// AuthTokenHandler gets the correct AuthTokenStore instance for the active circuit.
builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    // Resolve the handler from the current (circuit) scope so AuthTokenStore works correctly.
    var tokenHandler = sp.GetRequiredService<AuthTokenHandler>();
    tokenHandler.InnerHandler = new HttpClientHandler();
    var client = new HttpClient(tokenHandler)
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
    return client;
});

// Add controllers for API endpoints
builder.Services.AddControllers();

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var skipDatabaseMigrations = app.Configuration.GetValue<bool>("SkipDatabaseMigrations");

    // Recreate database if environment variable is set (Development only)
    var recreateDb = app.Configuration.GetValue<bool>("RecreateDatabase");
    if (recreateDb)
    {
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureDeletedAsync();
            app.Logger.LogInformation("Database deleted due to RecreateDatabase flag");
        }
        else
        {
            app.Logger.LogWarning("RecreateDatabase flag ignored in non-Development environment for safety");
        }
    }

    if (!skipDatabaseMigrations)
    {
        await context.Database.MigrateAsync();
    }

    // Update accounts to reference their current budget plan (all environments)
    await UpdateAccountBudgetPlanReferencesAsync(context);
}

/// <summary>
/// Updates accounts to reference their current budget plan (most recent year).
/// </summary>
static async Task UpdateAccountBudgetPlanReferencesAsync(AppDbContext context)
{
    var accountsWithoutBudgetPlan = await context.Accounts
        .Where(a => !a.IsArchived && a.CurrentBudgetPlanId == null)
        .ToListAsync();

    if (!accountsWithoutBudgetPlan.Any())
    {
        return;
    }

    // Collect account IDs to batch-load budget plans and avoid N+1 queries
    var accountIds = accountsWithoutBudgetPlan
        .Select(a => a.Id)
        .ToList();

    // Load all non-archived budget plans for these accounts in a single query
    var budgetPlans = await context.BudgetPlans
        .Where(bp => !bp.IsArchived && accountIds.Contains(bp.AccountId))
        .ToListAsync();

    // Group in memory and find the most recent budget plan per account
    var latestByAccountId = budgetPlans
        .GroupBy(bp => bp.AccountId)
        .ToDictionary(
            g => g.Key,
            g => g.OrderByDescending(bp => bp.Year).First()
        );

    foreach (var account in accountsWithoutBudgetPlan)
    {
        if (latestByAccountId.TryGetValue(account.Id, out var latestBudgetPlan))
        {
            account.CurrentBudgetPlanId = latestBudgetPlan.Id;
        }
    }

    await context.SaveChangesAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only apply status code pages for non-API requests; API controllers must return their own
// error responses (e.g. 401, 404) without having them replaced by the Blazor HTML shell.
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    b => b.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API controllers
app.MapControllers();

// Database health check endpoint
app.MapHealthChecks("/health/db");

app.Run();

// Make Program class accessible to test projects
public partial class Program { }

public sealed class DevSmokeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevSmokeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var env = Context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var configuration = Context.RequestServices.GetRequiredService<IConfiguration>();
        var enableDevSmokeAuth = configuration.GetValue<bool>("EnableDevSmokeAuth");

        if (!env.IsDevelopment() || !enableDevSmokeAuth)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue("X-Dev-Smoke-UserId", out var userIdHeader)
            || !Guid.TryParse(userIdHeader.ToString(), out var userId)
            || userId == Guid.Empty)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var sub = Request.Headers.TryGetValue("X-Dev-Smoke-Sub", out var subHeader)
            ? subHeader.ToString()
            : "00000000-0000-0000-0000-000000000001";

        var email = Request.Headers.TryGetValue("X-Dev-Smoke-Email", out var emailHeader)
            ? emailHeader.ToString()
            : "dev-smoke@localfinancemanager.local";

        var claims = new[]
        {
            new Claim("sub", sub),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
