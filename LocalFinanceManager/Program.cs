using LocalFinanceManager.Components;
using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.DTOs.Validators;
using LocalFinanceManager.Models;
using FluentValidation;
using IbanNet;
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
builder.Services.Configure<LocalFinanceManager.Configuration.MLOptions>(builder.Configuration.GetSection("MLOptions"));
builder.Services.Configure<LocalFinanceManager.Configuration.AutomationOptions>(builder.Configuration.GetSection("AutomationOptions"));
builder.Services.Configure<LocalFinanceManager.Configuration.CacheOptions>(builder.Configuration.GetSection("Caching"));

// Register memory cache with size limits
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Maximum 1000 cached entries
    options.CompactionPercentage = 0.25; // Remove 25% when limit reached
});

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBudgetPlanRepository, BudgetPlanRepository>();
builder.Services.AddScoped<IBudgetLineRepository, BudgetLineRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionSplitRepository, TransactionSplitRepository>();
builder.Services.AddScoped<ITransactionAuditRepository, TransactionAuditRepository>();
builder.Services.AddScoped<ILabeledExampleRepository, LabeledExampleRepository>();

// Register services
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<BudgetPlanService>();
builder.Services.AddScoped<ITransactionAssignmentService, TransactionAssignmentService>();

// Register cache services
builder.Services.AddSingleton<ICacheKeyTracker, CacheKeyTracker>();
builder.Services.AddScoped<IBudgetAccountLookupService, BudgetAccountLookupService>();

// Register ML services
builder.Services.AddScoped<LocalFinanceManager.ML.IFeatureExtractor, LocalFinanceManager.ML.FeatureExtractor>();
builder.Services.AddScoped<LocalFinanceManager.ML.IMLService, LocalFinanceManager.Services.MLService>();

// Register automation services
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IUndoService, UndoService>();

// Register background services
builder.Services.AddHostedService<LocalFinanceManager.Services.Background.MLRetrainingBackgroundService>();
builder.Services.AddHostedService<LocalFinanceManager.Services.Background.AutoApplyBackgroundService>();

// Register import services
builder.Services.AddScoped<LocalFinanceManager.Services.Import.CsvImportParser>();
builder.Services.AddScoped<LocalFinanceManager.Services.Import.JsonImportParser>();
builder.Services.AddScoped<LocalFinanceManager.Services.Import.ExactMatchStrategy>();
builder.Services.AddScoped<LocalFinanceManager.Services.Import.FuzzyMatchStrategy>();
builder.Services.AddScoped<LocalFinanceManager.Services.Import.ImportService>();

// Register validators
builder.Services.AddScoped<IValidator<CreateAccountRequest>, CreateAccountRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateAccountRequest>, UpdateAccountRequestValidator>();
builder.Services.AddScoped<IValidator<CreateCategoryDto>, CreateCategoryDtoValidator>();
builder.Services.AddScoped<IValidator<CreateBudgetPlanDto>, CreateBudgetPlanDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateBudgetPlanDto>, UpdateBudgetPlanDtoValidator>();
builder.Services.AddScoped<IValidator<CreateBudgetLineDto>, CreateBudgetLineDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateBudgetLineDto>, UpdateBudgetLineDtoValidator>();
builder.Services.AddScoped<IValidator<AssignTransactionRequest>, AssignTransactionRequestValidator>();
builder.Services.AddScoped<IValidator<SplitTransactionRequest>, SplitTransactionRequestValidator>();
builder.Services.AddScoped<IValidator<BulkAssignTransactionsRequest>, BulkAssignTransactionsRequestValidator>();
builder.Services.AddScoped<IValidator<UndoAssignmentRequest>, UndoAssignmentRequestValidator>();

// Register IbanNet
builder.Services.AddSingleton<IIbanValidator, IbanValidator>();

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
