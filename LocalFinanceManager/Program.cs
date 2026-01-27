using LocalFinanceManager.Components;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Extensions;
using LocalFinanceManager.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with SQLite
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=localfinancemanager.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Register configuration options
builder.Services.Configure<ImportOptions>(builder.Configuration.GetSection("ImportOptions"));
builder.Services.Configure<MLOptions>(builder.Configuration.GetSection("MLOptions"));
builder.Services.Configure<AutomationOptions>(builder.Configuration.GetSection("AutomationOptions"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Caching"));

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

// Register HttpClient for Blazor components
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
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

    await context.Database.MigrateAsync();

    // Seed only in Development
    if (app.Environment.IsDevelopment())
    {
        await context.SeedAsync();
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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map API controllers
app.MapControllers();

app.Run();

// Make Program class accessible to test projects
public partial class Program { }
