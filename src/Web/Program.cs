using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Application.Services;
using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Import;
using LocalFinanceManager.Infrastructure.Repositories;
using LocalFinanceManager.Web.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register DbContext with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Infrastructure Services (Repositories)
builder.Services.AddScoped<IAccountRepository, EfAccountRepository>();
builder.Services.AddScoped<ITransactionRepository, EfTransactionRepository>();
builder.Services.AddScoped<ICategoryRepository, EfCategoryRepository>();
builder.Services.AddScoped<IEnvelopeRepository, EfEnvelopeRepository>();
builder.Services.AddScoped<ICategoryLearningProfileRepository, EfCategoryLearningProfileRepository>();
builder.Services.AddScoped<IRuleRepository, EfRuleRepository>();
builder.Services.AddScoped<IBudgetRepository, EfBudgetRepository>();

// Register Application Services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IDeduplicationService, DeduplicationService>();
builder.Services.AddScoped<IScoringEngine, ScoringEngine>();
builder.Services.AddScoped<ILearningService, LearningService>();
builder.Services.AddScoped<IRuleEngine, RuleEngine>();
builder.Services.AddScoped<IBudgetService, BudgetService>();

// Register Transaction Importers
builder.Services.AddScoped<ITransactionImporter, CsvTransactionImporter>();
builder.Services.AddScoped<ITransactionImporter, JsonTransactionImporter>();
builder.Services.AddScoped<ITransactionImporter, Mt940TransactionImporter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

// Map minimal API endpoints for Transactions
app.MapGet("/api/transactions/{id}", async (int id, ITransactionService transactionService) =>
{
    var transaction = await transactionService.GetByIdAsync(id);
    return transaction is not null ? Results.Ok(transaction) : Results.NotFound();
});

app.MapGet("/api/transactions/account/{accountId}", async (int accountId, ITransactionService transactionService) =>
{
    var transactions = await transactionService.GetByAccountIdAsync(accountId);
    return Results.Ok(transactions);
});

app.MapPost("/api/transactions", async (Transaction transaction, ITransactionService transactionService) =>
{
    var created = await transactionService.AddAsync(transaction);
    return Results.Created($"/api/transactions/{created.Id}", created);
});

app.MapPut("/api/transactions/{id}", async (int id, Transaction transaction, ITransactionService transactionService) =>
{
    if (id != transaction.Id)
    {
        return Results.BadRequest("ID mismatch");
    }
    await transactionService.UpdateAsync(transaction);
    return Results.NoContent();
});

app.MapDelete("/api/transactions/{id}", async (int id, ITransactionService transactionService) =>
{
    var deleted = await transactionService.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

// Minimal API endpoints for Transaction Splits
app.MapGet("/api/transactions/{id}/splits", async (int id, ITransactionService transactionService) =>
{
    var splits = await transactionService.GetSplitsByParentIdAsync(id);
    return Results.Ok(splits);
});

app.MapGet("/api/transactions/splits/{id}", async (int id, ITransactionService transactionService) =>
{
    var split = await transactionService.GetSplitByIdAsync(id);
    return split is not null ? Results.Ok(split) : Results.NotFound();
});

app.MapPost("/api/transactions/{id}/splits", async (int id, List<TransactionSplit> splits, ITransactionService transactionService) =>
{
    // Ensure incoming splits reference the correct parent
    foreach (var s in splits)
    {
        s.ParentTransactionId = id;
    }

    var created = await transactionService.AddSplitsAsync(splits);
    return Results.Created($"/api/transactions/{id}/splits", created);
});

app.MapPost("/api/transactions/splits", async (TransactionSplit split, ITransactionService transactionService) =>
{
    var created = await transactionService.AddSplitAsync(split);
    return Results.Created($"/api/transactions/splits/{created.Id}", created);
});

app.MapPut("/api/transactions/splits/{id}", async (int id, TransactionSplit split, ITransactionService transactionService) =>
{
    if (id != split.Id)
    {
        return Results.BadRequest("ID mismatch");
    }

    await transactionService.UpdateSplitAsync(split);
    return Results.NoContent();
});

app.MapDelete("/api/transactions/splits/{id}", async (int id, ITransactionService transactionService) =>
{
    var deleted = await transactionService.DeleteSplitAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

// Map minimal API endpoints for Accounts
app.MapGet("/api/accounts", async (IAccountRepository accountRepository) =>
{
    var accounts = await accountRepository.GetAllAsync();
    return Results.Ok(accounts);
});

app.MapGet("/api/accounts/{id}", async (int id, IAccountRepository accountRepository) =>
{
    var account = await accountRepository.GetByIdAsync(id);
    return account is not null ? Results.Ok(account) : Results.NotFound();
});

app.MapPost("/api/accounts", async (Account account, IAccountRepository accountRepository) =>
{
    var created = await accountRepository.AddAsync(account);
    return Results.Created($"/api/accounts/{created.Id}", created);
});

app.MapPut("/api/accounts/{id}", async (int id, Account account, IAccountRepository accountRepository) =>
{
    if (id != account.Id)
    {
        return Results.BadRequest("ID mismatch");
    }
    await accountRepository.UpdateAsync(account);
    return Results.NoContent();
});

app.MapDelete("/api/accounts/{id}", async (int id, IAccountRepository accountRepository) =>
{
    var deleted = await accountRepository.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

// Map minimal API endpoints for Categories
app.MapGet("/api/categories", async (ICategoryRepository categoryRepository) =>
{
    var categories = await categoryRepository.GetAllAsync();
    return Results.Ok(categories);
});

app.MapGet("/api/categories/{id}", async (int id, ICategoryRepository categoryRepository) =>
{
    var category = await categoryRepository.GetByIdAsync(id);
    return category is not null ? Results.Ok(category) : Results.NotFound();
});

app.MapPost("/api/categories", async (Category category, ICategoryRepository categoryRepository) =>
{
    var created = await categoryRepository.AddAsync(category);
    return Results.Created($"/api/categories/{created.Id}", created);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Seed initial accounts and ensure migrations are applied
using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var applyMigrations = configuration.GetValue<bool>("ApplyMigrationsOnStartup", true);

    if (applyMigrations)
    {
        // Run migration + seeding in background so dotnet run isn't blocked.
        _ = Task.Run(async () =>
        {
            using var bgScope = app.Services.CreateScope();
            var db = bgScope.ServiceProvider.GetRequiredService<LocalFinanceManager.Infrastructure.ApplicationDbContext>();
            var logger = bgScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                await db.Database.MigrateAsync();

                if (!db.Accounts.Any())
                {
                    db.Accounts.AddRange(
                        new LocalFinanceManager.Domain.Entities.Account { Name = "Checking", AccountType = "Checking", InitialBalance = 1000m, IsActive = true },
                        new LocalFinanceManager.Domain.Entities.Account { Name = "Savings", AccountType = "Savings", InitialBalance = 5000m, IsActive = true }
                    );
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background database migration/seed failed");
            }
        });
    }
    else
    {
        // Optionally log that migrations are skipped
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("ApplyMigrationsOnStartup = false, skipping migrations at startup.");
    }
}
app.Run();
